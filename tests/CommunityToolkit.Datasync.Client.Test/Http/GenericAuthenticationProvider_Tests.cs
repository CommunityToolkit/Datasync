// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Common.Test.Mocks;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Http;

[ExcludeFromCodeCoverage]
public class GenericAuthenticationProvider_Tests
{
    /// <summary>
    /// An authentication token that is expired.
    /// </summary>
    private readonly AuthenticationToken expiredAuthenticationToken = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(-5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    /// <summary>
    /// A completely valid authentication token.
    /// </summary>
    private readonly AuthenticationToken validAuthenticationToken = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    /// <summary>
    /// A test header name - we use this name because the project used to use this header for authentication.
    /// </summary>
    private const string testHeaderName = "X-ZUMO-AUTH";

    [Fact]
    public void Ctor_NullTokenRequestor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GenericAuthenticationProvider(null));
    }

    [Fact]
    public void Ctor_CanSetTokenRequestor()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken));

        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Bearer");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_NullHeader_Throws()
    {
        Action act = () => _ = new GenericAuthenticationProvider(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), null);
        act.Should().Throw<NullReferenceException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_WhitespaceHeader_Throws(string headerName)
    {
        Action act = () => _ = new GenericAuthenticationProvider(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), headerName);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_Authorization_RequiresType(string authType)
    {
        Action act = () => _ = new GenericAuthenticationProvider(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), "Authorization", authType);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_CanDoOldAuth()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), testHeaderName, null);

        sut.HeaderName.Should().Be(testHeaderName);
        sut.AuthenticationType.Should().BeNull();
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_CanDoAuthBasic()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), "Authorization", "Basic");

        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Basic");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Ctor_CanDoAuthBearer()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), "Authorization");

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
        Action act = () => _ = new GenericAuthenticationProvider(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken)) { RefreshBufferTimeSpan = TimeSpan.FromMilliseconds(ms) };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RefreshBufferTimeSpan_Roundtrips()
    {
        TimeSpan ts = TimeSpan.FromMinutes(1);
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken)) { RefreshBufferTimeSpan = ts };
        sut.RefreshBufferTimeSpan.Should().Be(ts);
    }

    [Fact]
    public void IsExpired_NullToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken)) { RefreshBufferTimeSpan = TimeSpan.FromMinutes(2) };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(4) }
        };
        sut.IsExpired(sut.Current).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_InBuffer_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-1) }
        };
        Assert.True(sut.IsExpired(sut.Current));
    }

    [Fact]
    public void IsExpired_Expired_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-3) }
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(this.expiredAuthenticationToken).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_BasicToken_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(this.validAuthenticationToken).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_NoExpiration_ReturnsTrue()
    {
        AuthenticationToken authtoken = new()
        {
            DisplayName = this.validAuthenticationToken.DisplayName,
            Token = this.validAuthenticationToken.Token,
            UserId = this.validAuthenticationToken.UserId
        };
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(authtoken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(authtoken).Should().BeTrue();
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnFirstRun()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken); });
        string actual = await sut.GetTokenAsync();

        actual.Should().Be(this.validAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CachesResult()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        string secondCall = await sut.GetTokenAsync();

        firstCall.Should().Be(this.validAuthenticationToken.Token);
        secondCall.Should().Be(this.validAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnForce()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken); });
        (await sut.GetTokenAsync()).Should().Be(this.validAuthenticationToken.Token);
        (await sut.GetTokenAsync(true)).Should().Be(this.validAuthenticationToken.Token);
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetTokenAsync_LogsOutWhenExpired()
    {
        GenericAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken));
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(this.validAuthenticationToken.Token);
        sut.DisplayName.Should().Be(this.validAuthenticationToken.DisplayName);
        sut.UserId.Should().Be(this.validAuthenticationToken.UserId);
        sut.IsLoggedIn.Should().BeTrue();

        sut.TokenRequestorAsync = () => Task.FromResult<AuthenticationToken?>(this.expiredAuthenticationToken);
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
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken); });
        await sut.LoginAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_ForcesTokenRequestor()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(() => { count++; return Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken); });
        (await sut.GetTokenAsync()).Should().Be(this.validAuthenticationToken.Token);
        await sut.LoginAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_AddsHeader_BearerAuth()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        WrappedAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken)) { InnerHandler = handler };

        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.Authorization.Parameter.Should().Be(this.validAuthenticationToken.Token);
        handler.Requests[0].Headers.Authorization.Scheme.Should().Be("Bearer");
    }

    [Fact]
    public async Task SendAsync_AddsHeader_ZumoAuth()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        WrappedAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), testHeaderName) { InnerHandler = handler };

        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.GetValues(testHeaderName).FirstOrDefault().Should().Be(this.validAuthenticationToken.Token);
    }

    [Fact]
    public async Task SendAsync_NoHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        WrappedAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.expiredAuthenticationToken), testHeaderName) { InnerHandler = handler };

        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.Should().NotContainKey(testHeaderName);
    }

    [Fact]
    public async Task SendAsync_RemoveHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        WrappedAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.expiredAuthenticationToken), testHeaderName) { InnerHandler = handler };

        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add(testHeaderName, "a-test-header");
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.Should().NotContainKey(testHeaderName);
    }

    [Fact]
    public async Task SendAsync_OverwritesHeader_WhenNotExpired()
    {
        MockDelegatingHandler handler = new([new HttpResponseMessage(HttpStatusCode.OK)]);
        WrappedAuthenticationProvider sut = new(() => Task.FromResult<AuthenticationToken?>(this.validAuthenticationToken), testHeaderName) { InnerHandler = handler };

        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add(testHeaderName, "a-test-header");
        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.GetValues(testHeaderName).FirstOrDefault().Should().Be(this.validAuthenticationToken.Token);
    }
}
