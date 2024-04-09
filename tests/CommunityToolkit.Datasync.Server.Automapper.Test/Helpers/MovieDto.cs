// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;

namespace CommunityToolkit.Datasync.Server.Automapper.Test.Helpers;

[ExcludeFromCodeCoverage]
public class MovieDto : ITableData, IMovie
{
    #region ITableData
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool Deleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public byte[] Version { get; set; } = [];
    #endregion

    #region IMovie
    /// <inheritdoc />
    public bool BestPictureWinner { get; set; }

    /// <inheritdoc />
    public int Duration { get; set; }

    /// <inheritdoc />
    public MovieRating Rating { get; set; }

    /// <inheritdoc />
    public DateOnly ReleaseDate { get; set; }

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public int Year { get; set; }
    #endregion

    /// <inheritdoc />
    public bool Equals(ITableData other)
        => other != null && Id == other.Id && Version.SequenceEqual(other.Version);
}
