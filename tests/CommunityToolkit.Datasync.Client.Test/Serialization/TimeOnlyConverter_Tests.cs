// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class TimeOnlyConverter_Tests : SerializerTests
{
    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_ReadsJson(string culture)
    {
        const string json = """{"updatedAt":"12:30:15.123"}""";
        TimeOnly value = new(12, 30, 15, 123);

        TestWithCulture(culture, () =>
        {
            Entity entity = JsonSerializer.Deserialize<Entity>(json, SerializerOptions);
            entity.UpdatedAt.Ticks.Should().Be(value.Ticks);
        });
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_WritesJson(string culture)
    {
        const string json = """{"updatedAt":"12:30:15.123"}""";
        TimeOnly value = new(12, 30, 15, 123, 456);

        TestWithCulture(culture, () =>
        {
            Entity entity = new() { UpdatedAt = value };
            string actual = JsonSerializer.Serialize(entity, SerializerOptions);
            Assert.Equal(json, actual);
        });
    }

    [Fact]
    public void Converter_ThrowsOnBadDateInInput()
    {
        const string json = """{"updatedAt":"foo"}""";
        Action act = () => _ = JsonSerializer.Deserialize<Entity>(json, SerializerOptions);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_ReturnsMinValueOnNullDateInInput(string culture)
    {
        const string json = """{"updatedAt":null}""";
        TimeOnly value = TimeOnly.MinValue;

        TestWithCulture(culture, () =>
        {
            Entity entity = JsonSerializer.Deserialize<Entity>(json, SerializerOptions);
            entity.UpdatedAt.Should().Be(value);
        });
    }

    #region Models
    public class Entity
    {
        public TimeOnly UpdatedAt { get; set; }
    }
    #endregion
}
