// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// The serialization settings for Datasync settings.
/// </summary>
public static class DatasyncSerializer
{
    private readonly static Lazy<JsonSerializerOptions> _initializer = new(GetJsonSerializerOptions);
    private static JsonSerializerOptions? _userSuppliedOptions;

    /// <summary>
    /// Accessor for the common <see cref="JsonSerializerOptions"/> to use for serializing and deserializing 
    /// content in the service.
    /// </summary>
    public static JsonSerializerOptions JsonSerializerOptions
    { 
        get => _userSuppliedOptions ?? _initializer.Value;
        set => _userSuppliedOptions = value;
    }

    /// <summary>
    /// Serializes an object using the serializer options.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object.</param>
    /// <returns>The serialized version of the object.</returns>
    public static string Serialize<T>(T obj)
        => JsonSerializer.Serialize(obj, JsonSerializerOptions);

    /// <summary>
    /// Serializes an object using the serializer options.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <param name="objType">The type of the object.</param>
    /// <returns>The serialized version of the object.</returns>
    public static string Serialize(object obj, Type objType)
        => JsonSerializer.Serialize(obj, objType, JsonSerializerOptions);

    /// <summary>
    /// Internal method to create a new <see cref="JsonSerializerOptions"/> object for serializing and deserializing
    /// content in the service.  You should never have to call this.
    /// </summary>
    /// <returns>A configured <see cref="JsonSerializerOptions"/> object.</returns>
    public static JsonSerializerOptions GetJsonSerializerOptions() => new(JsonSerializerDefaults.Web)
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
