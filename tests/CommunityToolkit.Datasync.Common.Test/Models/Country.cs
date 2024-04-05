// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Common.Test.Models;

/// <summary>
/// The model returned by the <see cref="CountryData.GetCountries"/> method.
/// </summary>
[ExcludeFromCodeCoverage]
public class Country
{
    /// <summary>
    /// The 2-letter ISO code for the country.
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>
    /// The official name of the country.
    /// </summary>
    public string CountryName { get; set; } = string.Empty;

    /// <summary>
    /// The latitude of the country's capital city.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the country's capital city.
    /// </summary>
    public double Longitude { get; set; }
}
