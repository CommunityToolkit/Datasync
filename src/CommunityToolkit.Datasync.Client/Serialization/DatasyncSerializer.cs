// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Serialization;

internal static class DatasyncSerializer
{
    private readonly static Lazy<JsonSerializerOptions> _initializer = new(GetJsonSerializerOptions);

    /// <summary>
    /// Accessor for the common <see cref="JsonSerializerOptions"/> to use for serializing and deserializing 
    /// content in the service.
    /// </summary>
    internal static JsonSerializerOptions JsonSerializerOptions { get => _initializer.Value; }

    /// <summary>
    /// Internal method to create a new <see cref="JsonSerializerOptions"/> object for serializing and deserializing
    /// content in the service.  You should never have to call this.
    /// </summary>
    /// <returns>A configured <see cref="JsonSerializerOptions"/> object.</returns>
    internal static JsonSerializerOptions GetJsonSerializerOptions() => new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new DateTimeOffsetConverter(),
            new DateTimeConverter(),
            new TimeOnlyConverter(),
            new MicrosoftSpatialGeoJsonConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
}
