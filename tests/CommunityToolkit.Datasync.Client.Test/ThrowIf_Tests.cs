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
}
