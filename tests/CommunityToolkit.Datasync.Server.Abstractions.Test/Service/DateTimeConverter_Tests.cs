// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.Abstractions.Test.Service;

[ExcludeFromCodeCoverage]
public class DateTimeConverter_Tests
{
    private readonly JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;

    [Fact]
    public void Read_ValidString_ReturnsValidResult()
    {
        DateTime expected = DateTime.Parse("2023-12-23T12:23:20.010Z");
        const string json = "{\"dt\":\"2023-12-23T12:23:20.010Z\"}";
        Dictionary<string, DateTime> actual = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json, this.options);
        actual["dt"].Should().Be(expected);
    }

    [Fact]
    public void Read_NonString_ReturnsDefault()
    {
        const string json = "{\"dt\":null}";
        Action act = () => _ = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json, this.options);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Read_BadString_Throws()
    {
        const string json = "{\"dt\":\"not-date\"}";
        Action act = () => _ = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json, this.options);
        act.Should().Throw<FormatException>();
    }
}
