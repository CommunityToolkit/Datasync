// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Threading;

/// <summary>
/// An <see cref="IDisposable"/> that runs an action on disposing.
/// </summary>
/// <remarks>
/// This is most often used to release an asynchronous lock when disposed.
/// </remarks>
internal struct DisposeAction(Action action) : IDisposable
{
    bool isDisposed = false;

    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;
        action();
    }
}
