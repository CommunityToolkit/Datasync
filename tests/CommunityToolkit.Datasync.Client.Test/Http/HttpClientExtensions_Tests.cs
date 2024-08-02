// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class HttpClientExtensions_Tests : Disposable
{
    private readonly HttpClient client = new();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.client.Dispose();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddHeaderIfNotNullOrWhiteSpace_HeaderName_Returns(string headerName)
    {
        int nHeaders = this.client.DefaultRequestHeaders.Count();
        this.client.AddHeaderIfNotNullOrWhiteSpace(headerName, "foo");
        this.client.DefaultRequestHeaders.Should().HaveCount(nHeaders);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddHeaderIfNotNullOrWhiteSpace_HeaderValue_Returns(string headerValue)
    {
        int nHeaders = this.client.DefaultRequestHeaders.Count();
        this.client.AddHeaderIfNotNullOrWhiteSpace("X-Foo", headerValue);
        this.client.DefaultRequestHeaders.Should().HaveCount(nHeaders);
    }

    [Fact]
    public void AddHeaderIfNotNullOrWhiteSpace_Working()
    {
        int nHeaders = this.client.DefaultRequestHeaders.Count();
        this.client.AddHeaderIfNotNullOrWhiteSpace("X-Foo", "foo");
        this.client.DefaultRequestHeaders.Should().HaveCount(nHeaders + 1);
        this.client.DefaultRequestHeaders.GetValues("X-Foo").Should().HaveCount(1).And.Contain("foo");
    }

    [Fact]
    public void AddHeaderIfNotNullOrWhiteSpace_UserAgent()
    {
        this.client.AddHeaderIfNotNullOrWhiteSpace("User-Agent", "foo");
        this.client.DefaultRequestHeaders.UserAgent.Should().ContainSingle("foo");
    }

    [Theory]
    [InlineData(true, DecompressionMethods.All)]
    [InlineData(false, DecompressionMethods.None)]
    public void SetAutomaticDecompression_Works(bool enable, DecompressionMethods expected)
    {
        HttpClientHandler sut = new();
        sut.SetAutomaticDecompression(enable);
        sut.AutomaticDecompression.Should().Be(expected);
    }
}
