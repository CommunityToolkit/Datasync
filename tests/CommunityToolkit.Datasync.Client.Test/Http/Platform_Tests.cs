// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class Platform_Tests
{
    [Fact]
    public void AssemblyVersion_IsNotEmpty()
    {
        Platform.AssemblyVersion.Should().NotBeEmpty();
    }

    [Fact]
    public void UserAgentDetails_IsNotEmpty()
    {
        Platform.UserAgentDetails.Should().NotBeEmpty();
    }
}
