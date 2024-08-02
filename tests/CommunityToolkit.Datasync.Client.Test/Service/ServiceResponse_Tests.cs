// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Service;
using System.Net;

namespace CommunityToolkit.Datasync.Client.Test.Service;

[ExcludeFromCodeCoverage]
public class ServiceResponse_Tests
{
    [Theory]
    [InlineData(HttpStatusCode.Continue, false)]
    [InlineData(HttpStatusCode.SwitchingProtocols, false)]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.Accepted, false)]
    [InlineData(HttpStatusCode.NonAuthoritativeInformation, false)]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.ResetContent, false)]
    [InlineData(HttpStatusCode.PartialContent, false)]
    [InlineData(HttpStatusCode.MultipleChoices, false)]
    [InlineData(HttpStatusCode.MovedPermanently, false)]
    [InlineData(HttpStatusCode.Redirect, false)]
    [InlineData(HttpStatusCode.RedirectMethod, false)]
    [InlineData(HttpStatusCode.NotModified, false)]
    [InlineData(HttpStatusCode.UseProxy, false)]
    [InlineData(HttpStatusCode.Unused, false)]
    [InlineData(HttpStatusCode.TemporaryRedirect, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.PaymentRequired, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.MethodNotAllowed, false)]
    [InlineData(HttpStatusCode.NotAcceptable, false)]
    [InlineData(HttpStatusCode.ProxyAuthenticationRequired, false)]
    [InlineData(HttpStatusCode.RequestTimeout, false)]
    [InlineData(HttpStatusCode.Conflict, false)]
    [InlineData(HttpStatusCode.Gone, false)]
    [InlineData(HttpStatusCode.LengthRequired, false)]
    [InlineData(HttpStatusCode.PreconditionFailed, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.RequestUriTooLong, false)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, false)]
    [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable, false)]
    [InlineData(HttpStatusCode.ExpectationFailed, false)]
    [InlineData(HttpStatusCode.UpgradeRequired, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.NotImplemented, false)]
    [InlineData(HttpStatusCode.BadGateway, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    [InlineData(HttpStatusCode.GatewayTimeout, false)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, false)]
    public void IsSuccessful_Works(HttpStatusCode statusCode, bool expected)
    {
        HttpResponseMessage response = new(statusCode);
        ServiceResponse sut = new(response);
        sut.IsSuccessful.Should().Be(expected);
    }

    [Fact]
    public void TryGetHeader_Works()
    {
        HttpResponseMessage responseMessage = new(HttpStatusCode.OK);
        responseMessage.Headers.Location = new Uri("http://localhost");
        ServiceResponse sut = new(responseMessage);

        bool t1 = sut.TryGetHeader("Location", out string v1);
        t1.Should().BeTrue();
        v1.Should().Be("http://localhost/");

        bool t2 = sut.TryGetHeader("ETag", out string v2);
        t2.Should().BeFalse();
        v2.Should().BeNull();
    }
}
