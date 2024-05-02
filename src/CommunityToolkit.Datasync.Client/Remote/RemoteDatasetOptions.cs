// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core.Serialization;
using CommunityToolkit.Datasync.Common;
using CommunityToolkit.Datasync.Server.Converters;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The options that can be used for setting up a remote dataset.
/// </summary>
public class RemoteDatasetOptions : IRemoteDatasetOptions
{
    private JsonSerializerOptions _jsonSerializerOptions;
    private TimeSpan _httpTimeout = TimeSpan.FromSeconds(100);

    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication.  If not set, a new <see cref="HttpClient"/> will be created
    /// based on other options.
    /// </summary>
    public HttpClient HttpClient { get; set; }

    /// <summary>
    /// The <see cref="IHttpClientFactory"/> that will be used to create a <see cref="HttpClient"/>.  If not set, a new
    /// <see cref="HttpClient"/> will be created based on other options.
    /// </summary>
    public IHttpClientFactory HttpClientFactory { get; set; }

    /// <summary>
    /// The name of the <see cref="HttpClient"/> that will be created.  This is used to request the right <see cref="HttpClient"/>
    /// from the <see cref="IHttpClientFactory"/>.  If null or empty, the name of the table will be used as the name of the client.
    /// </summary>
    public string HttpClientName { get; set; } = "";

    /// <summary>
    /// An ordered set of <see cref="HttpMessageHandler"/> instances that will be used to set up a pipeline for modifying the
    /// HTTP request and response.  This is used to set up the default <see cref="HttpClient"/> or <see cref="IHttpClientFactory"/>.
    /// </summary>
    public IEnumerable<HttpMessageHandler> HttpPipeline { get; set; } = [];

    /// <summary>
    /// A set of headers that will be added to the HTTP request.  This is used to set up the default <see cref="HttpClient"/>
    /// </summary>
    public IDictionary<string, string> HttpRequestHeaders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The default timeout to use for HTTP requests.  If not set, the default timeout is 100 seconds.
    /// </summary>
    public TimeSpan HttpTimeout
    {
        get => this._httpTimeout;
        set { Ensure.That(value, nameof(HttpTimeout)).IsGte(TimeSpan.FromSeconds(5)); this._httpTimeout = value; }
    }

    /// <summary>
    /// The serializer options to use for serializing and deserializing data.  If not set, the same defaults as the service are used.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => this._jsonSerializerOptions ?? GetDefaultJsonSerializerOptions();
        set { Ensure.That(value, nameof(JsonSerializerOptions)).IsNotNull(); this._jsonSerializerOptions = value; }
    }

    /// <summary>
    /// Creates the default <see cref="JsonSerializerOptions"/> for communicating with the datasync service.
    /// </summary>
    /// <returns>A set of <see cref="JsonSerializerOptions"/>.</returns>
    private JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        this._jsonSerializerOptions = new(JsonSerializerDefaults.Web)
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
        return this._jsonSerializerOptions;
    }
}
