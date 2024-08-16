// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class AsyncLockDictionary_Tests
{
    [Fact]
    public void AcquireLock_Works()
    {
        AsyncLockDictionary sut = new();

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();

        using (sut.AcquireLock("key1"))
        {
            sut.IsLocked("key1").Should().BeTrue();
            sut.IsLocked("key2").Should().BeFalse();
        }

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_Works()
    {
        AsyncLockDictionary sut = new();

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();

        using (await sut.AcquireLockAsync("key1"))
        {
            sut.IsLocked("key1").Should().BeTrue();
            sut.IsLocked("key2").Should().BeFalse();
        }

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();
    }
}
