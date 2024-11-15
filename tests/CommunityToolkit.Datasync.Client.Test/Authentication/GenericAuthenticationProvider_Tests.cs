// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Authentication;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Authentication;

[ExcludeFromCodeCoverage]
public class GenericAuthenticationProvider_Tests
{
    #region Helpers
    /// <summary>
    /// An authentication token that is expired.
    /// </summary>
    private AuthenticationToken ExpiredAuthenticationToken { get; } = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(-5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };

    /// <summary>
    /// A completely valid authentication token.
    /// </summary>
    private AuthenticationToken ValidAuthenticationToken { get; } = new()
    {
        DisplayName = "John Smith",
        ExpiresOn = DateTimeOffset.Now.AddMinutes(5),
        Token = "YmFzaWMgdG9rZW4gZm9yIHRlc3Rpbmc=",
        UserId = "the_doctor"
    };
    #endregion

    [Fact]
    public void Ctor_WhiteSpace_Header_Throws()
    {
        Action act = () => _ = new GenericAuthenticationProvider(_ => Task.FromResult(ValidAuthenticationToken), "X-ZUMO-AUTH", " ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_NullTokenRequestor_Throws()
    {
        Action act = () => _ = new GenericAuthenticationProvider(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_WhitespaceHeader_Throws(string headerName)
    {
        Action act = () => _ = new GenericAuthenticationProvider(_ => Task.FromResult(ValidAuthenticationToken), headerName);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \t ")]
    public void Ctor_Authorization_RequiresType(string authType)
    {
        Action act = () => _ = new GenericAuthenticationProvider(_ => Task.FromResult(ValidAuthenticationToken), "Authorization", authType);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_CanSetTokenRequestor()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken));
        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Bearer");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Ctor_CanDoAuthBasic()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken), "Authorization", "Basic");
        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Basic");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Ctor_CanDoAuthBearer()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken), "Authorization");
        sut.HeaderName.Should().Be("Authorization");
        sut.AuthenticationType.Should().Be("Bearer");
        sut.Current.Should().BeNull();
        sut.RefreshBufferTimeSpan.Should().BeGreaterThan(TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void RefreshBufferTimeSpan_CannotBeSmall(long ms)
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken));
        Action act = () => sut.RefreshBufferTimeSpan = TimeSpan.FromMilliseconds(ms);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RefreshBufferTimeSpan_Roundtrips()
    {
        TimeSpan ts = TimeSpan.FromMinutes(1);
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken)) { RefreshBufferTimeSpan = ts };
        sut.RefreshBufferTimeSpan.Should().Be(ts);
    }

    [Fact]
    public void IsExpired_NullToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(4) }
        };
        sut.IsExpired(sut.Current).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_InBuffer_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-1) }
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Expired_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2),
            Current = new AuthenticationToken { ExpiresOn = DateTimeOffset.Now.AddMinutes(-3) }
        };
        sut.IsExpired(sut.Current).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(ExpiredAuthenticationToken).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_BasicToken_ReturnsFalse()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(ValidAuthenticationToken).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_NoExpiration_ReturnsTrue()
    {
        AuthenticationToken authtoken = new()
        {
            DisplayName = ValidAuthenticationToken.DisplayName,
            Token = ValidAuthenticationToken.Token,
            UserId = ValidAuthenticationToken.UserId
        };
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(authtoken))
        {
            RefreshBufferTimeSpan = TimeSpan.FromMinutes(2)
        };
        sut.IsExpired(authtoken).Should().BeTrue();
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnFirstRun()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(_ => { count++; return Task.FromResult(ValidAuthenticationToken); });
        string actual = await sut.GetTokenAsync();
        actual.Should().Be(ValidAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CachesResult()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(_ => { count++; return Task.FromResult(ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        string secondCall = await sut.GetTokenAsync();
        firstCall.Should().Be(ValidAuthenticationToken.Token);
        secondCall.Should().Be(ValidAuthenticationToken.Token);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CallsOnForce()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(_ => { count++; return Task.FromResult(ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(ValidAuthenticationToken.Token);
        string secondCall = await sut.GetTokenAsync(true);
        secondCall.Should().Be(ValidAuthenticationToken.Token);
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetTokenAsync_LogsOutWhenExpired()
    {
        GenericAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken));
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(ValidAuthenticationToken.Token);
        sut.DisplayName.Should().Be(ValidAuthenticationToken.DisplayName);
        sut.UserId.Should().Be(ValidAuthenticationToken.UserId);
        sut.IsLoggedIn.Should().BeTrue();

        sut.TokenRequestorAsync = _ => Task.FromResult(ExpiredAuthenticationToken);
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
        GenericAuthenticationProvider sut = new(_ => { count++; return Task.FromResult(ValidAuthenticationToken); });
        await sut.LoginAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_ForcesTokenRequestor()
    {
        int count = 0;
        GenericAuthenticationProvider sut = new(_ => { count++; return Task.FromResult(ValidAuthenticationToken); });
        string firstCall = await sut.GetTokenAsync();
        firstCall.Should().Be(ValidAuthenticationToken.Token);
        await sut.LoginAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_AddsHeader_BearerAuth()
    {
        MockDelegatingHandler handler = new();
        handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK));
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        WrappedAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            InnerHandler = handler
        };

        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Should().HaveHeader("Authorization", $"Bearer {ValidAuthenticationToken.Token}");
    }

    [Fact]
    public async Task SendAsync_NoHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new();
        handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK));
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        WrappedAuthenticationProvider sut = new(_ => Task.FromResult(ExpiredAuthenticationToken))
        {
            InnerHandler = handler
        };

        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Should().NotHaveHeader("Authorization");
    }

    [Fact]
    public async Task SendAsync_RemoveHeader_WhenExpired()
    {
        MockDelegatingHandler handler = new();
        handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK));
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add("Authorization", "Bearer 1234");
        WrappedAuthenticationProvider sut = new(_ => Task.FromResult(ExpiredAuthenticationToken))
        {
            InnerHandler = handler
        };

        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Should().NotHaveHeader("Authorization");
    }

    [Fact]
    public async Task SendAsync_OverwritesHeader_WhenNotExpired()
    {
        MockDelegatingHandler handler = new();
        handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK));
        HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/test");
        request.Headers.Add("Authorization", "Bearer 1234");
        WrappedAuthenticationProvider sut = new(_ => Task.FromResult(ValidAuthenticationToken))
        {
            InnerHandler = handler
        };

        HttpResponseMessage response = await sut.WrappedSendAsync(request);

        response.Should().NotBeNull();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Should().HaveHeader("Authorization", $"Bearer {ValidAuthenticationToken.Token}");
    }

    /// <summary>
    /// Wrap of the <see cref="GenericAuthenticationProvider"/> that provides public access to `SendAsync()`
    /// </summary
    [ExcludeFromCodeCoverage]
    public class WrappedAuthenticationProvider(Func<CancellationToken, Task<AuthenticationToken>> requestor, string header = "Authorization", string authType = null)
        : GenericAuthenticationProvider(requestor, header, authType)
    {
        public Task<HttpResponseMessage> WrappedSendAsync(HttpRequestMessage request, CancellationToken token = default)
            => base.SendAsync(request, token);
    }
}
