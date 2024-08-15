// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class DisposableLock_Tests
{
    [Fact]
    public void AcquireLock_Works()
    {
        DisposableLock sut = new();

        sut.IsLocked().Should().BeFalse();
        using (sut.AcquireLock())
        {
            sut.IsLocked().Should().BeTrue();
        }

        sut.IsLocked().Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_Works()
    {
        DisposableLock sut = new();

        sut.IsLocked().Should().BeFalse();
        using (await sut.AcquireLockAsync())
        {
            sut.IsLocked().Should().BeTrue();
        }

        sut.IsLocked().Should().BeFalse();
    }
}
