// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Common;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

/// <summary>
/// A base class for test classes that use operational tests
/// against mock services.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseOperationTest
{
    private readonly Lazy<MockDelegatingHandler> _mockHandler = new(() => new MockDelegatingHandler());
    private readonly JsonSerializerOptions _serializerOptions = new DatasyncServiceOptions().JsonSerializerOptions;
    private readonly RemoteDataset<ClientMovie> _dataset;

    protected BaseOperationTest()
    {
        HttpClient mockClient = GetMockClient();
        this._serializerOptions = new DatasyncServiceOptions().JsonSerializerOptions;
        this._dataset = new RemoteDataset<ClientMovie>(mockClient, this._serializerOptions, Path);
    }

    /// <summary>
    /// The base address of the service.
    /// </summary>
    protected const string BaseAddress = "http://localhost:8000";

    /// <summary>
    /// The mocked dataset.
    /// </summary>
    protected RemoteDataset<ClientMovie> Dataset { get => this._dataset; }

    /// <summary>
    /// The mock handler - holder of requests and responses
    /// </summary>
    protected MockDelegatingHandler MockHandler { get => this._mockHandler.Value; }

    /// <summary>
    /// The path to the table controller.
    /// </summary>
    protected const string Path = "/tables/movies";

    /// <summary>
    /// Retrieves a new HttpClient to use for mocked HTTP calls.
    /// </summary>
    protected HttpClient GetMockClient()
        => new(MockHandler) { BaseAddress = new Uri(BaseAddress) };
}