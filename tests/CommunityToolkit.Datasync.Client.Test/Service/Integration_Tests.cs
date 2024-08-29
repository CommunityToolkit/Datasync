// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.Spatial;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Service;

/// <summary>
/// A set of tests that use the online client and an actual server
/// </summary>
/// <param name="factory"></param>
[ExcludeFromCodeCoverage]
public class Integration_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    #region AddAsync
    [Theory]
    [InlineData(null)]
    [InlineData("de76a422-7fb0-4f1f-9bb4-12b3c7882541")]
    public async Task Create_WithValidInput_Returns201(string id)
    {
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = id };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        ServiceResponse<ClientMovie> response = await client.AddAsync(source);
        InMemoryMovie inMemoryMovie = GetServerEntityById<InMemoryMovie>(response.Value.Id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(201);
        response.HasValue.Should().BeTrue();

        response.Value.Should().NotBeNull().And.HaveChangedMetadata(id, StartTime).And.BeEquivalentTo<IMovie>(source);
        response.Value.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }

    [Fact]
    public async Task Create_ExistingId_Returns409()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        Func<Task> act = async () => _ = await client.AddAsync(source);
        ConflictException<ClientMovie> ex = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.First();

        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.StatusCode.Should().Be(409);

        ex.ClientEntity.Should().BeEquivalentTo(source);
        ex.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Create_SoftDeleted_Returns409()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        SoftDelete(existingMovie);
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        Func<Task> act = async () => _ = await client.AddAsync(source);
        ConflictException<ClientMovie> ex = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.First();

        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.StatusCode.Should().Be(409);

        ex.ClientEntity.Should().BeEquivalentTo(source);
        ex.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Create_CanRoundtrip_Types()
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
        DatasyncServiceClient<ClientKitchenSink> client = GetKitchenSinkClient();

        ServiceResponse<ClientKitchenSink> response = await client.AddAsync(source);
        InMemoryKitchenSink serverEntity = GetServerEntityById<InMemoryKitchenSink>(id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(201);
        response.HasValue.Should().BeTrue();

        response.Value.Should().NotBeNull().And.HaveChangedMetadata(id, StartTime).And.BeEquivalentTo<IKitchenSink>(source);
        serverEntity.Should().BeEquivalentTo<IKitchenSink>(source);
    }
    #endregion

    #region GetAsync
    [Fact]
    public async Task Read_Returns200()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        ServiceResponse<ClientMovie> response = await client.GetAsync(existingMovie.Id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();

        response.Value.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }

    [Fact]
    public async Task Read_MissingId_Returns404()
    {
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };

        ServiceResponse response = await client.GetAsync("id-is-missing", options);

        response.IsSuccessful.Should().BeFalse();
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Read_MissingId_Returns404_ThrowIfMissing()
    {
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = true };

        Func<Task> act = async () => _ = await client.GetAsync("id-is-missing", options);
        await act.Should().ThrowAsync<EntityDoesNotExistException>();
    }

    [Fact]
    public async Task Read_SoftDeleted_NotDeleted_Returns200()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        DatasyncServiceClient<ClientMovie> client = GetSoftDeletedMovieClient();

        ServiceResponse<ClientMovie> response = await client.GetAsync(existingMovie.Id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();

        response.Value.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_Returns404()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        SoftDelete(existingMovie);

        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = true };

        Func<Task> act = async () => _ = await client.GetAsync("id-is-missing", options);
        await act.Should().ThrowAsync<EntityDoesNotExistException>();
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_WithIncludeDeleted_Returns200()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        SoftDelete(existingMovie);
        DatasyncServiceClient<ClientMovie> client = GetSoftDeletedMovieClient();
        DatasyncServiceOptions options = new() { IncludeDeleted = true };

        ServiceResponse<ClientMovie> response = await client.GetAsync(existingMovie.Id, options);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();

        response.Value.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }
    #endregion

    #region RemoveAsync
    [Fact]
    public async Task Delete_ById_Returns204()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        ServiceResponse response = await client.RemoveAsync(existingMovie.Id, new DatasyncServiceOptions());

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(204);
        response.HasContent.Should().BeFalse();

        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();

    }

    [Fact]
    public async Task Delete_WithVersioning_Works()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        string version = Convert.ToBase64String(existingMovie.Version);
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { Version = version };

        ServiceResponse response = await client.RemoveAsync(existingMovie.Id, options);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(204);
        response.HasContent.Should().BeFalse();

        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithVersioning_Conflict()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        string version = "dGVzdA==";
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { Version = version };

        Func<Task> act = async () => _ = await client.RemoveAsync(existingMovie.Id, options);
        ConflictException<ClientMovie> ex = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.First();

        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.StatusCode.Should().Be(412);

        ex.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);

        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull().And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Delete_MissingId_Returns404()
    {
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };

        ServiceResponse response = await client.RemoveAsync("id-is-missing", options);

        response.IsSuccessful.Should().BeFalse();
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_MissingId_Returns404_ThrowIfMissing()
    {
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = true };

        Func<Task> act = async () => _ = await client.RemoveAsync("id-is-missing", options);
        await act.Should().ThrowAsync<EntityDoesNotExistException>();
    }

    [Fact]
    public async Task Delete_NotSoftDeleted_Returns204()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        byte[] existingVersion = [.. existingMovie.Version];
        DateTimeOffset existingUpdatedAt = (DateTimeOffset)existingMovie.UpdatedAt;
        DatasyncServiceClient<ClientMovie> client = GetSoftDeletedMovieClient();
        DatasyncServiceOptions options = new();

        ServiceResponse response = await client.RemoveAsync(existingMovie.Id, options);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(204);
        response.HasContent.Should().BeFalse();

        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
        serverEntity.UpdatedAt.Should().NotBe(existingUpdatedAt).And.BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        serverEntity.Version.Should().NotBeEquivalentTo(existingVersion);
        serverEntity.Deleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SoftDeletedId_Returns410()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        SoftDelete(existingMovie);
        DatasyncServiceClient<ClientMovie> client = GetSoftDeletedMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };

        Func<Task> act = async () => _ = await client.RemoveAsync(existingMovie.Id, options);
        (await act.Should().ThrowAsync<DatasyncHttpException>()).Which.ServiceResponse.StatusCode.Should().Be(410);

        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
    }
    #endregion

    #region ReplaceAsync
    [Fact]
    public async Task Replace_Returns200()
    {
        ClientMovie existingMovie = new(GetRandomMovie()) { Title = "New Title" };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new();

        ServiceResponse<ClientMovie> response = await client.ReplaceAsync(existingMovie, options);
        InMemoryMovie inMemoryMovie = GetServerEntityById<InMemoryMovie>(response.Value.Id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();

        response.Value.Should().NotBeNull().And.HaveChangedMetadata(existingMovie.Id, StartTime).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Value.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }

    [Fact]
    public async Task Replace_WithVersioning_Works()
    {
        ClientMovie existingMovie = new(GetRandomMovie()) { Title = "New Title" };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        ServiceResponse<ClientMovie> response = await client.ReplaceAsync(existingMovie);
        InMemoryMovie inMemoryMovie = GetServerEntityById<InMemoryMovie>(response.Value.Id);

        response.IsSuccessful.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.HasValue.Should().BeTrue();

        response.Value.Should().NotBeNull().And.HaveChangedMetadata(existingMovie.Id, StartTime).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Value.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
        response.Headers.Should().Contain("ETag", $"\"{response.Value.Version}\"");
    }

    [Fact]
    public async Task Replace_WithVersioning_Conflict()
    {
        ClientMovie existingMovie = new(GetRandomMovie()) { Title = "New Title" };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { Version = "dGVzdA==" };
        InMemoryMovie serverEntity = GetServerEntityById<InMemoryMovie>(existingMovie.Id);

        Func<Task> act = async () => _ = await client.ReplaceAsync(existingMovie, options);
        ConflictException<ClientMovie> ex = (await act.Should().ThrowAsync<ConflictException<ClientMovie>>()).Subject.First();

        ex.ServiceResponse.IsSuccessful.Should().BeFalse();
        ex.ServiceResponse.IsConflictStatusCode.Should().BeTrue();
        ex.ServiceResponse.StatusCode.Should().Be(412);

        ex.ClientEntity.Should().BeEquivalentTo(existingMovie);
        ex.ServerEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(serverEntity).And.BeEquivalentTo<IMovie>(serverEntity);
    }

    [Fact]
    public async Task Replace_MissingId_Returns404()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        ClientMovie source = new(existingMovie) { Id = "missing" };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = false };

        ServiceResponse response = await client.ReplaceAsync(source, options);

        response.IsSuccessful.Should().BeFalse();
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Replace_MissingId_Returns404_ThrowIfMissing()
    {
        InMemoryMovie existingMovie = GetRandomMovie();
        ClientMovie source = new(existingMovie) { Id = "missing" };
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();
        DatasyncServiceOptions options = new() { ThrowIfMissing = true };

        Func<Task> act = async () => _ = await client.ReplaceAsync(source, options);
        await act.Should().ThrowAsync<EntityDoesNotExistException>();
    }
    #endregion
}
