// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http;

namespace TodoApp.WPF.Services;

/// <summary>
/// A delegating handler that logs the request/response to stdout.
/// </summary>
public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler() : base()
    {
    }

    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"[HTTP] >>> {request.Method} {request.RequestUri}");
        await WriteContentAsync(request.Content, cancellationToken);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Debug.WriteLine($"[HTTP] <<< {response.StatusCode} {response.ReasonPhrase}");
        await WriteContentAsync(response.Content, cancellationToken);

        return response;
    }

    private static async Task WriteContentAsync(HttpContent? content, CancellationToken cancellationToken = default)
    {
        if (content is not null)
        {
            Debug.WriteLine($"[HTTP] >>> {await content.ReadAsStringAsync(cancellationToken)}");
        }
    }
}
