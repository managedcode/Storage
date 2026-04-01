const sessions = new Map();
const maxWriteBlockBytes = 1024 * 1024;
const maxDigestReadBlockBytes = 4 * 1024 * 1024;
const crc32Table = buildCrc32Table();

self.onmessage = async (event) => {
  const { id, command, payload } = event.data ?? {};

  try {
    const result = await dispatchAsync(command, payload ?? {});
    const transferables = result instanceof Uint8Array ? [result.buffer] : [];
    self.postMessage({ id, result }, transferables);
  } catch (error) {
    self.postMessage({
      id,
      error: error instanceof Error ? error.message : String(error)
    });
  }
};

async function dispatchAsync(command, payload) {
  switch (command) {
    case "beginWrite":
      return await beginWriteAsync(payload.databaseName, payload.blobKey);
    case "appendChunks":
      await appendChunksAsync(payload.databaseName, payload.blobKey, payload.chunks);
      return true;
    case "completeWrite":
      await completeWriteAsync(payload.databaseName, payload.blobKey);
      return true;
    case "abortWrite":
      await abortWriteAsync(payload.databaseName, payload.blobKey);
      return true;
    case "readRange":
      return await readRangeAsync(payload.databaseName, payload.blobKey, payload.offset, payload.count);
    case "getFileDigest":
      return await getFileDigestAsync(payload.databaseName, payload.blobKey);
    case "deleteFile":
      return await deleteFileAsync(payload.databaseName, payload.blobKey);
    case "fileExists":
      return await fileExistsAsync(payload.databaseName, payload.blobKey);
    default:
      throw new Error(`Unsupported OPFS command: ${command}`);
  }
}

async function beginWriteAsync(databaseName, blobKey) {
  const sessionKey = getSessionKey(databaseName, blobKey);
  const existing = sessions.get(sessionKey);
  if (existing) {
    existing.accessHandle.close();
    sessions.delete(sessionKey);
  }

  const fileHandle = await getBlobFileHandleAsync(databaseName, blobKey, true);
  const accessHandle = await fileHandle.createSyncAccessHandle();
  accessHandle.truncate(0);
  sessions.set(sessionKey, { accessHandle, position: 0 });
  return true;
}

async function appendChunksAsync(databaseName, blobKey, chunks) {
  const session = getSession(databaseName, blobKey);

  for (const chunk of Array.isArray(chunks) ? chunks : []) {
    const data = chunk.data instanceof Uint8Array ? chunk.data : new Uint8Array(chunk.data ?? []);
    writeAllBytes(session, blobKey, data);
  }
}

async function completeWriteAsync(databaseName, blobKey) {
  const sessionKey = getSessionKey(databaseName, blobKey);
  const session = sessions.get(sessionKey);
  if (!session) {
    return;
  }

  try {
    session.accessHandle.flush();
  } finally {
    session.accessHandle.close();
    sessions.delete(sessionKey);
  }
}

async function abortWriteAsync(databaseName, blobKey) {
  const sessionKey = getSessionKey(databaseName, blobKey);
  const session = sessions.get(sessionKey);
  if (session) {
    try {
      session.accessHandle.close();
    } finally {
      sessions.delete(sessionKey);
    }
  }

  await deleteFileAsync(databaseName, blobKey);
}

async function readRangeAsync(databaseName, blobKey, offset, count) {
  const fileHandle = await getBlobFileHandleAsync(databaseName, blobKey, false);
  const accessHandle = await fileHandle.createSyncAccessHandle();

  try {
    const size = accessHandle.getSize();
    if (offset >= size || count <= 0) {
      return new Uint8Array();
    }

    const bytesToRead = Math.min(count, size - offset);
    const buffer = new Uint8Array(bytesToRead);
    const bytesRead = accessHandle.read(buffer, { at: offset });
    return bytesRead === bytesToRead ? buffer : buffer.subarray(0, bytesRead);
  } finally {
    accessHandle.close();
  }
}

async function deleteFileAsync(databaseName, blobKey) {
  try {
    const directory = await getDatabaseDirectoryAsync(databaseName, false);
    await directory.removeEntry(getBlobFileName(blobKey));
    return true;
  } catch (error) {
    if (error?.name === "NotFoundError") {
      return false;
    }

    throw error;
  }
}

