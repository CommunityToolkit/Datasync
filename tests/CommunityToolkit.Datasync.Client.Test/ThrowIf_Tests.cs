// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;

namespace CommunityToolkit.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public class ThrowIf_Tests
{
    [Theory, MemberData(nameof(EndpointTestCases.InvalidEndpointTestCases), MemberType = typeof(EndpointTestCases))]
    public void ThrowIf_IsNotValidEndpoint_Throws(Uri endpoint)
    {
        Action act = () => ThrowIf.IsNotValidEndpoint(endpoint, nameof(endpoint));
        act.Should().Throw<UriFormatException>();
    }

    [Theory, MemberData(nameof(EndpointTestCases.ValidEndpointTestCases), MemberType = typeof(EndpointTestCases))]
    public void ThrowIf_IsNotValidEndpoint_Passed(Uri endpoint)
    {
        Action act = () => ThrowIf.IsNotValidEndpoint(endpoint, nameof(endpoint));
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("\"")]
    [InlineData("\t")]
    [InlineData("\x7F")]
    public void ThrowIf_IsInvalidETag_Throws(string etag)
    {
        Action act = () => ThrowIf.IsInvalidETag(etag, nameof(etag));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowIf_IsNotNull_Throws()
    {
        string sut = "something";
        Action act = () => ThrowIf.IsNotNull(sut, nameof(sut));
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ThrowIf_IsNotNullOrEmpty_Passes(string sut)
    {
        Action act = () => ThrowIf.IsNotNullOrEmpty(sut, nameof(sut));
        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIf_IsNotNullOrEmpty_Throws()
    {
        string sut = "something";
        Action act = () => ThrowIf.IsNotNullOrEmpty(sut, nameof(sut));
        act.Should().Throw<ArgumentException>();
    }
}
