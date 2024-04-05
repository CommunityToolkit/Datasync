// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Common.Test.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MovieRating
{
    Unrated,
    G,
    PG,
    PG13,
    NC17,
    R
}

/// <summary>
/// An abstraction of the movie data used for query and read tests. This
/// data is provided because it is one of each type that we support.
/// </summary>
public interface IMovie
{
    /// <summary>
    /// True if the movie won the oscar for Best Picture
    /// </summary>
    bool BestPictureWinner { get; set; }

    /// <summary>
    /// The running time of the movie
    /// </summary>
    int Duration { get; set; }

    /// <summary>
    /// The MPAA rating for the movie, if available.
    /// </summary>
    MovieRating Rating { get; set; }

    /// <summary>
    /// The release date of the movie.
    /// </summary>
    DateOnly ReleaseDate { get; set; }

    /// <summary>
    /// The title of the movie.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// The year that the movie was released.
    /// </summary>
    int Year { get; set; }
}
