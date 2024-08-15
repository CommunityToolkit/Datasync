// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class ExecutableOperation_Tests
{
    [Fact]
    public async Task CreateAsync_InvalidKind()
    {
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = (OperationKind)9999,
            State = OperationState.Pending,
            EntityType = string.Empty,
            ItemId = string.Empty,
            Item = string.Empty,
            Sequence = 0,
            Version = 0
        };

        Func<Task> act = async () => _ = await ExecutableOperation.CreateAsync(op);
        await act.Should().ThrowAsync<DatasyncException>();
    }
}