async function getFileDigestAsync(databaseName, blobKey) {
  const fileHandle = await getBlobFileHandleAsync(databaseName, blobKey, false);
  const accessHandle = await fileHandle.createSyncAccessHandle();

  try {
    const size = accessHandle.getSize();
    if (size <= 0) {
      return { length: 0, crc: 0 };
    }

    const buffer = new Uint8Array(Math.min(maxDigestReadBlockBytes, size));
    let offset = 0;
    let crc = 0xffffffff;

    while (offset < size) {
      const bytesToRead = Math.min(buffer.byteLength, size - offset);
      const bytesRead = normalizeBytesRead(
        accessHandle.read(buffer.subarray(0, bytesToRead), { at: offset }),
        bytesToRead,
        blobKey,
        offset);

      crc = updateCrc32(crc, buffer, bytesRead);
      offset += bytesRead;
    }

    return {
      length: Number(size),
      crc: completeCrc32(crc)
    };
  } finally {
    accessHandle.close();
  }
}

async function fileExistsAsync(databaseName, blobKey) {
  try {
    await getBlobFileHandleAsync(databaseName, blobKey, false);
    return true;
  } catch (error) {
    if (error?.name === "NotFoundError") {
      return false;
    }

    throw error;
  }
}

function getSession(databaseName, blobKey) {
  const session = sessions.get(getSessionKey(databaseName, blobKey));
  if (!session) {
    throw new Error(`No active OPFS session for ${blobKey}.`);
  }

  return session;
}

function getSessionKey(databaseName, blobKey) {
  return `${databaseName}::${blobKey}`;
}

function writeAllBytes(session, blobKey, data) {
  let offset = 0;

  while (offset < data.byteLength) {
    const bytesRemaining = data.byteLength - offset;
    const bytesToWrite = Math.min(bytesRemaining, maxWriteBlockBytes);
    const slice = data.subarray(offset, offset + bytesToWrite);
    const written = normalizeBytesWritten(
      session.accessHandle.write(slice, { at: session.position }),
      bytesToWrite,
      blobKey,
      session.position);

    offset += written;
    session.position += written;
  }
}

function normalizeBytesWritten(value, expectedBytes, blobKey, position) {
  if (!Number.isFinite(value)) {
    throw new Error(`Invalid OPFS write result for ${blobKey} at ${position}: ${String(value)}.`);
  }

  const written = Math.trunc(value);
  if (written <= 0 || written > expectedBytes) {
    throw new Error(`Invalid OPFS write result for ${blobKey} at ${position}: wrote ${written} of ${expectedBytes} bytes.`);
  }

  return written;
}

function normalizeBytesRead(value, expectedBytes, blobKey, position) {
  if (!Number.isFinite(value)) {
    throw new Error(`Invalid OPFS read result for ${blobKey} at ${position}: ${String(value)}.`);
  }

  const bytesRead = Math.trunc(value);
  if (bytesRead <= 0 || bytesRead > expectedBytes) {
    throw new Error(`Invalid OPFS read result for ${blobKey} at ${position}: read ${bytesRead} of ${expectedBytes} bytes.`);
  }

  return bytesRead;
}

async function getDatabaseDirectoryAsync(databaseName, create) {
  const root = await navigator.storage.getDirectory();
  return await root.getDirectoryHandle(getDatabaseDirectoryName(databaseName), { create });
}

async function getBlobFileHandleAsync(databaseName, blobKey, create) {
  const directory = await getDatabaseDirectoryAsync(databaseName, create);
  return await directory.getFileHandle(getBlobFileName(blobKey), { create });
}

function getDatabaseDirectoryName(databaseName) {
  return encodeURIComponent(databaseName);
}

function getBlobFileName(blobKey) {
  return encodeURIComponent(blobKey);
}

function buildCrc32Table() {
  const table = new Uint32Array(256);

  for (let index = 0; index < table.length; index++) {
    let value = index;

    for (let bit = 0; bit < 8; bit++) {
      value = (value & 1) === 0 ? value >>> 1 : (value >>> 1) ^ 0xedb88320;
    }

    table[index] = value >>> 0;
  }

  return table;
}

function updateCrc32(crc, buffer, length) {
  let current = crc >>> 0;

  for (let index = 0; index < length; index++) {
    current = (current >>> 8) ^ crc32Table[(current ^ buffer[index]) & 0xff];
  }

  return current >>> 0;
}

function completeCrc32(crc) {
  return (crc ^ 0xffffffff) >>> 0;
}
