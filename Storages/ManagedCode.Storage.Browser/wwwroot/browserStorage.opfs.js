const opfsRequests = new Map();
let opfsRequestId = 0;
let opfsWorkerPromise;

export function supportsOpfs() {
  return typeof navigator !== "undefined" && typeof navigator.storage?.getDirectory === "function";
}

export function normalizeBytes(data) {
  return data instanceof Uint8Array ? data : new Uint8Array(data ?? []);
}

async function getOpfsWorker() {
  if (!supportsOpfs()) {
    return null;
  }

  if (!opfsWorkerPromise) {
    opfsWorkerPromise = Promise.resolve(new Worker(new URL("./browserStorage.worker.js", import.meta.url), { type: "module" }));
    const worker = await opfsWorkerPromise;
    worker.onmessage = (event) => {
      const { id, result, error } = event.data ?? {};
      const pending = opfsRequests.get(id);
      if (!pending) {
        return;
      }

      opfsRequests.delete(id);
      if (error) {
        pending.reject(new Error(error));
        return;
      }

      pending.resolve(result);
    };

    worker.onerror = (event) => {
      for (const [, pending] of opfsRequests) {
        pending.reject(event.error ?? new Error(event.message ?? "OPFS worker failed."));
      }

      opfsRequests.clear();
      opfsWorkerPromise = undefined;
    };
  }

  return opfsWorkerPromise;
}

async function postToOpfsWorker(command, payload, transferables = []) {
  const worker = await getOpfsWorker();
  if (!worker) {
    return null;
  }

  return await new Promise((resolve, reject) => {
    const id = ++opfsRequestId;
    opfsRequests.set(id, { resolve, reject });
    worker.postMessage({ id, command, payload }, transferables);
  });
}

export async function beginPayloadWriteInternal(databaseName, blobKey) {
  if (!supportsOpfs()) {
    return false;
  }

  try {
    const result = await postToOpfsWorker("beginWrite", { databaseName, blobKey });
    return result === true;
  } catch {
    return false;
  }
}

export async function appendPayloadChunksInternal(databaseName, blobKey, chunks) {
  const normalizedChunks = (Array.isArray(chunks) ? chunks : []).map((chunk) => {
    const data = normalizeBytes(chunk.data);
    return { data };
  });

  const transferables = normalizedChunks.map((chunk) => chunk.data.buffer);
  await postToOpfsWorker("appendChunks", { databaseName, blobKey, chunks: normalizedChunks }, transferables);
}

export async function completePayloadWriteInternal(databaseName, blobKey) {
  await postToOpfsWorker("completeWrite", { databaseName, blobKey });
}

export async function abortPayloadWriteInternal(databaseName, blobKey) {
  await postToOpfsWorker("abortWrite", { databaseName, blobKey });
}

export async function readPayloadRangeInternal(databaseName, blobKey, offset, count) {
  if (!supportsOpfs()) {
    return null;
  }

  const result = await postToOpfsWorker("readRange", { databaseName, blobKey, offset, count });
  return result ? normalizeBytes(result) : null;
}

export async function opfsFileExistsInternal(databaseName, blobKey) {
  if (!supportsOpfs()) {
    return false;
  }

  try {
    const result = await postToOpfsWorker("fileExists", { databaseName, blobKey });
    return result === true;
  } catch {
    return false;
  }
}

export async function tryDeletePayloadFileInternal(databaseName, blobKey) {
  if (!supportsOpfs()) {
    return false;
  }

  try {
    const result = await postToOpfsWorker("deleteFile", { databaseName, blobKey });
    return result === true;
  } catch {
    return false;
  }
}
