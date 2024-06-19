// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0058 // Expression value is never used

using CommunityToolkit.Datasync.Common;
using Defaults = CommunityToolkit.Datasync.Client.DatasyncClientDefaults;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Options to use for configuring the <see cref="DatasyncClient"/> object.
/// </summary>
public class DatasyncClientOptions
{
    private int _parallelOperations = Defaults.ParallelOperations;
    private string _clientName = string.Empty;

    /// <summary>
    /// The <see cref="IDatasyncServiceOptions"/> to use when communicating with the
    /// datasync service.
    /// </summary>
    public IDatasyncServiceOptions DatasyncServiceOptions { get; set; } = new DatasyncServiceOptions();

    /// <summary>
    /// If set, the <see cref="IHttpClientFactory"/> to use for generating
    /// <see cref="HttpClient"/> objects for communicating with the datasync
    /// server.
    /// </summary>
    public IHttpClientFactory? HttpClientFactory { get; set; }

    /// <summary>
    /// The name of the client to use when creating a new <see cref="HttpClient"/>
    /// via the <see cref="HttpClientFactory"/>.
    /// </summary>
    public string HttpClientName
    {
        get => this._clientName;
        set
        {
            Ensure.That(value, nameof(HttpClientName)).IsNotNullOrWhiteSpace();
            this._clientName = value;
        }
    }

    /// <summary>
    /// The number of parallel operations that can be executed within the scope of 
    /// a push or pull operation.
    /// </summary>
    public int ParallelOperations
    {
        get => this._parallelOperations;
        set
        {
            Ensure.That(value, nameof(ParallelOperations)).IsInRange(1, Defaults.MaxParallelOperations);
            this._parallelOperations = value;
        }
    }

    /// <summary>
    /// The function to use for generating a globally unique ID for an entity being
    /// synchronized to the datasync service.
    /// </summary>
    public Func<string, string> EntityIdGenerator { get; set; } = Defaults.EntityIdGenerator;

    /// <summary>
    /// The function to use for turning a table name into a relative path for accessing 
    /// the table endpoint on the remote service.
    /// </summary>
    public Func<string, string> TableEndpointResolver { get; set; } = Defaults.TableEndpointResolver;
}
