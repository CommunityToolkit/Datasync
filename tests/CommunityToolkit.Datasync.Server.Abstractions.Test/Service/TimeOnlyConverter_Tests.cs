// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.Abstractions.Test.Service;

[ExcludeFromCodeCoverage]
public class TimeOnlyConverter_Tests
{
    private readonly JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;

    [Fact]
    public void Read_ValidString_ReturnsValidResult()
    {
        TimeOnly expected = TimeOnly.Parse("12:23:20.010");
        const string json = "{\"dt\":\"12:23:20.010\"}";
        Dictionary<string, TimeOnly> actual = JsonSerializer.Deserialize<Dictionary<string, TimeOnly>>(json, this.options);
        actual["dt"].Should().Be(expected);
    }

    [Fact]
    public void Read_NonString_ReturnsDefault()
    {
        const string json = "{\"dt\":null}";
        Action act = () => _ = JsonSerializer.Deserialize<Dictionary<string, TimeOnly>>(json, this.options);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_BadString_Throws()
    {
        const string json = "{\"dt\":\"not-time\"}";
        Action act = () => _ = JsonSerializer.Deserialize<Dictionary<string, TimeOnly>>(json, this.options);
        act.Should().Throw<FormatException>();
    }
}
