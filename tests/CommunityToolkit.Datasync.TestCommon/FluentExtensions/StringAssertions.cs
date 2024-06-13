// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using FluentAssertions;

namespace CommunityToolkit.Datasync.TestCommon;

public static class FluentStringAssertions
{
    /// <summary>
    /// An extension to FluentAssertions to validate that a string is a GUID.
    /// </summary>
    public static AndConstraint<StringAssertions> BeAGuid(this StringAssertions current, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Guid.TryParse(current.Subject, out _))
            .FailWith("Expected object to be a Guid, but found {0}", current.Subject);
        return new AndConstraint<StringAssertions>(current);
    }
}
