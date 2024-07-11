// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Get_ServiceTests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task Read_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie clientMovie = await MovieDataset.GetAsync(existingMovie.Id, DefaultOperationOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Read_CanRoundtripTypes()
    {
        InMemoryKitchenSink storedEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UpdatedAt = DateTimeOffset.Now.AddDays(-1),
            Version = Guid.NewGuid().ToByteArray(),
            Deleted = false,
            StringValue = "state=none",
            EnumValue = KitchenSinkState.Completed,
            DateOnlyValue = new DateOnly(2023, 12, 15),
            TimeOnlyValue = new TimeOnly(9, 52, 35)
        };
        this.factory.Store(storedEntity);

        ClientKitchenSink clientEntity = await KitchenSinkDataset.GetAsync(storedEntity.Id, DefaultOperationOptions);
        clientEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(storedEntity).And.BeEquivalentTo<IKitchenSink>(storedEntity);
    }

    [Fact]
    public async Task Read_MissingId_Returns404()
    {
        Func<Task> act = async () => await MovieDataset.GetAsync("missing", DefaultOperationOptions);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Read_SoftDeleted_NotDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie clientMovie = await SoftDeletedMovieDataset.GetAsync(existingMovie.Id, DefaultOperationOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        Func<Task> act = async () => await SoftDeletedMovieDataset.GetAsync(existingMovie.Id, DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>()).Which.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_WithIncludeDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);

        RemoteOperationOptions options = new() { IncludeDeletedItems = true };
        ClientMovie clientMovie = await SoftDeletedMovieDataset.GetAsync(existingMovie.Id, options);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }
}
