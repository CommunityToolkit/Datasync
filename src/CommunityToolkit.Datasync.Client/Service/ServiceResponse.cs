// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Service;

/// <summary>
/// A response from a remote datasync service.
/// </summary>
public class ServiceResponse
{
    private readonly Dictionary<string, string> _headers = [];

    /// <summary>
    /// Creates a new <see cref="ServiceResponse"/> based on the provided <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> used for generating this service response.</param>
    internal ServiceResponse(HttpResponseMessage responseMessage)
    {
        ReasonPhrase = responseMessage.ReasonPhrase ?? string.Empty;
        StatusCode = (int)responseMessage.StatusCode;
        CopyHeaders(responseMessage.Headers);
        CopyHeaders(responseMessage.Content.Headers);

        MemoryStream memoryStream = new();
        responseMessage.Content.ReadAsStream().CopyTo(memoryStream);
        memoryStream.Position = 0;
        ContentStream = memoryStream;
        HasContent = memoryStream.Length > 0;
    }

    /// <summary>
    /// The content of the response as a resettable stream.
    /// </summary>
    public Stream ContentStream { get; }

    /// <summary>
    /// If <c>true</c>, then the content stream actually has data in it.
    /// </summary>
    public bool HasContent { get; }

    /// <summary>
    /// If <c>true</c>, the service request was considered completed successfully.
    /// </summary>
    public bool IsSuccessful { get => StatusCode is >= 200 and <= 299; }

    /// <summary>
    /// The reason phrase that was provided in the response.
    /// </summary>
    public string ReasonPhrase { get; }

    /// <summary>
    /// The HTTP status code for this response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Tries to get a header returned by the response; returns true if the header exists and false otherwise.
    /// </summary>
    /// <param name="name">The name of the header to retrieve.</param>
    /// <param name="value">On completion, the value of the header or null if it doesn't exist.</param>
    /// <returns>true if the header exists and false otherwise.</returns>
    public bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
        => this._headers.TryGetValue(name, out value);

    /// <summary>
    /// Copies the provided headers into the combined headers list.
    /// </summary>
    /// <param name="headers">The list of headers to process.</param>
    private void CopyHeaders(HttpHeaders headers)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> item in headers)
        {
            this._headers[item.Key] = item.Value.First();
        }
    }
}

/// <summary>
/// A response from a remote datasync service that potentially includes deserialized content.
/// </summary>
public class ServiceResponse<TEntity>(HttpResponseMessage responseMessage) : ServiceResponse(responseMessage)
{
    /// <summary>
    /// If <c>true</c>, then the service response has value.
    /// </summary>
    public bool HasValue { get => Value != null; }

    /// <summary>
    /// The value of the service response.
    /// </summary>
    public TEntity? Value { get; internal set; }

    /// <summary>
    /// sets the value of this service response based on the content stream.
    /// </summary>
    /// <param name="options">The serializer options to use in deserialization.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An asynchronous task that can be awaited.</returns>
    /// <exception cref="JsonException">Thrown if the content cannot be deserialized into the value.</exception>
    internal async Task SetValueFromContentAsync(JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        Value = await JsonSerializer.DeserializeAsync<TEntity>(ContentStream, options, cancellationToken);
    }
}
