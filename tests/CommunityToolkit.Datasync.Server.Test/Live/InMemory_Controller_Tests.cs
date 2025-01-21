// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.InMemory;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class InMemory_Controller_Tests : LiveControllerTests<InMemoryMovie>
{
    #region Setup
    private readonly Random random = new();
    private InMemoryRepository<InMemoryMovie> repository;

    protected override string DriverName { get; } = "InMemory";

    protected override Task<InMemoryMovie> GetEntityAsync(string id)
        => Task.FromResult(this.repository.GetEntity(id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.repository.GetEntities().Count);

    protected override Task<IRepository<InMemoryMovie>> GetPopulatedRepositoryAsync()
    {
        this.repository = new InMemoryRepository<InMemoryMovie>(TestCommon.TestData.Movies.OfType<InMemoryMovie>());
        return Task.FromResult<IRepository<InMemoryMovie>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.repository.GetEntities()[this.random.Next(this.repository.GetEntities().Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
