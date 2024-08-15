// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Threading;

/// <summary>
/// A lock class that can be disposed.
/// </summary>
internal class DisposableLock : IDisposable
{
    /// <summary>
    /// The underlying lock.
    /// </summary>
    private readonly SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// Acquire the lock.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> to release the lock when the lock is acquired.</returns>
    public IDisposable AcquireLock()
    {
        this.semaphore.Wait();
        return new DisposeAction(() => this.semaphore.Release());
    }

    /// <summary>
    /// Acquire the lock.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that returns an <see cref="IDisposable"/> to release the lock when the lock is acquired.</returns>
    public async Task<IDisposable> AcquireLockAsync(CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new DisposeAction(() => this.semaphore.Release());
    }

    /// <summary>
    /// Determines if the disposable lock is locked or not.
    /// </summary>
    /// <returns>true if the disposable lock is locked; false otherwise.</returns>
    public bool IsLocked()
        => this.semaphore.CurrentCount == 0;

    /// <summary>
    /// Disposes of the lock.
    /// </summary>
    public void Dispose()
    {
        this.semaphore.Dispose();
    }
}
