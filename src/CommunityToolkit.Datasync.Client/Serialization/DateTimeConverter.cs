// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// A specialized converter for the <see cref="DateTime"/> type that
/// handles the specific requirements of the Datasync Toolkit.
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    private const string format = "yyyy-MM-dd'T'HH:mm:ss.fffK";

    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();
        if (string.IsNullOrEmpty(token))
        {
            return DateTime.MinValue;
        }
        else
        {
            // Check if datetime was 'default'. If so do not adjust to local time.
            DateTime utc = DateTime.Parse(token, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            if (utc == default)
            {
                return utc;
            }

            return DateTime.Parse(token);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture));
}
