// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core.Serialization;
using CommunityToolkit.Datasync.Server.Abstractions.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server.Abstractions.Test.Json;

[ExcludeFromCodeCoverage]
public abstract class SerializerTests
{
    private readonly Lazy<JsonSerializerOptions> _defaultSerializerOptions = new(() => GetJsonSerializerOptions());

    protected JsonSerializerOptions SerializerOptions { get => this._defaultSerializerOptions.Value; }

    private static JsonSerializerOptions GetJsonSerializerOptions() => new(JsonSerializerDefaults.Web)
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
