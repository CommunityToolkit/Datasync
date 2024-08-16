// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Threading;

/// <summary>
/// A class for handling a dictionary of locks.
/// </summary>
internal class AsyncLockDictionary
{
    /// <summary>
    /// The dictionary of locks.
    /// </summary>
    private readonly ConcurrentDictionary<string, LockEntry> locks = [];

    /// <summary>
    /// Acquire a lock (asynchronously) from the dictionary of locks.
    /// </summary>
    /// <param name="key">The key for the lock.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="IDisposable"/> to release the lock when done.</returns>
    public async Task<IDisposable> AcquireLockAsync(string key, CancellationToken cancellationToken = default)
    {
        LockEntry entry = this.locks.GetOrAdd(key, _ => new LockEntry());
        _ = entry.IncrementCount();

        IDisposable releaser = await entry.Lock.AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        return new DisposeAction(() =>
        {
            releaser.Dispose();
            if (entry.DecrementCount() == 0)
            {
                _ = this.locks.TryRemove(key, out _);
                entry.Dispose();
            }
        });
    }

    /// <summary>
    /// Acquire a lock (asynchronously) from the dictionary of locks.
    /// </summary>
    /// <param name="key">The key for the lock.</param>
    /// <returns>An <see cref="IDisposable"/> to release the lock when done.</returns>
    public IDisposable AcquireLock(string key)
    {
        LockEntry entry = this.locks.GetOrAdd(key, _ => new LockEntry());
        _ = entry.IncrementCount();

        IDisposable releaser = entry.Lock.AcquireLock();
        return new DisposeAction(() =>
        {
            releaser.Dispose();
            if (entry.DecrementCount() == 0)
            {
                _ = this.locks.TryRemove(key, out _);
                entry.Dispose();
            }
        });
    }

    /// <summary>
    /// Determines if the specific lock referenced by key is locked now.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>true if locked; false otherwise.</returns>
    public bool IsLocked(string key)
    {
        if (this.locks.TryGetValue(key, out LockEntry? entry))
        {
            return entry.IsLocked();
        }

        return false;
    }

    /// <summary>
    /// A single entry in the dictionary of locks.
    /// </summary>
    private sealed class LockEntry : IDisposable
    {
        private int _count = 0;

        public DisposableLock Lock { get; } = new();

        public int IncrementCount()
            => Interlocked.Increment(ref this._count);

        public int DecrementCount()
            => Interlocked.Decrement(ref this._count);

        internal bool IsLocked()
            => Lock.IsLocked();

        public void Dispose()
        {
            Lock.Dispose();
        }
    }
}
