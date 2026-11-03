using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Storage.Server.Hubs;

internal sealed class StorageHubTransferRegistry
{
    private readonly ConcurrentDictionary<(string ConnectionId, string TransferId), TransferRegistration> _transfers = new();
    private readonly object _admission = new();

    internal TransferRegistration? Find(string connectionId, string transferId) =>
        _transfers.GetValueOrDefault((connectionId, transferId));

    internal void Cancel(string connectionId, string transferId)
    {
        var registration = Find(connectionId, transferId);
        if (registration is not null)
            CancelRegistration(registration);
    }

    internal void CancelConnection(string connectionId)
    {
        foreach (var (_, registration) in _transfers)
        {
            if (registration.ConnectionId == connectionId)
                CancelRegistration(registration);
        }
    }

    internal TransferRegistration Register(HubCallerContext context, int maxConcurrentTransfers, string transferId,
        string operation, string resourceName, long? totalBytes, CancellationToken cancellationToken,
        UploadStreamDescriptor? descriptor = null)
    {
        lock (_admission)
        {
            if (maxConcurrentTransfers > 0 && _transfers.Count >= maxConcurrentTransfers)
                throw new HubException("Too many concurrent transfers");

            var key = (context.ConnectionId, transferId);
            if (_transfers.ContainsKey(key))
                throw new HubException("Transfer identifier already exists");

            var status = new TransferStatus
            {
                TransferId = transferId,
                Operation = operation,
                ResourceName = resourceName,
                TotalBytes = totalBytes
            };
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.ConnectionAborted, cancellationToken);
            var registration = new TransferRegistration(status, cts, context.ConnectionId) { UploadDescriptor = descriptor };
            _transfers[key] = registration;
            return registration;
        }
    }

    private void CancelRegistration(TransferRegistration registration)
    {
        registration.Status.IsCanceled = true;
        if (registration.TryRetirePendingUpload())
        {
            Remove(registration);
            return;
        }

        try
        {
            registration.Cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Completion removed and disposed the registration while cancellation was requested.
        }
    }

    internal void Remove(TransferRegistration registration)
    {
        var key = (registration.ConnectionId, registration.Status.TransferId);
        if (_transfers.TryRemove(KeyValuePair.Create(key, registration)))
            registration.Cancellation.Dispose();
    }

    internal sealed class TransferRegistration
    {
        private int _uploadStarted;

        public TransferRegistration(TransferStatus status, CancellationTokenSource cancellation, string connectionId)
        {
            Status = status;
            Cancellation = cancellation;
            ConnectionId = connectionId;
            LastTouchedUtc = DateTimeOffset.UtcNow;
        }

        public TransferStatus Status { get; }
        public CancellationTokenSource Cancellation { get; }
        public string ConnectionId { get; }
        public UploadStreamDescriptor? UploadDescriptor { get; init; }
        public DateTimeOffset LastTouchedUtc { get; private set; }

        public bool TryStartUpload()
        {
            return Interlocked.CompareExchange(ref _uploadStarted, 1, 0) == 0;
        }

        public bool TryRetirePendingUpload() =>
            UploadDescriptor is not null && Interlocked.CompareExchange(ref _uploadStarted, 2, 0) == 0;

        public void Touch()
        {
            LastTouchedUtc = DateTimeOffset.UtcNow;
        }
    }
}
