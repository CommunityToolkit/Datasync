// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

using CommunityToolkit.Datasync.Common;

namespace CommunityToolkit.Datasync.Server.Abstractions.Test;

[ExcludeFromCodeCoverage]
public class Ensure_Tests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("notnull", false)]
    public void IsNotNull_Works(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNull();
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentNullException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("notwhitespace", false)]
    public void IsNotNullOrWhiteSpace_Works(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNullOrWhiteSpace();
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("notwhitespace", false)]
    public void IsNotNullOrWhiteSpace_Works_WithMessage(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNullOrWhiteSpace("because message");
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", false)]
    [InlineData("notwhitespace", false)]
    public void IsNotNullOrEmpty_Works(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNullOrEmpty();
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", false)]
    [InlineData("notwhitespace", false)]
    public void IsNotNullOrEmpty_Works_WithMessage(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNullOrEmpty("because message");
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("match", true)]
    [InlineData("midmatchmid", true)]
    [InlineData("catch", false)]
    public void Matches_Works(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).Matches(new("cat"));
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("match", true)]
    [InlineData("midmatchmid", true)]
    [InlineData("catch", false)]
    public void Matches_Works_WithMessage(string sut, bool shouldThrow)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).Matches(new("cat"), "is not a cat");
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }
}
