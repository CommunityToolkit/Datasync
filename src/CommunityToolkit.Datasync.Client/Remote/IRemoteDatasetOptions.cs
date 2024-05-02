// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The options that can be used for setting up a remote dataset.
/// </summary>
public interface IRemoteDatasetOptions : IDatasyncHttpOptions
{
    /// <summary>
    /// The <see cref="HttpClient"/> to use for communication.  If not set, a new <see cref="HttpClient"/> will be created
    /// based on other options.
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    /// The <see cref="IHttpClientFactory"/> that will be used to create a <see cref="HttpClient"/>.  If not set, a new
    /// <see cref="HttpClient"/> will be created based on other options.
    /// </summary>
    IHttpClientFactory HttpClientFactory { get; }

    /// <summary>
    /// The name of the <see cref="HttpClient"/> that will be created.  This is used to request the right <see cref="HttpClient"/>
    /// from the <see cref="IHttpClientFactory"/>.  If null or empty, the name of the table will be used as the name of the client.
    /// </summary>
    string HttpClientName { get; }

    /// <summary>
    /// The serializer options to use for serializing and deserializing data.  If not set, the same defaults as the service are used.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }
}
