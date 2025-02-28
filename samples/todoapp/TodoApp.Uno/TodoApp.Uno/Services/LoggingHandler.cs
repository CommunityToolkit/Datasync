// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TodoApp.Uno.Services;

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
        if (request.Content is not null)
        {
            await WriteContentAsync(request.Content, cancellationToken);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Debug.WriteLine($"[HTTP] <<< {response.StatusCode} {response.ReasonPhrase}");
        if (response.Content is not null)
        {
            await WriteContentAsync(response.Content, cancellationToken);
        }

        return response;
    }

    private static async Task WriteContentAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        if (content != null)
        {
            Debug.WriteLine($"[HTTP] >>> {await content.ReadAsStringAsync(cancellationToken)}");
        }
    }
}
