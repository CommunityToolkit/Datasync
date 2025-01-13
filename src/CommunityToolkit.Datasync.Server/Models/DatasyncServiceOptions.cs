// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Json;
using CommunityToolkit.Datasync.Server.OData;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A standard implementation of the <see cref="IDatasyncServiceOptions"/> interface that
/// is configured to serialize and deserialize JSON using the default options required for
/// the Datasync Toolkit.
/// </summary>
public class DatasyncServiceOptions : IDatasyncServiceOptions
{
    private readonly Lazy<JsonSerializerOptions> _options;

    /// <summary>
    /// Creates a new set of service options for the Datasync Toolkit.
    /// </summary>
    public DatasyncServiceOptions()
    {
        this._options = new Lazy<JsonSerializerOptions>(() => GetJsonSerializerOptions());
    }

    /// <inheritdoc />
    public JsonSerializerOptions JsonSerializerOptions => this._options.Value;

    private static JsonSerializerOptions GetJsonSerializerOptions() => new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new DateTimeOffsetConverter(),
            new DateTimeConverter(),
            new TimeOnlyConverter(),
            new SpatialGeoJsonConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}
