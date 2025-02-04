// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.Abstractions.Json;

/// <summary>
/// A specialized converter for the <see cref="TimeOnly"/> type that
/// handles the specific requirements of the Datasync Toolkit.
/// </summary>
public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string format = "HH:mm:ss.fff";

    /// <inheritdoc />
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeOnly.Parse(reader.GetString() ?? string.Empty);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(format, CultureInfo.InvariantCulture));
}
