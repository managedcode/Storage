import {
  abortPayloadWriteInternal,
  appendPayloadChunksInternal,
  beginPayloadWriteInternal,
  completePayloadWriteInternal,
  opfsFileExistsInternal,
  readPayloadRangeInternal,
  tryDeletePayloadFileInternal
} from "./browserStorage.opfs.js";

const databaseCache = new Map();
const containerStoreName = "containers";
const blobStoreName = "blobs";

function requestToPromise(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error("IndexedDB request failed."));
  });
}

function transactionCompletion(transaction) {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error ?? new Error("IndexedDB transaction failed."));
    transaction.onabort = () => reject(transaction.error ?? new Error("IndexedDB transaction aborted."));
  });
}

async function openDatabase(databaseName) {
  if (databaseCache.has(databaseName)) {
    return databaseCache.get(databaseName);
  }

  const database = await new Promise((resolve, reject) => {
    const request = indexedDB.open(databaseName, 1);

    request.onupgradeneeded = () => {
      const db = request.result;

      if (!db.objectStoreNames.contains(containerStoreName)) {
        db.createObjectStore(containerStoreName, { keyPath: "name" });
      }

      if (!db.objectStoreNames.contains(blobStoreName)) {
        const blobStore = db.createObjectStore(blobStoreName, { keyPath: "key" });
        blobStore.createIndex("byContainer", "container", { unique: false });
      }

    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error("Failed to open IndexedDB."));
  });

  database.onclose = () => databaseCache.delete(databaseName);
  databaseCache.set(databaseName, database);
  return database;
}

async function getBlobsForContainer(databaseName, containerName) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(blobStoreName, "readonly");
  const store = transaction.objectStore(blobStoreName);
  const index = store.index("byContainer");
  const blobs = await requestToPromise(index.getAll(containerName));
  await transactionCompletion(transaction);
  return Array.isArray(blobs) ? blobs : [];
}

async function getAllBlobs(databaseName) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(blobStoreName, "readonly");
  const blobs = await requestToPromise(transaction.objectStore(blobStoreName).getAll());
  await transactionCompletion(transaction);
  return Array.isArray(blobs) ? blobs : [];
}

async function opfsFileExists(databaseName, blobKey) {
  return await opfsFileExistsInternal(databaseName, blobKey);
}

function resolvePayloadKey(blob) {
  return blob?.payloadKey ?? blob?.key ?? null;
}

export async function containerExists(databaseName, containerName) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(containerStoreName, "readonly");
  const store = transaction.objectStore(containerStoreName);
  const container = await requestToPromise(store.get(containerName));
  await transactionCompletion(transaction);
  return container !== undefined;
}

export async function createContainer(databaseName, containerName) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(containerStoreName, "readwrite");
  transaction.objectStore(containerStoreName).put({
    name: containerName,
    createdOn: new Date().toISOString()
  });
  await transactionCompletion(transaction);
}

export async function removeContainer(databaseName, containerName) {
  const blobs = await getBlobsForContainer(databaseName, containerName);

  for (const blob of blobs) {
    await deleteBlob(databaseName, blob.key);
  }

  const database = await openDatabase(databaseName);
  const transaction = database.transaction(containerStoreName, "readwrite");
  const containerStore = transaction.objectStore(containerStoreName);
  containerStore.delete(containerName);
  await transactionCompletion(transaction);
}

export async function getBlob(databaseName, blobKey) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(blobStoreName, "readonly");
  const store = transaction.objectStore(blobStoreName);
  const blob = await requestToPromise(store.get(blobKey));
  await transactionCompletion(transaction);
  return blob ?? null;
}

export async function listBlobs(databaseName, containerName, prefix) {
  const blobs = await getBlobsForContainer(databaseName, containerName);
  const resolvedPrefix = prefix ?? `${containerName}::`;

  return blobs
    .filter((blob) => blob.key.startsWith(resolvedPrefix))
    .sort((left, right) => left.key.localeCompare(right.key));
}

export async function putBlob(databaseName, blob) {
  const database = await openDatabase(databaseName);
  const transaction = database.transaction(blobStoreName, "readwrite");
  transaction.objectStore(blobStoreName).put(blob);
  await transactionCompletion(transaction);
}

export async function deleteBlob(databaseName, blobKey) {
  const existingBlob = await getBlob(databaseName, blobKey);
  if (!existingBlob) {
    return false;
  }

  const database = await openDatabase(databaseName);
  const transaction = database.transaction(blobStoreName, "readwrite");
  const blobStore = transaction.objectStore(blobStoreName);

  blobStore.delete(blobKey);
  await transactionCompletion(transaction);
  const payloadKey = resolvePayloadKey(existingBlob);
  if (payloadKey) {
    await tryDeletePayloadFileInternal(databaseName, payloadKey);
  }

  return true;
}

export async function deletePayloadFile(databaseName, payloadKey) {
  return await tryDeletePayloadFileInternal(databaseName, payloadKey);
}

export async function deleteByPrefix(databaseName, containerName, prefix) {
  const blobs = await listBlobs(databaseName, containerName, prefix);
  for (const blob of blobs) {
    await deleteBlob(databaseName, blob.key);
  }

  return blobs.length;
}

export async function beginPayloadWrite(databaseName, blobKey) {
  return await beginPayloadWriteInternal(databaseName, blobKey);
}

export async function appendPayloadChunks(databaseName, blobKey, chunks) {
  await appendPayloadChunksInternal(databaseName, blobKey, chunks);
}

export async function completePayloadWrite(databaseName, blobKey) {
  await completePayloadWriteInternal(databaseName, blobKey);
}

export async function abortPayloadWrite(databaseName, blobKey) {
  await abortPayloadWriteInternal(databaseName, blobKey);
}

export async function readPayloadRange(databaseName, blobKey, offset, count) {
  return await readPayloadRangeInternal(databaseName, blobKey, offset, count);
}

export async function getPayloadStoreByFullName(databaseName, fullName) {
  const blob = (await getAllBlobs(databaseName)).find((item) => item.fullName === fullName);
  if (!blob) {
    return null;
  }

  const payloadKey = resolvePayloadKey(blob);
  if (blob.payloadStore === "opfs" && payloadKey && await opfsFileExists(databaseName, payloadKey)) {
    return "opfs";
  }

  return null;
}

const browserStorageApi = {
  containerExists,
  createContainer,
  removeContainer,
  getBlob,
  listBlobs,
  putBlob,
  deleteBlob,
  deletePayloadFile,
  deleteByPrefix,
  beginPayloadWrite,
  appendPayloadChunks,
  completePayloadWrite,
  abortPayloadWrite,
  readPayloadRange,
  getPayloadStoreByFullName
};

if (typeof window !== "undefined") {
  window.ManagedCodeStorageBrowser = browserStorageApi;
}
