// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.Abstractions.Test.Json;

[ExcludeFromCodeCoverage]
public class DateTimeConverter_Tests : SerializerTests
{
    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_ReadsJson(string culture)
    {
        const string json = """{"updatedAt":"2021-08-21T12:30:15.123+00:00"}""";
        DateTime value = new(2021, 8, 21, 12, 30, 15, 123, DateTimeKind.Utc);

        TestWithCulture(culture, () =>
        {
            Entity entity = JsonSerializer.Deserialize<Entity>(json, SerializerOptions);
            entity.UpdatedAt.ToFileTime().Should().Be(value.ToFileTime());
        });
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_WritesJson(string culture)
    {
        const string json = """{"updatedAt":"2021-08-21T12:30:15.123Z"}""";
        DateTime value = new(2021, 8, 21, 12, 30, 15, 123, 456, DateTimeKind.Utc);

        TestWithCulture(culture, () =>
        {
            Entity entity = new() { UpdatedAt = value };
            string actual = JsonSerializer.Serialize(entity, SerializerOptions);
            Assert.Equal(json, actual);
        });
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_WritesJson_WithTimeZone(string culture)
    {
        const string json = """{"updatedAt":"2021-08-21T12:30:15.123Z"}""";
        DateTime value = DateTime.Parse("2021-08-21T20:30:15.1234567+08:00");

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

    [Fact]
    public void Converter_ThrowsOnNullDateInInput()
    {
        const string json = """{"updatedAt":null}""";
        Action act = () => _ = JsonSerializer.Deserialize<Entity>(json, SerializerOptions);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_Roundtrip_Consistent_Default(string culture)
    {
        DateTime value = default;

        TestWithCulture(culture, () =>
        {
            Entity entity = new() { UpdatedAt = value };
            string serialized = JsonSerializer.Serialize(entity, SerializerOptions);
            Entity deserialized = JsonSerializer.Deserialize<Entity>(serialized, SerializerOptions);
            Assert.Equal(deserialized.UpdatedAt, value);
        });
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_Roundtrip_Consistent_Local(string culture)
    {
        DateTime value = new(2021, 8, 21, 12, 30, 15, 123, DateTimeKind.Local);

        TestWithCulture(culture, () =>
        {
            Entity entity = new() { UpdatedAt = value };
            string serialized = JsonSerializer.Serialize(entity, SerializerOptions);
            Entity deserialized = JsonSerializer.Deserialize<Entity>(serialized, SerializerOptions);
            Assert.Equal(deserialized.UpdatedAt, value);
        });
    }

    [Theory]
    [MemberData(nameof(Locales))]
    public void Converter_Roundtrip_Consistent_Utc(string culture)
    {
        DateTime value = new(2021, 8, 21, 14, 35, 20, 12, DateTimeKind.Utc);

        TestWithCulture(culture, () =>
        {
            Entity entity = new() { UpdatedAt = value };
            string serialized = JsonSerializer.Serialize(entity, SerializerOptions);
            Entity deserialized = JsonSerializer.Deserialize<Entity>(serialized, SerializerOptions);
            // Roundtrip will convert to local time, DateTimeKind is not preserved.
            Assert.Equal(deserialized.UpdatedAt, value.ToLocalTime());
        });
    }

    #region Models
    public class Entity
    {
        public DateTime UpdatedAt { get; set; }
    }
    #endregion
}
