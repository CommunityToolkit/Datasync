// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public class ByteVersionMovie
{
    public ByteVersionMovie()
    { 
    }

    public ByteVersionMovie(object source)
    {
        if (source is ITableData metadata)
        {
            Id = metadata.Id;
            Deleted = metadata.Deleted;
            UpdatedAt = metadata.UpdatedAt;
            Version = [..metadata.Version];
        }

        if (source is IMovie movie)
        {
            BestPictureWinner = movie.BestPictureWinner;
            Duration = movie.Duration;
            Rating = movie.Rating;
            ReleaseDate = movie.ReleaseDate;
            Title = movie.Title;
            Year = movie.Year;
        }
    }

    [Key]
    public string Id { get; set; }

    [Column(TypeName = "INTEGER")]
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] Version { get; set; }
    public bool Deleted { get; set; }

    public bool BestPictureWinner { get; set; }
    public int Duration { get; set; }
    public MovieRating Rating { get; set; } = MovieRating.Unrated;
    public DateOnly ReleaseDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
}

