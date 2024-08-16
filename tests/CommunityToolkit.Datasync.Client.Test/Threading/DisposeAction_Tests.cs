// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class DisposeAction_Tests
{
    [Fact]
    public void DisposeAction_CallsAction_OnDispose()
    {
        bool isCalled = false;
        using (DisposeAction sut = new(() => isCalled = true))
        {
            Assert.False(isCalled);
        }

        Assert.True(isCalled);
    }

    [Fact]
    public void DisposeAction_CanDisposeTwice()
    {
        int isCalled = 0;
        using (DisposeAction sut = new(() => isCalled++))
        {
            Assert.Equal(0, isCalled);
            sut.Dispose();
            Assert.Equal(1, isCalled);
        }

        Assert.Equal(1, isCalled);
    }
}
