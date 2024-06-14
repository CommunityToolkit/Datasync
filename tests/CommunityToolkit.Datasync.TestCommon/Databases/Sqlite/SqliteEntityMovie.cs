// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class SqliteEntityMovie : SqliteEntityTableData, IMovie, IEquatable<IMovie>
{
    /// <summary>
    /// True if the movie won the oscar for Best Picture
    /// </summary>
    public bool BestPictureWinner { get; set; }

    /// <summary>
    /// The running time of the movie
    /// </summary>
    [Required]
    [Range(60, 360)]
    public int Duration { get; set; }

    /// <summary>
    /// The MPAA rating for the movie, if available.
    /// </summary>
    public MovieRating Rating { get; set; } = MovieRating.Unrated;

    /// <summary>
    /// The release date of the movie.
    /// </summary>
    [Required]
    public DateOnly ReleaseDate { get; set; }

    /// <summary>
    /// The title of the movie.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The year that the movie was released.
    /// </summary>
    [Required]
    [Range(1920, 2030)]
    public int Year { get; set; }

    /// <summary>
    /// Determines if this movie has the same content as another movie.
    /// </summary>
    /// <param name="other">The other movie</param>
    /// <returns>true if the content is the same</returns>
    public bool Equals(IMovie other)
        => other != null
        && other.BestPictureWinner == BestPictureWinner
        && other.Duration == Duration
        && other.Rating == Rating
        && other.ReleaseDate == ReleaseDate
        && other.Title == Title
        && other.Year == Year;
}
