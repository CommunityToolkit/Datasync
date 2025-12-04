// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

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
        ReasonPhrase = responseMessage.ReasonPhrase;
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
    /// Creates a new <see cref="ServiceResponse"/> based on the provided <see cref="ServiceResponse"/>.
    /// </summary>
    /// <param name="response">The <see cref="ServiceResponse"/> used for generating this service response.</param>
    internal ServiceResponse(ServiceResponse response)
    {
        ReasonPhrase = response.ReasonPhrase;
        StatusCode = response.StatusCode;
        ContentStream = response.ContentStream;
        HasContent = response.HasContent;
        this._headers = new Dictionary<string, string>(response.Headers);
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
    /// The collection of HTTP headers sent as part of the response.
    /// </summary>
    public IDictionary<string, string> Headers { get => this._headers; }

    /// <summary>
    /// If <c>true</c>, the service request was rejected because of a conflict.
    /// </summary>
    public bool IsConflictStatusCode { get => StatusCode is 409 or 412; }

    /// <summary>
    /// If <c>true</c>, the service request was considered completed successfully.
    /// </summary>
    /// <remarks>
    /// Datasync service has a constrained view of what is successful that 
    /// deliberately does not match HTTP semantics.
    /// </remarks>
    public bool IsSuccessful { get => StatusCode is 200 or 201 or 204; }

    /// <summary>
    /// The reason phrase that was provided in the response.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// The HTTP status code for this response.
    /// </summary>
    public int StatusCode { get; internal set; }

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

    /// <summary>
    /// Helper method to throw the right exception if an error is returned by the service.
    /// </summary>
    /// <exception cref="DatasyncHttpException">Thrown if an error is returned by the service.</exception>
    public void ThrowIfNotSuccessful(bool throwOnNotFound = true, bool requireContent = false)
    {
        if (!IsSuccessful && (StatusCode != 404 || throwOnNotFound))
        {
            throw new DatasyncHttpException(this);
        }

        if (requireContent && !HasContent)
        {
            throw new DatasyncException(ServiceErrorMessages.NoContent);
        }
    }
}

/// <summary>
/// A response from a remote datasync service that potentially includes deserialized content.
/// </summary>
public class ServiceResponse<TEntity> : ServiceResponse
{
    /// <summary>
    /// Creates a new <see cref="ServiceResponse{TEntity}"/> based on a <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> to use in creating the <see cref="ServiceResponse{TEntity}"/>.</param>
    internal ServiceResponse(HttpResponseMessage responseMessage) : base(responseMessage)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ServiceResponse{TEntity}"/> based on a <see cref="ServiceResponse"/>.
    /// </summary>
    /// <param name="response">The source <see cref="ServiceResponse"/> object.</param>
    internal ServiceResponse(ServiceResponse response) : base(response)
    {
    }

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

    /// <summary>
    /// Creates a <see cref="ServiceResponse{TEntity}"/> with a deserialized value.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for this response.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The service response.</returns>
    /// <exception cref="JsonException">Thrown if the content cannot be deserialized into the value.</exception>
    public static async Task<ServiceResponse<TEntity>> CreateAsync(HttpResponseMessage responseMessage, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default)
    {
        ServiceResponse<TEntity> result = new(responseMessage);
        if (result.HasContent)
        {
            await result.SetValueFromContentAsync(serializerOptions, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Helper method to throw the right exception if the service indicates that a conflict has occurred.
    /// </summary>
    /// <exception cref="ConflictException{TEntity}">Thrown if the service indicates that a conflict has occurred.</exception>
    public void ThrowIfConflict(TEntity entity)
    {
        if (IsConflictStatusCode)
        {
            throw new ConflictException<TEntity>(entity, this);
        }
    }
}
