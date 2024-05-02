// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Contract;

/// <summary>
/// A singleton cache for all the entity types that have been validated for use with the datasync service.
/// </summary>
internal class EntityTypeCache
{
    private static readonly Lazy<EntityTypeCache> _instance = new(() => new EntityTypeCache());

    /// <summary>
    /// Attempts to retrieve the system properties for the given entity type.
    /// </summary>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="properties">On completion, the <see cref="SystemProperties"/> for the type.</param>
    /// <returns><c>true</c> if the system properties were retrieved; <c>false</c> if they were created.</returns>
    internal static bool TryGetSystemProperties(Type entityType, out SystemProperties properties)
        => _instance.Value.InternalTryGetSystemProperties(entityType, out properties);

    #region Singleton Implementation
    private readonly ConcurrentDictionary<Type, SystemProperties> cache = new();

    /// <summary>
    /// Internal version of the <see cref="TryGetSystemProperties(Type, out SystemProperties)"/> method, which
    /// attempts to retrieve the system properties for the given entity type and creates them if they do not exist.
    /// </summary>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="properties">On completion, the <see cref="SystemProperties"/> for the type.</param>
    /// <returns><c>true</c> if the system properties were retrieved; <c>false</c> if they were created.</returns>
    private bool InternalTryGetSystemProperties(Type entityType, out SystemProperties properties)
    {
        if (this.cache.TryGetValue(entityType, out properties))
        {
            return true;
        }

        properties = new SystemProperties(entityType);
        this.cache.TryAdd(entityType, properties);
        return false;
    }
    #endregion
}
