// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Common;

/// <summary>
/// A set of common extension methods for the Datasync application and service.
/// </summary>
public static class DatasyncExtensions
{
    /// <summary>
    /// Creates a deep clone of an entity using JSON serialization.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to clone.</typeparam>
    /// <param name="entity">The entity to clone.</param>
    /// <returns>The cloned entity.</returns>
    public static TEntity Clone<TEntity>(this TEntity entity)
    {
        JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;
        return JsonSerializer.Deserialize<TEntity>(JsonSerializer.Serialize(entity, options), options)!;
    }
}
