// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Threading;

/// <summary>
/// A class for handling a dictionary of locks.
/// </summary>
internal class AsyncLockDictionary
{
    /// <summary>
    /// The dictionary of locks.  All access (including the reference counts of the
    /// entries) must be done while holding <see cref="gate"/>.
    /// </summary>
    private readonly Dictionary<string, LockEntry> locks = [];

    /// <summary>
    /// Guards <see cref="locks"/> and the reference count of each entry, so that looking
    /// up an entry, taking a reference, dropping a reference, and removing/disposing an
    /// entry are atomic with respect to each other.  This is never held across an await.
    /// </summary>
    private readonly object gate = new();

    /// <summary>
    /// Acquire a lock (asynchronously) from the dictionary of locks.
    /// </summary>
    /// <param name="key">The key for the lock.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="IDisposable"/> to release the lock when done.</returns>
    public async Task<IDisposable> AcquireLockAsync(string key, CancellationToken cancellationToken = default)
    {
        LockEntry entry;
        lock (this.gate)
        {
            if (!this.locks.TryGetValue(key, out LockEntry? existing))
            {
                existing = new LockEntry();
                this.locks[key] = existing;
            }

            entry = existing;
            entry.Count++;
        }

        IDisposable releaser;
        try
        {
            releaser = await entry.Lock.AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The wait failed (e.g. cancelled), so give back the reference we took above.
            ReleaseEntry(key, entry);
            throw;
        }

        return new DisposeAction(() =>
        {
            // Release the semaphore first, then drop the reference; the entry can only be
            // disposed once nobody holds or is waiting on it.
            releaser.Dispose();
            ReleaseEntry(key, entry);
        });
    }

    /// <summary>
    /// Determines if the specific lock referenced by key is locked now.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>true if locked; false otherwise.</returns>
    public bool IsLocked(string key)
    {
        lock (this.gate)
        {
            return this.locks.TryGetValue(key, out LockEntry? entry) && entry.IsLocked();
        }
    }

    /// <summary>
    /// Drops a reference to an entry, removing and disposing it when it is no longer in use.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <param name="entry">The entry.</param>
    private void ReleaseEntry(string key, LockEntry entry)
    {
        lock (this.gate)
        {
            entry.Count--;
            if (entry.Count == 0)
            {
                _ = this.locks.Remove(key);
                entry.Dispose();
            }
        }
    }

    /// <summary>
    /// A single entry in the dictionary of locks.
    /// </summary>
    private sealed class LockEntry : IDisposable
    {
        /// <summary>
        /// The number of callers holding or waiting on the lock.  Only accessed under the dictionary's gate.
        /// </summary>
        public int Count { get; set; }

        public DisposableLock Lock { get; } = new();

        internal bool IsLocked()
            => Lock.IsLocked();

        public void Dispose()
        {
            Lock.Dispose();
        }
    }
}
