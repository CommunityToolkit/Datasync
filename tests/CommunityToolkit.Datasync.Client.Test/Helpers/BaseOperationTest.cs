// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

/// <summary>
/// A base class for test classes that use operational tests
/// against mock services.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseOperationTest
{
    private readonly Lazy<MockDelegatingHandler> _mockHandler = new(() => new MockDelegatingHandler());

    /// <summary>
    /// The base address of the service.
    /// </summary>
    protected string BaseAddress => "http://localhost:8000";

    /// <summary>
    /// The mock handler - holder of requests and responses
    /// </summary>
    protected MockDelegatingHandler MockHandler { get => this._mockHandler.Value; }

    /// <summary>
    /// Retrieves a new HttpClient to use for mocked HTTP calls.
    /// </summary>
    protected HttpClient GetMockClient()
        => new HttpClient(MockHandler) { BaseAddress = new Uri(BaseAddress) };
}