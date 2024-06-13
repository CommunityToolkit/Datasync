// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;

namespace CommunityToolkit.Datasync.TestCommon;

[ExcludeFromCodeCoverage]
public static class LibraryExtensions
{
    /// <summary>
    /// Creates a copy of an <see cref="ITableData"/> into a base table data object.
    /// </summary>
    /// <typeparam name="T">The type of base table data object to create.</typeparam>
    /// <param name="entity">The entity to copy.</param>
    /// <returns>A copy of the original entity.</returns>
    public static T ToTableEntity<T>(this ITableData entity) where T : ITableData, new() => new()
    {
        Id = entity.Id,
        Deleted = entity.Deleted,
        UpdatedAt = entity.UpdatedAt,
        Version = (byte[])entity.Version.Clone()
    };
}
