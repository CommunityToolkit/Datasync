// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class AuthenticationToken_Tests
{
    #region Test Data
    /// <summary>
    /// A completely valid authentication token.
    /// </summary>
    private readonly AuthenticationToken ValidAuthenticationToken = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = new DateTimeOffset(2024, 12, 24, 01, 23, 45, TimeSpan.Zero),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    private readonly AuthenticationToken NullAuthenticationToken = new()
    {
        ExpiresOn = DateTimeOffset.MinValue
    };
    #endregion

    [Fact]
    public void ToString_Valid_ReturnsExpected()
    {
        string actual = this.ValidAuthenticationToken.ToString();
        actual.Should().Be("AuthenticationToken(DisplayName=\"John Smith\",ExpiresOn=\"12/24/2024 1:23:45 AM +00:00\",Token=\"YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=\",UserId=\"the_doctor\")");
    }

    [Fact]
    public void ToString_Null_ReturnsExpected()
    {
        string actual = this.NullAuthenticationToken.ToString();
        actual.Should().Be("AuthenticationToken(DisplayName=\"\",ExpiresOn=\"1/1/0001 12:00:00 AM +00:00\",Token=\"\",UserId=\"\")");
    }
}
