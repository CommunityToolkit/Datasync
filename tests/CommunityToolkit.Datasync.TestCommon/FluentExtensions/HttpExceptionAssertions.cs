// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using FluentAssertions;
using FluentAssertions.Specialized;

namespace CommunityToolkit.Datasync.TestCommon;

[ExcludeFromCodeCoverage]
public static class FluentHttpExceptionAssertions
{
    /// <summary>
    /// An extension to FluentAssertions to validate the payload of a <see cref="HttpException"/>.
    /// </summary>
    public static AndConstraint<ExceptionAssertions<HttpException>> WithPayload(this ExceptionAssertions<HttpException> current, object expectedPayload, string because = "", params object[] becauseArgs)
    {
        object actualPayload = current.Subject.First().Payload;
        actualPayload.Should().NotBeNull();

        // The UpdatedAt field should be within 1msec of the subject.
        actualPayload.Should().BeEquivalentTo(expectedPayload, options => options
            .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1)))
            .WhenTypeIs<DateTimeOffset>(), because, becauseArgs);

        return new AndConstraint<ExceptionAssertions<HttpException>>(current);
    }

    /// <summary>
    /// An extension to FluentAssertions to validate the StatusCode of a <see cref="HttpException"/>
    /// </summary>
    public static AndConstraint<ExceptionAssertions<HttpException>> WithStatusCode(this ExceptionAssertions<HttpException> current, int statusCode, string because = "", params object[] becauseArgs)
    {
        current.Subject.First().StatusCode.Should().Be(statusCode, because, becauseArgs);
        return new AndConstraint<ExceptionAssertions<HttpException>>(current);
    }
}
