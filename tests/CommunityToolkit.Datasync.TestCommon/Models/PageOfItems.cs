// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.TestCommon.Models;

/// <summary>
/// The deserialized content from a paging operation.
/// </summary>
/// <typeparam name="T">The type of entity being transmitted</typeparam>
[ExcludeFromCodeCoverage]
public class PageOfItems<T> where T : class
{
    public T[] Items { get; set; }
    public long? Count { get; set; }
    public string NextLink { get; set; }
}
