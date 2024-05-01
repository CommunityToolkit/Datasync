// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class GenericAuthenticationProvider_Tests
{
    #region Test Data
    /// <summary>
    /// An authentication token that is expired.
    /// </summary>
    private readonly AuthenticationToken ExpiredAuthenticationToken = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(-5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    /// <summary>
    /// A completely valid authentication token.
    /// </summary>
    private readonly AuthenticationToken ValidAuthenticationToken = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    /// <summary>
    /// A test authentication header - we use X-ZUMO-AUTH because that is what the original code used.
    /// </summary>
    private const string TestAuthHeader = "X-ZUMO-AUTH";
    #endregion

    [Fact]
    public void Ctor_NullTokenRequestor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GenericAuthenticationProvider(null));
    }

    [Fact]
    public void Ctor_CanSetTokenRequestor()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken));

        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Bearer");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_NullHeader_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GenericAuthenticationProvider(() => Task.FromResult(this.ValidAuthenticationToken), null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_WhitespaceHeader_Throws(string headerName)
    {
        Assert.Throws<ArgumentException>(() => new GenericAuthenticationProvider(() => Task.FromResult(this.ValidAuthenticationToken), headerName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_Authorization_RequiresType(string authType)
    {
        Assert.Throws<ArgumentException>(() => new GenericAuthenticationProvider(() => Task.FromResult(this.ValidAuthenticationToken), "Authorization", authType));
    }

    [Fact]
    public void Ctor_CanDoXZumoAuth()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken), TestAuthHeader, null);

        sut.HeaderName.Should().Be(TestAuthHeader);
        sut.AuthenticationType.Should().BeNull();
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_CanDoAuthBasic()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken), "Authorization", "Basic");

        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Basic");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_CanDoAuthBearer()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken), "Authorization");

        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Bearer");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void RefreshBufferTimeSpan_CannotBeSmall(long ms)
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken));
        Assert.Throws<ArgumentException>(() => sut.RefreshBufferTimeSpan = TimeSpan.FromMilliseconds(ms));
    }

    [Theory]
    [InlineData(60)]
    public void RefreshBufferTimeSpan_Roundtrips(int secs)
    {
        TimeSpan ts = TimeSpan.FromSeconds(secs);
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken)) { RefreshBufferTimeSpan = ts };
        sut.RefreshBufferTimeSpan.Should().Be(ts);
    }

    [Fact]
    public void IsExpired_NullToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken)) { RefreshBufferTimeSpan = TimeSpan.FromMinutes(2) };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(4) }
        };
        sut.IsExpired(sut.Current).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_InBuffer_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-1) }
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Expired_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-3) }
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(this.ExpiredAuthenticationToken).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_BasicToken_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(this.ValidAuthenticationToken).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_NoExpiration_ReturnsTrue()
    {
        AuthenticationToken authtoken = new()
        {
            DisplayName = this.ValidAuthenticationToken.DisplayName,
            Token = this.ValidAuthenticationToken.Token,
            UserId = this.ValidAuthenticationToken.UserId
        };
        GenericAuthenticationProvider sut = new(() => Task.FromResult(authtoken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(authtoken).Should().BeTrue();
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnFirstRun()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult(this.ValidAuthenticationToken); });
        string actual = await sut.GetTokenAsync();
        actual.Should().Be(this.ValidAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CachesResult()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult(this.ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        string secondCall = await sut.GetTokenAsync();

        firstCall.Should().Be(this.ValidAuthenticationToken.Token);
        secondCall.Should().Be(this.ValidAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnForce()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult(this.ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(this.ValidAuthenticationToken.Token);
        string secondCall = await sut.GetTokenAsync(true);
        secondCall.Should().Be(this.ValidAuthenticationToken.Token);
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetTokenAsync_LogsOutWhenExpired()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken));
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(this.ValidAuthenticationToken.Token);
        sut.DisplayName.Should().Be(this.ValidAuthenticationToken.DisplayName);
        sut.UserId.Should().Be(this.ValidAuthenticationToken.UserId);
        sut.IsLoggedIn.Should().BeTrue();

        sut.TokenRequestorAsync = () => Task.FromResult(this.ExpiredAuthenticationToken);
        string secondCall = await sut.GetTokenAsync(true);
        secondCall.Should().BeNull();
        sut.DisplayName.Should().BeNull();
        sut.UserId.Should().BeNull();
        sut.IsLoggedIn.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_CallsTokenRequestor()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult(this.ValidAuthenticationToken); });
        await sut.LoginAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_ForcesTokenRequestor()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult(this.ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(this.ValidAuthenticationToken.Token);
        await sut.LoginAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_AddsHeader_BearerAuth()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        WrappedAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken)) { InnerHandler = handler };
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Should().HaveHeader("Authorization", $"Bearer {this.ValidAuthenticationToken.Token}");
    }

    [Fact]
    public async Task SendAsync_AddsHeader_ZumoAuth()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        WrappedAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken), TestAuthHeader) { InnerHandler = handler };
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Should().HaveHeader(TestAuthHeader, this.ValidAuthenticationToken.Token);
    }

    [Fact]
    public async Task SendAsync_NoHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        WrappedAuthenticationProvider sut = new(() => Task.FromResult(this.ExpiredAuthenticationToken), TestAuthHeader) { InnerHandler = handler };
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Should().NotHaveHeader(TestAuthHeader);
    }

    [Fact]
    public async Task SendAsync_RemoveHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add(TestAuthHeader, "a-test-header");
        WrappedAuthenticationProvider sut = new(() => Task.FromResult(this.ExpiredAuthenticationToken), TestAuthHeader) { InnerHandler = handler };
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Should().NotHaveHeader(TestAuthHeader);
    }

    [Fact]
    public async Task SendAsync_OverwritesHeader_WhenNotExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add(TestAuthHeader, "a-test-header");
        WrappedAuthenticationProvider sut = new(() => Task.FromResult(this.ValidAuthenticationToken), TestAuthHeader) { InnerHandler = handler };
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Should().HaveHeader(TestAuthHeader, this.ValidAuthenticationToken.Token);
    }
}
