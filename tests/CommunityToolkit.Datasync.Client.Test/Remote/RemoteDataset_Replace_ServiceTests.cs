// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using FluentAssertions.Specialized;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Replace_ServiceTests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Theory, CombinatorialData]
    public async Task Replace_Returns200(bool includePrecondition)
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie()) { Title = "New Title" };
        RemoteOperationOptions options = new() { RequiredVersion = includePrecondition ? existingMovie.Version : null };

        ClientMovie clientMovie = await MovieDataset.ReplaceAsync(existingMovie, options);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
    }

    [Fact]
    public async Task Replace_Conflict()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie replacement = new(existingMovie) { Title = "New Title" };
        RemoteOperationOptions options = new() { RequiredVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) };

        Func<Task> act = async () => await MovieDataset.ReplaceAsync(replacement, options);
        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.ServerEntity.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Replace_MissingId_Returns404()
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie()) { Id = "missing", Title = "New Title" };
        Func<Task> act = async () => await MovieDataset.ReplaceAsync(existingMovie, DefaultOperationOptions);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Replace_NotSoftDeleted_Works()
    {
        ClientMovie existingMovie = new(this.factory.GetRandomMovie()) { Title = "New Title" };

        ClientMovie clientMovie = await SoftDeletedMovieDataset.ReplaceAsync(existingMovie, DefaultOperationOptions);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
    }

    [Fact]
    public async Task Replace_SoftDeleted_NotUndeleting_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        ClientMovie submission = new(existingMovie) { Title = "New Title" };
        Func<Task> act = async () => await SoftDeletedMovieDataset.ReplaceAsync(submission, DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>()).Which.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Replace_SoftDeleted_Undeleting_WithShowDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        ClientMovie submission = new(existingMovie) { Deleted = false };
        RemoteOperationOptions options = new() { IncludeDeletedItems = true };

        ClientMovie clientMovie = await SoftDeletedMovieDataset.ReplaceAsync(submission, options);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(existingMovie, this.StartTime).And.BeEquivalentTo<IMovie>(existingMovie);
        clientMovie.Deleted.Should().BeFalse();

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);
    }
}
