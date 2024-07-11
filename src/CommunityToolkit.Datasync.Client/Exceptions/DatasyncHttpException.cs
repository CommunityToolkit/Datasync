// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An exception used by the Datasync client library to report HTTP errors.
/// </summary>
public class DatasyncHttpException : DatasyncException
{
    /// <inheritdoc />
    public DatasyncHttpException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor")]
    public DatasyncHttpException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The HTTP Status Code that was returned.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Ambiguous;

    /// <summary>
    /// The MIME content type for the payload, or the empty string if none was found.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The payload within the response, or the empty string if none was found.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="DatasyncHttpException"/> for the response.
    /// </summary>
    /// <param name="responseMessage">The <see cref="HttpResponseMessage"/> that is creating the exception.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns></returns>
    public static async Task<DatasyncHttpException> CreateAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken = default)
    {
        if (responseMessage.StatusCode == HttpStatusCode.NotFound)
        {
            throw new EntityNotFoundException();
        }

        string mediaType = responseMessage.Content.Headers.ContentType?.MediaType ?? string.Empty;
        string content = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new DatasyncHttpException(responseMessage.ReasonPhrase)
        {
            StatusCode = responseMessage.StatusCode,
            ContentType = mediaType,
            Payload = content
        };
    }
}
