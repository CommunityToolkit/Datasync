// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Remote;

/// <summary>
/// A helper class that stores the reflection values for an entity.
/// </summary>
internal static class EntityTypeCache
{
    internal const string IdPropertyName = "Id";
    internal const string VersionPropertyName = "Version";

    internal static readonly ConcurrentDictionary<Type, EntityTypeAccessor> _cache = new();

    /// <summary>
    /// Returns the entity ID, or throws an exception if the entity ID is not available as a string.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity ID.</returns>
    internal static string? GetEntityId<T>(T entity) where T : notnull
    {
        EntityTypeAccessor accessor = _cache.GetOrAdd(typeof(T), (Type t) => new EntityTypeAccessor(t));
        return accessor.GetEntityId(entity);
    }

    /// <summary>
    /// Returns the entity version as a string.  If the version is available but is
    /// the wrong type (not a string or byte[]), then an exception is thrown.  If the
    /// version is available but null/empty, then an empty string is returned.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity version, or an empty string.</returns>
    internal static string GetEntityVersion<T>(T entity) where T : notnull
    {
        EntityTypeAccessor accessor = _cache.GetOrAdd(typeof(T), (Type t) => new EntityTypeAccessor(t));
        return accessor.GetEntityVersion(entity);
    }

    /// <summary>
    /// An internal class that uses Reflection to return the Id and Version for
    /// the entity provided.
    /// </summary>
    internal class EntityTypeAccessor
    {
        private readonly PropertyInfo idPropertyInfo;
        private readonly PropertyInfo? versionPropertyInfo;

        internal EntityTypeAccessor(Type entityType)
        {
            this.idPropertyInfo = entityType.GetProperty(IdPropertyName, BindingFlags.Instance | BindingFlags.Public)
                ?? throw new MissingMemberException(entityType.FullName, IdPropertyName);
            if (this.idPropertyInfo.PropertyType != typeof(string))
            {
                throw new InvalidCastException($"Property {IdPropertyName} must be a string.");
            }

            this.versionPropertyInfo = entityType.GetProperty(VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            Type? versionType = this.versionPropertyInfo?.PropertyType;
            if (versionType != null && versionType != typeof(string) && versionType != typeof(byte[]))
            {
                throw new InvalidCastException($"Property {VersionPropertyName} must be a string or byte array");
            }
        }

        /// <summary>
        /// Returns the entity ID, or throws an exception if the entity ID is not available as a string.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The entity ID.</returns>
        internal string? GetEntityId(object entity)
            => this.idPropertyInfo.GetValue(entity) as string;

        /// <summary>
        /// Returns the entity version as a string. If the version is available
        /// but null/empty, then an empty string is returned.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The entity version, or an empty string.</returns>
        internal string GetEntityVersion(object entity)
        {
            if (this.versionPropertyInfo?.PropertyType == typeof(string))
            {
                return (this.versionPropertyInfo.GetValue(entity) as string) ?? string.Empty;
            }

            if (this.versionPropertyInfo?.PropertyType == typeof(byte[]))
            {
                byte[] version = (this.versionPropertyInfo.GetValue(entity) as byte[]) ?? [];
                return version.Length > 0 ? Convert.ToBase64String(version) : string.Empty;
            }

            return string.Empty;
        }
    }
}
