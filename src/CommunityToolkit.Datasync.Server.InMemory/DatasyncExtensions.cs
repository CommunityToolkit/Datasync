// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.InMemory;

internal static class DatasyncExtensions
{
    /// <summary>
    /// Creates a deep clone of an entity using JSON serialization.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to clone.</typeparam>
    /// <param name="entity">The entity to clone.</param>
    /// <returns>The cloned entity.</returns>
    public static TEntity Clone<TEntity>(this TEntity entity)
    {
        JsonSerializerOptions options = GetJsonSerializerOptions();
        return JsonSerializer.Deserialize<TEntity>(JsonSerializer.Serialize(entity, options), options)!;
    }

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
