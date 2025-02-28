// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon.Models;

namespace CommunityToolkit.Datasync.Server.CosmosDb.Test.Models;
public class CosmosDbMovie : CosmosTableData, IMovie
{
    public bool BestPictureWinner { get; set; }

    public int Duration { get; set; }

    public MovieRating Rating { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public string Title { get; set; }

    public int Year { get; set; }
}
