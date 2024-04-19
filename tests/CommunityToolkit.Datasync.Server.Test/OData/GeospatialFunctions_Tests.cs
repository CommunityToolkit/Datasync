// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.OData;
using Microsoft.Spatial;

namespace CommunityToolkit.Datasync.Server.Test.OData;

[ExcludeFromCodeCoverage]
public class GeospatialFunctions_Tests
{
    [Fact]
    public void GeoDistance_NullArg_ReturnsNaN()
    {
        GeographyPoint p0 = null;
        GeographyPoint p1 = GeographyPoint.Create(0, 0);

        GeospatialFunctions.GeoDistance(p0, p1).Should().Be(double.NaN);
        GeospatialFunctions.GeoDistance(p1, p0).Should().Be(double.NaN);
    }
}
