// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
using CommunityToolkit.Datasync.TestCommon.Models;

namespace CommunityToolkit.Datasync.Client.Test.Query.Linq;

[ExcludeFromCodeCoverage]
public class ImplicitConversion_Tests
{
    [Theory]
    [InlineData(typeof(float), typeof(int), false)]
    [InlineData(typeof(KitchenSinkState), typeof(int), true)]
    [InlineData(typeof(DateTime), typeof(DateTimeOffset), false)]
    [InlineData(typeof(int), typeof(int), true)]
    public void ImplicitConversions_Works(Type from, Type to, bool expected)
    {
        ImplicitConversions.IsImplicitConversion(from, to).Should().Be(expected);
    }
}
