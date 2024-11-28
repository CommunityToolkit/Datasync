// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Text.Json;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class DatasyncSerializer_Tests
{
    [Fact]
    public void Serializer_Tests()
    {
        ClientMovie movie = new()
        {
            Id = "54E200F5-A0C9-4C77-86F8-1A73168D1A6F",
            UpdatedAt = DateTimeOffset.Parse("2024-08-16T13:53:20.123Z"),
            Version = "1234"
        };
        TestData.Movies.BlackPanther.CopyTo(movie);

        string expected = """{"bestPictureWinner":true,"duration":134,"rating":"PG13","releaseDate":"2018-02-16","title":"Black Panther","year":2018,"id":"54E200F5-A0C9-4C77-86F8-1A73168D1A6F","updatedAt":"2024-08-16T13:53:20.123Z","version":"1234"}""";

        string actual = DatasyncSerializer.Serialize(movie);
        actual.Should().Be(expected);

        actual = DatasyncSerializer.Serialize(movie, typeof(ClientMovie));
        actual.Should().Be(expected);
    }

    [Fact]
    public void CanSetSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        JsonSerializerOptions sut = DatasyncSerializer.JsonSerializerOptions;
        sut.Should().NotBeNull().And.NotBe(options);

        DatasyncSerializer.JsonSerializerOptions = options;
        DatasyncSerializer.JsonSerializerOptions.Should().Be(options);

        DatasyncSerializer.JsonSerializerOptions = null;
        DatasyncSerializer.JsonSerializerOptions.Should().Be(sut);
    }
}
