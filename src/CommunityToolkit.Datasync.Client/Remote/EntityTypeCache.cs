// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Datasync.Client.Remote;

/// <summary>
/// A helper class that stores the reflection values for an entity.
/// </summary>
internal static class EntityTypeCache
{
    /// <summary>
    /// Returns the entity ID, or throws an exception if the entity ID is not available as a string.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity ID.</returns>
    /// <exception cref="MissingMemberException">Thrown if there is no ID in the entity.</exception>
    /// <exception cref="FieldAccessException">Thrown if the Id field is protected, private, or static.</exception>
    internal static string? GetEntityId<T>(T entity) where T : notnull
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the entity version as a string.  If the version is available but is
    /// the wrong type (not a string or byte[]), then an exception is thrown.  If the
    /// version is available but null/empty, then an empty string is returned.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity version, or an empty string.</returns>
    /// <exception cref="InvalidCastException">Thrown if the Version property is the wrong type.</exception>
    /// <exception cref="FieldAccessException">Thrown if the version field is protected, private, or static.</exception>
    internal static string GetEntityVersion<T>(T entity) where T : notnull
    {
        throw new NotImplementedException();
    }
}
