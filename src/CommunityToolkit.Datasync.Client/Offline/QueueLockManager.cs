// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// This is a singleton lock manager class that is used to manage the interlocks for
/// the operations queue.  It ensures that only one thread can access a single entity
/// in the queue at any given time (and that any other threads are blocked until the
/// queue entity is available).
/// </summary>
internal class QueueLockManager
{
    private static readonly Lazy<QueueLockManager> _instance = new(() => new QueueLockManager());

    /// <summary>
    /// Acquires a lock on the queue for the given entity type/ID.  This will block until the
    /// queue entity is available.  When the processing is complete, the lock will be released
    /// as part of the dispose pattern.
    /// </summary>
    /// <param name="entityName">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to release the lock.</returns>
    internal static IDisposable AcquireLock(string entityName, string entityId)
        => _instance.Value.AcquireLockInternal(entityName, entityId);

    #region Internal Implementation
    internal readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    internal QueueLockManager()
    {
    }

    /// <summary>
    /// Acquires the lock on the queue for the given entity type/ID.  This will block until the
    /// queue entity is available.  When the processing is complete, the lock will be released
    /// as part of the dispose pattern.
    /// </summary>
    /// <param name="entityName">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to release the lock.</returns>
    internal IDisposable AcquireLockInternal(string entityName, string entityId)
    {
        string key = $"{entityName}:#:{entityId}";
        if (!this._locks.TryGetValue(key, out _))
        {
            this._locks.TryAdd(key, new SemaphoreSlim(1, 1));
        }

        return new LockEntry(key);
    }
    #endregion

    #region LockEntry
    internal class LockEntry : IDisposable
    {
        private readonly string _key;

        internal LockEntry(string key)
        {
            this._key = key;
            _instance.Value._locks[key].Wait();
        }

        public void Dispose()
        {
            _instance.Value._locks[this._key].Release();
        }
    }
    #endregion
}
