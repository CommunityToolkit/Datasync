// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core.Serialization;
using CommunityToolkit.Datasync.Server.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A class for defining the options for the datasync client.
/// </summary>
public class DatasyncClientOptions : IDatasyncClientOptions
{
    private readonly Lazy<JsonSerializerOptions> _defaultSerializerOptions = new(() => GetDefaultJsonSerializerOptions());
    private JsonSerializerOptions? _serializerOptions;

    /// <summary>
    /// The HTTP Pipeline to use.  This can be null.  If set, it must
    /// be an ordered set of <see cref="DelegatingHandler"/> objects,
    /// potentially followed by a <see cref="HttpClientHandler"/> for
    /// a transport.
    /// </summary>
    /// <remarks>
    /// By default, no additional handlers are added to the pipeline.
    /// </remarks>
    public IEnumerable<HttpMessageHandler> HttpPipeline { get; set; } = [];

    /// <summary>
    /// The timeout to use with <see cref="HttpClient"/> connections.
    /// </summary>
    /// <remarks>
    /// By default, the timeout is set to 100 seconds.
    /// </remarks>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// The function that generates a globall unique ID for the item when required.
    /// </summary>
    /// <remarks>
    /// By default, a globally unique ID is generated using <c>Guid.NewGuid()</c>.
    /// </remarks>
    public Func<string, string> IdGenerator { get; set; } = (string _) => Guid.NewGuid().ToString("N");

    /// <summary>
    /// The serializer options to use when serializing and deserializing data.
    /// </summary>
    /// <remarks>
    /// By default, the JSON serializer options match the default serializer options used by the server.
    /// </remarks>
    public JsonSerializerOptions SerializerOptions
    {
        get => this._serializerOptions ?? this._defaultSerializerOptions.Value;
        set => this._serializerOptions = value;
    }

    /// <summary>
    /// The function that creates the path to the table endpoint based on the table name.
    /// </summary>
    public Func<string, string> TableEndpointResolver { get; set; } = (string tableName) => $"/tables/{tableName}";

    private static JsonSerializerOptions GetDefaultJsonSerializerOptions() => new(JsonSerializerDefaults.Web)
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
