// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Spatial;

namespace CommunityToolkit.Datasync.Server.OData;

/// <summary>
/// Implements the geospatial functions for the OData service.
/// </summary>
internal static class GeospatialFunctions
{
    private const double R = 6371; // Radius of Earth in km

    /// <summary>
    /// The distance between two points.
    /// </summary>
    /// <param name="p0">The first point.</param>
    /// <param name="p1">The second point.</param>
    internal static double GeoDistance(GeographyPoint p0, GeographyPoint p1)
    {
        if (p0 == null || p1 == null)
        {
            return double.NaN;
        }
        else
        {
            return DistanceBetweenPlaces(p0.Latitude, p0.Longitude, p1.Latitude, p1.Longitude);
        }
    }

    /// <summary>
    /// Calculates the distance between two points on a sphere.
    /// </summary>
    /// <remarks>
    /// cos(d) = sin(φА)·sin(φB) + cos(φА)·cos(φB)·cos(λА − λB),
    ///  where φА, φB are latitudes and λА, λB are longitudes
    /// Distance = d * R
    /// </remarks>
    /// <param name="lon1">Longitude of the first point</param>
    /// <param name="lat1">Latitude of the first point</param>
    /// <param name="lon2">Longitude of the second point</param>
    /// <param name="lat2">Latitude of the second point</param>
    public static double DistanceBetweenPlaces(double lon1, double lat1, double lon2, double lat2)
    {
        if (lat1 == lat2 && lon1 == lon2)
        {
            return 0.0;
        }

        double sLat1 = Math.Sin(Radians(lat1));
        double sLat2 = Math.Sin(Radians(lat2));
        double cLat1 = Math.Cos(Radians(lat1));
        double cLat2 = Math.Cos(Radians(lat2));
        double cLon = Math.Cos(Radians(lon1) - Radians(lon2));

        double d = Math.Acos((sLat1 * sLat2) + (cLat1 * cLat2 * cLon));
        return R * d;
    }

    /// <summary>
    /// Convert degrees to Radians
    /// </summary>
    /// <param name="degrees">Degrees</param>
    /// <returns>The equivalent in radians</returns>
    public static double Radians(double degrees)
        => degrees * Math.PI / 180;
}
