// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnsureThat;

namespace CommunityToolkit.Datasync.Client.Test.Extensions;

[ExcludeFromCodeCoverage]
public class EnsureThatExtensions_Tests
{
    #region IsValidDatasyncUri()
    [Fact]
    public void IsValidEndpoint_Null_Throws()
    {
        Uri sut = null;
        Assert.Throws<ArgumentNullException>(() => Ensure.That(sut).IsValidDatasyncUri());
    }

    [Theory]
    [InlineData("file://localhost/foo", false)]
    [InlineData("http://foo.azurewebsites.net", false)]
    [InlineData("http://foo.azure-api.net", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000", false)]
    [InlineData("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi", false)]
    [InlineData("http://10.0.0.8", false)]
    [InlineData("http://10.0.0.8:3000", false)]
    [InlineData("http://10.0.0.8:3000/myapi", false)]
    [InlineData("foo/bar", true)]
    [Trait("Method", "IsValidEndpoint(Uri,string)")]
    public void IsValidEndpoint_Invalid_Throws(string endpoint, bool isRelative = false)
    {
        Uri sut = isRelative ? new Uri(endpoint, UriKind.Relative) : new Uri(endpoint);
        Assert.Throws<UriFormatException>(() => Ensure.That(sut).IsValidDatasyncUri());
    }

    [Theory, ClassData(typeof(EndpointTestCases))]
    [Trait("Method", "IsValidEndpoint(Uri,string)")]
    public void IsValidEndpoint_Valid_Passes(EndpointTestCase testcase)
    {
        Uri sut = new(testcase.BaseEndpoint);
        Ensure.That(sut).IsValidDatasyncUri();
    }
    #endregion
}
