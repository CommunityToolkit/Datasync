// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using Microsoft.Spatial;
using System.Net;
using System.Net.Http.Json;
using System.Text;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Server.Test.Service;

[ExcludeFromCodeCoverage]
public class Create_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Theory]
    [InlineData(null)]
    [InlineData("de76a422-7fb0-4f1f-9bb4-12b3c7882541")]
    public async Task Create_WithValidInput_Returns201(string id)
    {
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = id };

        HttpResponseMessage response = await this.client.PostAsJsonAsync(this.factory.MovieEndpoint, source, this.serializerOptions);
        response.Should().HaveStatusCode(HttpStatusCode.Created);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveChangedMetadata(id, this.StartTime).And.BeEquivalentTo<IMovie>(source);

        InMemoryMovie inMemoryMovie = this.factory.GetServerEntityById<InMemoryMovie>(clientMovie.Id);
        clientMovie.Should().HaveEquivalentMetadataTo(inMemoryMovie).And.BeEquivalentTo<IMovie>(inMemoryMovie);

        response.Headers.Location.Should().Be($"{this.client.BaseAddress}{this.factory.MovieEndpoint}/{clientMovie.Id}");
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Fact]
    public async Task Create_ExistingId_Returns409()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };

        HttpResponseMessage response = await this.client.PostAsJsonAsync(this.factory.MovieEndpoint, source, this.serializerOptions);
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);

        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Fact]
    public async Task Create_SoftDeleted_Returns409()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        ClientMovie source = new(TestData.Movies.BlackPanther) { Id = existingMovie.Id };

        HttpResponseMessage response = await this.client.PostAsJsonAsync(this.factory.MovieEndpoint, source, this.serializerOptions);
        response.Should().HaveStatusCode(HttpStatusCode.Conflict);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);

        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
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

        HttpResponseMessage response = await this.client.PostAsJsonAsync(this.factory.KitchenSinkEndpoint, source, this.serializerOptions);
        response.Should().HaveStatusCode(HttpStatusCode.Created);

        ClientKitchenSink clientKitchenSink = await response.Content.ReadFromJsonAsync<ClientKitchenSink>(this.serializerOptions);
        clientKitchenSink.Should().NotBeNull().And.HaveChangedMetadata(id, this.StartTime).And.BeEquivalentTo<IKitchenSink>(source);

        InMemoryKitchenSink serverEntity = this.factory.GetServerEntityById<InMemoryKitchenSink>(id);
        serverEntity.Should().BeEquivalentTo<IKitchenSink>(source);
    }

    [Fact]
    public async Task Create_NonJsonData_Returns415()
    {
        const string content = "<html><body><h1>Not JSON</h1></body></html>";
        HttpResponseMessage response = await this.client.PostAsync(this.factory.MovieEndpoint, new StringContent(content, Encoding.UTF8, "text/html"));
        response.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);
    }

    [Theory]
    [InlineData("duration", 50)]
    [InlineData("duration", 370)]
    [InlineData("duration", null)]
    [InlineData("title", "a")]
    [InlineData("title", "Lorem ipsum dolor sit amet, consectetur adipiscing elit accumsan.")]
    [InlineData("title", null)]
    [InlineData("year", 1900)]
    [InlineData("year", 2035)]
    [InlineData("year", null)]
    public async Task Create_ValidationError_Returns400(string propName, object propValue)
    {
        Dictionary<string, object> source = new()
        {
            { "id", "test-id" },
            { "updatedAt", DateTimeOffset.Parse("2018-12-31T01:01:01.000Z") },
            { "version", Convert.ToBase64String(Guid.NewGuid().ToByteArray()) },
            { "bestPictureWinner", false },
            { "duration", 120 },
            { "rating", "G" },
            { "releaseDate", DateOnly.Parse("2018-12-30") },
            { "title", "Home Video" },
            { "year", 2021 }
        };
        if (propValue == null)
        {
            source.Remove(propName);
        }
        else
        {
            source[propName] = propValue;
        }

        HttpResponseMessage response = await this.client.PostAsJsonAsync(this.factory.MovieEndpoint, source, this.serializerOptions);
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }
}
