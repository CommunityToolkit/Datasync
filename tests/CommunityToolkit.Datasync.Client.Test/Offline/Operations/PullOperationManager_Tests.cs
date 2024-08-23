// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Operations;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Operations;

[ExcludeFromCodeCoverage]
public class PullOperationManager_Tests : BaseTest
{
    private TestDbContext context = CreateContext();

    private PullOperationManager operationManager { get => this.context.PullOperationManager as PullOperationManager; }

    [Fact]
    public void Ctor_SetsValues()
    {
        operationManager.Should().NotBeNull();

        operationManager.DeltaTokenStore.Should().NotBeNull();
        operationManager.SynchronizableTypes.Should().BeEquivalentTo([typeof(ClientMovie), typeof(Entity3)]);
    }
}
