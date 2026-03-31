const sessions = new Map();

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
    const written = session.accessHandle.write(data, { at: session.position });
    if (written !== data.byteLength) {
      throw new Error(`Short OPFS write for ${blobKey}: wrote ${written} of ${data.byteLength} bytes.`);
    }

    session.position += written;
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
