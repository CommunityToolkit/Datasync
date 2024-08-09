// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OperationsQueueManager_Tests : BaseTest
{
    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        Action act = () => sut.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion
}
