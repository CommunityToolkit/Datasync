// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

using CommunityToolkit.Datasync.Common;

namespace CommunityToolkit.Datasync.Common.Test.Guards;

[ExcludeFromCodeCoverage]
public class Ensure_Tests
{
    #region Test Cases
    public static IEnumerable<UriTestCase> GetUriTestCases()
    {
        List<UriTestCase> testCases = [
            new UriTestCase { Endpoint = new Uri("", UriKind.Relative), IsAbsolute = false, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://foo.azurewebsites.net"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://foo.azure-api.net"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("foo/bar", UriKind.Relative), IsAbsolute = false, IsValid = false },
            new UriTestCase { Endpoint = new Uri("file://localhost/foo"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://[2001:db8:0:b:0:0:0:1A]"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://[2001:db8:0:b:0:0:0:1A]:3000"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://10.0.0.8"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://10.0.0.8:3000"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://10.0.0.8:3000/myapi"), IsAbsolute = true, IsValid = false },
            new UriTestCase { Endpoint = new Uri("http://localhost/tables/api"), IsAbsolute = true, IsValid = true },
            new UriTestCase { Endpoint = new Uri("https://foo.azurewebsites.net"), IsAbsolute = true, IsValid = true }
        ];
        return testCases;
    }

    public static IEnumerable<IdTestCase> GetIdTestCases()
    {
        List<IdTestCase> testCases = [
            new IdTestCase { Id = "", IsValid = false },
            new IdTestCase { Id = " ", IsValid = false },
            new IdTestCase { Id = "\t", IsValid = false },
            new IdTestCase { Id = "abcdef gh", IsValid = false },
            new IdTestCase { Id = "?", IsValid = false },
            new IdTestCase { Id = ";", IsValid = false },
            new IdTestCase { Id = "{EA235ADF-9F38-44EA-8DA4-EF3D24755767}", IsValid = false },
            new IdTestCase { Id = "###", IsValid = false },
            new IdTestCase { Id = "!!!", IsValid = false },
            new IdTestCase { Id = "db0ec08d-46a9-465d-9f5e-0066a3ee5b5f", IsValid = true },
            new IdTestCase { Id = "0123456789", IsValid = true },
            new IdTestCase { Id = "abcdefgh", IsValid = true },
            new IdTestCase { Id = "2023|05|01_120000", IsValid = true },
            new IdTestCase { Id = "db0ec08d_46a9_465d_9f5e_0066a3ee5b5f", IsValid = true },
            new IdTestCase { Id = "db0ec08d.46a9.465d.9f5e.0066a3ee5b5f", IsValid = true }
        ];
        return testCases;
    }

    public static IEnumerable<StringTestCase> GetStringTestCases()
    {
        List<StringTestCase> testCases = [
            new StringTestCase { String = null, IsEmpty = false, IsNull = true, IsWhitespace = false },
            new StringTestCase { String = "", IsEmpty = true, IsNull = false, IsWhitespace = false },
            new StringTestCase { String = " ", IsEmpty = false, IsNull = false, IsWhitespace = true },
            new StringTestCase { String = "   ", IsEmpty = false, IsNull = false, IsWhitespace = true },
            new StringTestCase { String = " a test", IsEmpty = false, IsNull = false, IsWhitespace = false }
        ];
        return testCases;
    }

    public static IEnumerable<IdTestCase> GetHttpHeaderTestCases()
    {
        List<IdTestCase> testCases = [
            new IdTestCase { Id = "X-ZUMO-AUTH", IsValid = true },
            new IdTestCase { Id = "0_BAD", IsValid = false },
            new IdTestCase { Id = " HEADER", IsValid = false }
        ];
        return testCases;
    }
    #endregion

    [Theory, CombinatorialData]
    public void HasCount_works([CombinatorialRange(0, 5)] int count, bool withMessage)
    {
        IList<int> sut = new List<int>([1, 2, 3]);
        bool shouldThrow = count != 3;
        Action act = () => Ensure.That(sut, nameof(sut)).IsNotNull().And.HasCount(count, withMessage ? "because" : null);
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory, CombinatorialData]
    public void HasItems_Works(bool withMessage)
    {
        IList<int> sut1 = [];
        Action a1 = () => Ensure.That(sut1, nameof(sut1)).HasItems(withMessage ? "because" : null);
        a1.Should().Throw<ArgumentException>();

        IList<int> sut2 = new List<int>([1, 2, 3]);
        Action a2 = () => Ensure.That(sut2, nameof(sut2)).HasItems(withMessage ? "because" : null);
        a2.Should().NotThrow();

        IList<int> sut3 = new List<int>([1, 2, 3]);
        Action a3 = () => Ensure.That(sut3, nameof(sut3)).HasItems(withMessage ? "because" : null).And.HasCount(3);
        a3.Should().NotThrow();
    }

    [Theory, CombinatorialData]
    public void IsAbsoluteUri_Works([CombinatorialMemberData(nameof(GetUriTestCases))] UriTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.Endpoint, nameof(testCase)).IsAbsoluteUri(withMessage ? "because" : null);
        if (testCase.IsAbsolute)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<UriFormatException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsDatasyncEndpoint_Works([CombinatorialMemberData(nameof(GetUriTestCases))] UriTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.Endpoint, nameof(testCase)).IsDatasyncEndpoint(withMessage ? "because" : null);
        if (testCase.IsValid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<UriFormatException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsGt_Int_Works([CombinatorialRange(0, 10)] int sut, bool withMessage)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsGt(5, withMessage ? "because" : null);
        if (sut > 5)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsGte_Int_Works([CombinatorialRange(0, 10)] int sut, bool withMessage)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsGte(5, withMessage ? "because" : null);
        if (sut >= 5)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsGte_TimeSpan_Works([CombinatorialRange(0, 600, 10)] int seconds, bool withMessage)
    {
        TimeSpan sut = TimeSpan.FromSeconds(seconds);
        TimeSpan comparison = TimeSpan.FromSeconds(300);
        Action act = () => Ensure.That(sut, nameof(sut)).IsGte(comparison, withMessage ? "because" : null);
        if (seconds >= 300)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsHttpHeaderName_Works([CombinatorialMemberData(nameof(GetHttpHeaderTestCases))] IdTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.Id, nameof(testCase)).IsHttpHeaderName(withMessage ? "because" : null);
        if (testCase.IsValid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsInRange_Works([CombinatorialRange(0, 10)] int sut, bool withMessage)
    {
        Action act = () => Ensure.That(sut, nameof(sut)).IsInRange(3, 5, withMessage ? "because" : null);
        if (sut is >= 3 and <= 5)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsValidId_Works([CombinatorialMemberData(nameof(GetIdTestCases))] IdTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.Id, nameof(testCase)).IsValidId(withMessage ? "because" : null);
        if (testCase.IsValid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Theory, CombinatorialData]
    public void IsNotNull_Works([CombinatorialMemberData(nameof(GetStringTestCases))] StringTestCase testCase)
    {
        Action act = () => Ensure.That(testCase.String, nameof(testCase)).IsNotNull();
        if (testCase.IsNull)
        {
            act.Should().Throw<ArgumentNullException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory, CombinatorialData]
    public void IsNotNullOrEmpty_Works([CombinatorialMemberData(nameof(GetStringTestCases))] StringTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.String, nameof(testCase)).IsNotNullOrEmpty(withMessage ? "because" : null);
        if (testCase.IsNull || testCase.IsEmpty)
        {
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory, CombinatorialData]
    public void IsNotNullOrWhiteSpace_Works([CombinatorialMemberData(nameof(GetStringTestCases))] StringTestCase testCase, bool withMessage)
    {
        Action act = () => Ensure.That(testCase.String, nameof(testCase)).IsNotNullOrWhiteSpace(withMessage ? "because" : null);
        if (testCase.IsNull || testCase.IsEmpty || testCase.IsWhitespace)
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

    public class UriTestCase
    {
        public Uri Endpoint { get; set; }
        public bool IsAbsolute { get; set; }
        public bool IsValid { get; set; }
    }

    public class IdTestCase
    {
        public string Id { get; set; }
        public bool IsValid { get; set; }
    }

    public class StringTestCase
    {
        public string String { get; set; }
        public bool IsNull { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsWhitespace { get; set; }
    }
}
