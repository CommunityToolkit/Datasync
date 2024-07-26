// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public abstract class Disposable : IDisposable
{
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: dispose managed state (managed objects)
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
