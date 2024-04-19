// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Common.Test;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Spatial;

namespace CommunityToolkit.Datasync.Server.Test.Service;

[ExcludeFromCodeCoverage]
public class Read_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task Read_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.MovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Theory]
    [InlineData("If-Match", null, HttpStatusCode.OK)]
    [InlineData("If-Match", "\"dGVzdA==\"", HttpStatusCode.PreconditionFailed)]
    [InlineData("If-None-Match", null, HttpStatusCode.NotModified)]
    [InlineData("If-None-Match", "\"dGVzdA==\"", HttpStatusCode.OK)]
    public async Task Read_WithVersioning_Works(string headerName, string headerValue, HttpStatusCode expectedStatusCode)
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        HttpRequestMessage request = new(HttpMethod.Get, $"{this.factory.MovieEndpoint}/{existingMovie.Id}");
        request.Headers.Add(headerName, headerValue ?? $"\"{Convert.ToBase64String(existingMovie.Version)}\"");
        HttpResponseMessage response = await this.client.SendAsync(request);
        response.Should().HaveStatusCode(expectedStatusCode);

        if (expectedStatusCode == HttpStatusCode.OK)
        {
            ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
            clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
            response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
        }
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

        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.KitchenSinkEndpoint}/{storedEntity.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientKitchenSink clientEntity = await response.Content.ReadFromJsonAsync<ClientKitchenSink>(this.serializerOptions);
        clientEntity.Should().NotBeNull().And.HaveEquivalentMetadataTo(storedEntity).And.BeEquivalentTo<IKitchenSink>(storedEntity);
        response.Headers.ETag.Should().BeETag($"\"{clientEntity.Version}\"");
    }

    [Fact]
    public async Task Read_SerializationTests()
    {
        InMemoryKitchenSink entity1 = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UpdatedAt = new DateTimeOffset(2023, 12, 20, 10, 21, 05, 421, TimeSpan.Zero),
            Version = Guid.NewGuid().ToByteArray(),
            Deleted = false,

            BooleanValue = true,
            ByteValue = 42,
            ByteArrayValue = [0x01, 0x02, 0x03, 0x04],
            CharValue = 'a',
            DateOnlyValue = new DateOnly(2023, 12, 15),
            DateTimeValue = new DateTime(2023, 12, 20, 10, 26, 30, 231, DateTimeKind.Utc),
            DateTimeOffsetValue = new DateTimeOffset(2023, 12, 20, 10, 21, 05, 421, TimeSpan.Zero),
            DecimalValue = 42.42m,
            DoubleValue = 42.42,
            EnumValue = KitchenSinkState.Completed,
            FloatValue = 42.42f,
            GuidValue = new Guid("29386779-34b1-464a-bb5b-4053c2ee61b3"),
            IntValue = 42,
            LongValue = 42L,
            NullableDouble = null,
            NullableEnumValue = null,
            PointValue = GeographyPoint.Create(47.609722, -122.333056),
            StringValue = "state=none",
            TimeOnlyValue = new TimeOnly(9, 52, 35, 321)
        };

        InMemoryKitchenSink entity2 = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UpdatedAt = new DateTimeOffset(2023, 12, 20, 10, 21, 05, 421, TimeSpan.Zero),
            Version = Guid.NewGuid().ToByteArray(),
            Deleted = false,

            NullableDouble = 42.42,
            NullableEnumValue = KitchenSinkState.Failed
        };

        this.factory.Store(entity1);
        this.factory.Store(entity2);

        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.KitchenSinkEndpoint}/{entity1.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientObject actual = await response.Content.ReadFromJsonAsync<ClientObject>(this.serializerOptions);

        actual.Data["id"].Should().BeJsonElement(entity1.Id);
        actual.Data["booleanValue"].Should().BeJsonElement(true);
        actual.Data["byteValue"].Should().BeJsonElement(42);
        actual.Data["byteArrayValue"].Should().BeJsonElement("AQIDBA==");
        actual.Data["charValue"].Should().BeJsonElement("a");
        actual.Data["dateOnlyValue"].Should().BeJsonElement("2023-12-15");
        actual.Data["dateTimeValue"].Should().BeJsonElement("2023-12-20T10:26:30.231Z");
        actual.Data["dateTimeOffsetValue"].Should().BeJsonElement("2023-12-20T10:21:05.421Z");
        actual.Data["decimalValue"].Should().BeJsonElement(42.42);
        actual.Data["doubleValue"].Should().BeJsonElement(42.42);
        actual.Data["enumValue"].Should().BeJsonElement("Completed");
        actual.Data["floatValue"].Should().BeJsonElement(42.42);
        actual.Data["guidValue"].Should().BeJsonElement("29386779-34b1-464a-bb5b-4053c2ee61b3");
        actual.Data["intValue"].Should().BeJsonElement(42);
        actual.Data["longValue"].Should().BeJsonElement(42);
        actual.Data["nullableDouble"].Should().BeNullJsonElement();
        actual.Data["nullableEnumValue"].Should().BeNullJsonElement();
        actual.Data["pointValue"].Should().BeJsonObject("{\"type\":\"Point\",\"coordinates\":[-122.333056,47.609722]}");
        actual.Data["stringValue"].Should().BeJsonElement("state=none");
        actual.Data["timeOnlyValue"].Should().BeJsonElement("09:52:35.321");

        HttpResponseMessage response2 = await this.client.GetAsync($"{this.factory.KitchenSinkEndpoint}/{entity2.Id}");
        response2.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientObject actual2 = await response2.Content.ReadFromJsonAsync<ClientObject>();

        actual2.Data["nullableDouble"].Should().BeJsonElement(42.42);
        actual2.Data["nullableEnumValue"].Should().BeJsonElement("Failed");
    }

    [Fact]
    public async Task Read_MissingId_Returns404()
    {
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.MovieEndpoint}/missing");
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Read_SoftDeleted_NotDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_Returns410()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}");
        response.Should().HaveStatusCode(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Read_SoftDeleted_Deleted_WithIncludeDeleted_Returns200()
    {
        InMemoryMovie existingMovie = this.factory.GetRandomMovie();
        this.factory.SoftDelete(existingMovie);
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.SoftDeletedMovieEndpoint}/{existingMovie.Id}?__includedeleted=true");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        ClientMovie clientMovie = await response.Content.ReadFromJsonAsync<ClientMovie>(this.serializerOptions);
        clientMovie.Should().NotBeNull().And.HaveEquivalentMetadataTo(existingMovie).And.BeEquivalentTo<IMovie>(existingMovie);
        response.Headers.ETag.Should().BeETag($"\"{clientMovie.Version}\"");
    }
}
