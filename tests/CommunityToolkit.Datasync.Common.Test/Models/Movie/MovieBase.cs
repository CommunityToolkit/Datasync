// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Common.Test.Models;

/// <summary>
/// The base class for the movie data, implementing <see cref="IMovie"/>.
/// </summary>
public class MovieBase : IMovie
{
    /// <summary>
    /// True if the movie won the oscar for Best Picture
    /// </summary>
    public bool BestPictureWinner { get; set; }

    /// <summary>
    /// The running time of the movie
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// The MPAA rating for the movie, if available.
    /// </summary>
    public MovieRating Rating { get; set; }

    /// <summary>
    /// The release date of the movie.
    /// </summary>
    public DateOnly ReleaseDate { get; set; }

    /// <summary>
    /// The title of the movie.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The year that the movie was released.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Copies the current data into another IMovie based object.
    /// </summary>
    /// <param name="other"></param>
    public void CopyTo(IMovie other)
    {
        other.BestPictureWinner = BestPictureWinner;
        other.Duration = Duration;
        other.Rating = Rating;
        other.ReleaseDate = ReleaseDate;
        other.Title = Title;
        other.Year = Year;
    }
}
