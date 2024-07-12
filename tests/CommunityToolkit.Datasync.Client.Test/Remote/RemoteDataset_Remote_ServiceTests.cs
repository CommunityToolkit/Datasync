// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using FluentAssertions.Specialized;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class RemoteDataset_Remote_ServiceTests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Theory, CombinatorialData]
    public async Task Delete_ById_Returns204(bool includePrecondition)
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();

        RemoteOperationOptions options = new() { RequiredVersion = includePrecondition ? Convert.ToBase64String(existingMovie.Version) : null };
        Func<Task> act = async () => await MovieDataset.RemoveAsync(existingMovie.Id, options);
        await act.Should().NotThrowAsync();

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ById_PreconditionFails()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        RemoteOperationOptions options = new() { RequiredVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) };
        Func<Task> act = async () => await MovieDataset.RemoveAsync(existingMovie.Id, options);
        ExceptionAssertions<ConflictException<ClientMovie>> ex = await act.Should().ThrowAsync<ConflictException<ClientMovie>>();
        ex.Which.ServerEntity.Should().HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
    }

    [Fact]
    public async Task Delete_MissingId_Returns404()
    {
        Func<Task> act = async () => await MovieDataset.RemoveAsync("missing", DefaultOperationOptions);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Delete_SoftDeletedId_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);

        Func<Task> act = async () => await SoftDeletedMovieDataset.RemoveAsync(existingMovie.Id, DefaultOperationOptions);
        (await act.Should().ThrowAsync<DatasyncHttpException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.Gone);

        InMemoryMovie serverEntity = this.factory.GetServerEntityById<InMemoryMovie>(existingMovie.Id);
        serverEntity.Should().NotBeNull();
    }
}
