// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception used by the Datasync client library to report server conflict errors.
/// </summary>
/// <typeparam name="T">The type of entity in conflict</typeparam>
public class ConflictException<T> : DatasyncHttpException where T : notnull
{
    /// <inheritdoc />
    public ConflictException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor")]
    public ConflictException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The parsed server-side entity (or null).
    /// </summary>
    public T? ServerEntity { get; set; } = default;

    /// <summary>
    /// Creates a new <see cref="ConflictException{T}"/> for the response.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> that is creating the exception.</param>
    /// <param name="serializerOptions">The JSON serializer options to use for deserializing content.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns></returns>
    public static async Task<ConflictException<T>> CreateAsync(HttpResponseMessage responseMessage, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default)
    {
        string mediaType = responseMessage.Content.Headers.ContentType?.MediaType ?? string.Empty;
        string content = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new ConflictException<T>(responseMessage.ReasonPhrase)
        {
            StatusCode = responseMessage.StatusCode,
            ContentType = mediaType,
            Payload = content,
            ServerEntity = JsonSerializer.Deserialize<T>(content, serializerOptions)
        };
    }
}
