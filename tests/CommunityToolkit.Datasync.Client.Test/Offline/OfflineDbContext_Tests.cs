// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests : BaseTest
{
    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext sut = CreateContext();
        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_bool_Works()
    {
        TestDbContext sut = CreateContext();
        sut.TestDispose(false); // Doesn't dispose the underlying thing
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        TestDbContext sut = CreateContext();
        Action act = () => sut.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion
}
