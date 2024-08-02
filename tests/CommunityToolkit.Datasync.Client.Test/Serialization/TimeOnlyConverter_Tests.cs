// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class TimeOnlyConverter_Tests
{
    private readonly JsonSerializerOptions serializerOptions;

    public TimeOnlyConverter_Tests()
    {
        this.serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        this.serializerOptions.Converters.Add(new TimeOnlyConverter());
    }

    [Fact]
    public void Read_Null_Works()
    {
        string json = """{"dt":null}""";
        SUT actual = JsonSerializer.Deserialize<SUT>(json, this.serializerOptions);
        actual.dt.Should().Be(TimeOnly.MinValue);
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
        public TimeOnly dt { get; set; }
    }
}
