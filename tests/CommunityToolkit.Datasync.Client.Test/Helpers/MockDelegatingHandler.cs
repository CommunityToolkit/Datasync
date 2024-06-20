// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

/// <summary>
/// A delegating handler for mocking responses.
/// </summary>
[ExcludeFromCodeCoverage]
public class MockDelegatingHandler : DelegatingHandler
{
    // For manipulating the request/response link - we need to surround it with a lock
    private readonly SemaphoreSlim requestLock = new(1, 1);

    /// <summary>
    /// Used for serializing objects to be returned as responses.
    /// </summary>
    private static readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// List of requests that have been received.
    /// </summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>
    /// List of responses that will be sent.
    /// </summary>
    public List<HttpResponseMessage> Responses { get; } = [];

    /// <summary>
    /// Handler for the request/response
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token = default)
    {
        await this.requestLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Requests.Add(await CloneRequest(request).ConfigureAwait(false));
            return Responses[Requests.Count - 1];
        }
        finally
        {
            this.requestLock.Release();
        }
    }

    /// <summary>
    /// Clone the <see cref="HttpRequestMessage"/>.
    /// </summary>
    public static async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage request)
    {
        HttpRequestMessage clone = new(request.Method, request.RequestUri) 
        { 
            Version = request.Version 
        };
        request.Headers.ToList().ForEach(header => clone.Headers.TryAddWithoutValidation(header.Key, header.Value));

        if (request.Content != null)
        {
            MemoryStream ms = new();
            await request.Content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            request.Content.Headers?.ToList().ForEach(header => clone.Content.Headers.Add(header.Key, header.Value));
        }

        return clone;
    }

    /// <summary>
    /// Adds a response with no payload to the list of responses.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="headers"></param>
    public void AddResponse(HttpStatusCode statusCode, IDictionary<string, string> headers = null)
        => Responses.Add(CreateResponse(statusCode, headers));

    /// <summary>
    /// Adds a response with a string payload.
    /// </summary>
    /// <param name="content">The JSON content</param>
    public void AddResponseContent(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        HttpResponseMessage response = CreateResponse(statusCode);
        response.Content = new StringContent(content, Encoding.UTF8, "application/json");
        Responses.Add(response);
    }

    /// <summary>
    /// Adds a response with a payload to the list of responses.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="statusCode"></param>
    /// <param name="payload"></param>
    /// <param name="headers"></param>
    public void AddResponse<T>(HttpStatusCode statusCode, T payload, IDictionary<string, string> headers = null)
    {
        HttpResponseMessage response = CreateResponse(statusCode, headers);
        response.Content = new StringContent(JsonSerializer.Serialize(payload, serializerOptions), Encoding.UTF8, "application/json");
        Responses.Add(response);
    }

    /// <summary>
    /// Creates a <see cref="HttpResponseMessage"/> with no payload
    /// </summary>
    /// <param name="statusCode">The status code</param>
    /// <param name="headers">The headers (if any) to add</param>
    /// <returns>The <see cref="HttpResponseMessage"/></returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, IDictionary<string, string> headers = null)
    {
        HttpResponseMessage response = new(statusCode);
        if (headers != null)
        {
            foreach (KeyValuePair<string, string> kv in headers)
            {
                if (!response.Content.Headers.TryAddWithoutValidation(kv.Key, kv.Value))
                {
                    response.Headers.Add(kv.Key, kv.Value);
                }
            }
        }

        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.requestLock.Dispose();
        }

        base.Dispose(disposing);
    }
}
