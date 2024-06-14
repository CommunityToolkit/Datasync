// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Common.Json;

/// <summary>
/// A specialized converter for the <see cref="DateTimeOffset"/> type that
/// handles the specific requirements of the Datasync Toolkit.
/// </summary>
public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string format = "yyyy-MM-dd'T'HH:mm:ss.fffK";

    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString() ?? string.Empty);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime().UtcDateTime.ToString(format));
}
