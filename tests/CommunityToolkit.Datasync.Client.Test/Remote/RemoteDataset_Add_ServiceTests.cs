// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using FluentAssertions.Specialized;
using Microsoft.Spatial;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Add_ServiceTests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Theory]
    [InlineData(null)]
    [InlineData("de76a422-7fb0-4f1f-9bb4-12b3c7882541")]
    public async Task AddAsync_WithValidInput_Works(string id)
    {
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = id };

        ClientMovie clientMovie = await MovieDataset.AddAsync(source, DefaultOperationOptions);

        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(id, this.StartTime).And.BeEquivalentTo<IMovie>(source);

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
    }

    [Fact]
    public async Task AddAsync_ExistingId_Conflict()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };

        Func<Task> act = async () => await MovieDataset.AddAsync(source, DefaultOperationOptions);

        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task AddAsync_SoftDeleted_Conflict()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };

        Func<Task> act = async () => await MovieDataset.AddAsync(source, DefaultOperationOptions);

        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task AddAsync_CanRoundtrip_Types()
    {
        const string id = "ks01";
        ClientKitchenSink source = new()
        {
            Id = id,
            StringValue = "state=none",
            EnumValue = KitchenSinkState.None,
            DateOnlyValue = new DateOnly(2023, 12, 15),
            TimeOnlyValue = new TimeOnly(9, 52, 35),
            PointValue = GeographyPoint.Create(-122.333056, 47.609722)
        };

        ClientKitchenSink clientKitchenSink = await KitchenSinkDataset.AddAsync(source, DefaultOperationOptions);
        clientKitchenSink.Should().NotBeNull().And.HaveChangedMetadata(id, this.StartTime).And.BeEquivalentTo<IKitchenSink>(source);

        InMemoryKitchenSink serverEntity = this.factory.GetServerEntityById<InMemoryKitchenSink>(id);
        serverEntity.Should().BeEquivalentTo<IKitchenSink>(source);
    }
}
