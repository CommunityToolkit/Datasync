// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class DateTimeConverter_Tests
{
    private readonly JsonSerializerOptions serializerOptions;

    public DateTimeConverter_Tests()
    {
        this.serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        this.serializerOptions.Converters.Add(new DateTimeConverter());
    }

    [Fact]
    public void Read_Null_Works()
    {
        string json = """{"dt":null}""";
        SUT actual = JsonSerializer.Deserialize<SUT>(json, this.serializerOptions);
        actual.dt.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void Read_Int_Throws()
    {
        string json = """{"dt":42}""";
        Action act = () => _ = JsonSerializer.Deserialize<SUT>(json, this.serializerOptions);
        act.Should().Throw<JsonException>();
    }

    class SUT
    {
        public DateTime dt { get; set; }
    }
}
