// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Spatial;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.Abstractions.Json;

/// <summary>
/// Converters between Microsoft.Spatial types and GeoJSON
/// </summary>
/// <remarks>
/// Only handles GeographyPoint at this time.
/// </remarks>
/// <see href="https://github.com/Azure/azure-sdk-for-net/tree/Microsoft.Azure.Core.Spatial_1.1.0/sdk/core/Microsoft.Azure.Core.Spatial/src/Serialization" />
public class SpatialGeoJsonConverter : JsonConverter<object>
{
    private const string CoordinatesPropertyName = "coordinates";
    private const string PointTypeName = "Point";
    private const string TypePropertyName = "type";

    private static readonly JsonEncodedText s_CoordinatesPropertyNameBytes = JsonEncodedText.Encode(CoordinatesPropertyName);
    private static readonly JsonEncodedText s_TypePropertyNameBytes = JsonEncodedText.Encode(TypePropertyName);

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeof(GeographyPoint).IsAssignableFrom(typeToConvert);

    /// <inheritdoc/>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        string? type = default;
        double? longitude = default;
        double? latitude = default;

        reader.Expect(JsonTokenType.StartObject);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            reader.Expect(JsonTokenType.PropertyName);
            string? propertyName = reader.GetString();

            _ = reader.Read();
            if (string.Equals(TypePropertyName, propertyName, StringComparison.Ordinal))
            {
                reader.Expect(JsonTokenType.String);
                type = reader.GetString();
            }
            else if (string.Equals(CoordinatesPropertyName, propertyName, StringComparison.Ordinal))
            {
                reader.Expect(JsonTokenType.StartArray);

                // Longitude
                _ = reader.Read();
                reader.Expect(JsonTokenType.Number);
                longitude = reader.GetDouble();

                // Latitude
                _ = reader.Read();
                reader.Expect(JsonTokenType.Number);
                latitude = reader.GetDouble();

                // Skip the rest.
                do
                {
                    _ = reader.Read();
                } while (reader.TokenType != JsonTokenType.EndArray);
            }
            else
            {
                reader.Skip();
            }
        }

        if (!string.Equals(PointTypeName, type, StringComparison.Ordinal))
        {
            throw new JsonException($"Deserialization of {nameof(GeographyPoint)} failed. Expected geographic type: '{PointTypeName}'.");
        }

        if (!longitude.HasValue || !latitude.HasValue)
        {
            throw new JsonException($"Deserialization of {nameof(GeographyPoint)} failed. Expected both longitude and latitude.");
        }

        return GeographyPoint.Create(latitude.Value, longitude.Value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is GeographyPoint point)
        {
            writer.WriteStartObject();
            writer.WriteString(s_TypePropertyNameBytes, PointTypeName);
            writer.WriteStartArray(s_CoordinatesPropertyNameBytes);
            writer.WriteNumberValue(point.Longitude);
            writer.WriteNumberValue(point.Latitude);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
