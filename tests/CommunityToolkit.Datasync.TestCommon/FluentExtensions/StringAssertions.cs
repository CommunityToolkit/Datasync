// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AwesomeAssertions.Primitives;
using AwesomeAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace CommunityToolkit.Datasync.TestCommon;

[ExcludeFromCodeCoverage]
public static class FluentStringAssertions
{
    /// <summary>
    /// An extension to AwesomeAssertions to validate that a string is a GUID.
    /// </summary>
    public static AndConstraint<StringAssertions> BeAGuid(this StringAssertions current, string because = "", params object[] becauseArgs)
    {
        current.CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Guid.TryParse(current.Subject, out _))
            .FailWith("Expected object to be a Guid, but found {0}", current.Subject);
        return new AndConstraint<StringAssertions>(current);
    }

    public static AndConstraint<StringAssertions> MatchQueryString(this StringAssertions current, string queryString, string because = "", params object[] becauseArgs)
    {
        Dictionary<string, StringValues> q1 = QueryHelpers.ParseNullableQuery(queryString) ?? [];
        Dictionary<string, StringValues> q2 = QueryHelpers.ParseNullableQuery(current.Subject) ?? [];
        bool isEquivalent = q1.Count == q2.Count && !q1.Except(q2).Any();

        current.CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(isEquivalent)
            .FailWith("Expected query string to match '{0}', but found '{1}'", queryString, current.Subject);
        return new AndConstraint<StringAssertions>(current);
    }
}
