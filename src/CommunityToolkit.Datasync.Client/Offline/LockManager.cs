// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// A manager for disposable locks that are used throughout the offline
/// synchronization process to ensure two threads don't work on the same
/// data at the same time.
/// </summary>
/// <summary>
/// A class for handling a dictionary of locks.
/// </summary>
internal static class LockManager
{
    /// <summary>
    /// The name of the shared synchronization lock.
    /// </summary>
    internal const string synchronizationLockName = "synclock";

    /// <summary>
    /// The underlying async lock dictionary to use.
    /// </summary>
    internal static Lazy<AsyncLockDictionary> lockDictionary = new(() => new AsyncLockDictionary());

    /// <summary>
    /// Acquire a disposable lock from the dictionary of locks.
    /// </summary>
    /// <param name="key">The key to the lock.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to release the lock.</returns>
    internal static Task<IDisposable> AcquireLockAsync(string key, CancellationToken cancellationToken)
        => lockDictionary.Value.AcquireLockAsync(key, cancellationToken);
}