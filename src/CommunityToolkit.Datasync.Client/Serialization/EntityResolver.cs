// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// A caching resolver for the entity metadata
/// </summary>
internal static class EntityResolver
{
    /// <summary>
    /// The internal cache of entity property information.
    /// </summary>
    internal static Dictionary<Type, EntityPropertyInfo> cache = [];

    /// <summary>
    /// Retrieves the EntityPropertyInfo for a specific type.
    /// </summary>
    /// <param name="type">The type to be processed.</param>
    /// <returns>The <see cref="EntityPropertyInfo"/> for a given type.</returns>
    internal static EntityPropertyInfo GetEntityPropertyInfo(Type type)
    {
        if (cache.TryGetValue(type, out EntityPropertyInfo? entityPropertyInfo))
        {
            return entityPropertyInfo;
        }

        EntityPropertyInfo createdInfo = new(type);
        cache.Add(type, createdInfo);
        return createdInfo;
    }

    /// <summary>
    /// Retrieves the <see cref="EntityMetadata"/> for the given entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The metadata for the entity.</returns>
    internal static EntityMetadata GetEntityMetadata<TEntity>(TEntity entity) where TEntity : class
        => GetEntityPropertyInfo(typeof(TEntity)).GetEntityMetadata(entity);

    /// <summary>
    /// The class for handling a single type.
    /// </summary>
    internal class EntityPropertyInfo
    {
        /// <summary>
        /// Creates a new <see cref="EntityPropertyInfo"/> based on the type.
        /// </summary>
        /// <remarks>
        /// An entity type must have a string Id and may have a DateTimeOffset UpdatedAt field (used for incremental sync)
        /// and a string or byte[] Version field (used for conflict resolution).
        /// </remarks>
        /// <param name="type">The type to parse.</param>
        /// <exception cref="DatasyncException">Thrown if the metadata properties are invalid.</exception>
        internal EntityPropertyInfo(Type type)
        {
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).ToArray();
            
            // Find the IdPropertyInfo
            IdPropertyInfo = props.SingleOrDefault(x => x.Name.Equals(nameof(EntityMetadata.Id), StringComparison.Ordinal))
                ?? throw new DatasyncException($"Entity type '{type.Name}' does not have an 'Id' property.");
            if (IdPropertyInfo.PropertyType != typeof(string))
            {
                throw new DatasyncException($"Entity type '{type.Name}' does not have a string 'Id' property.");
            }

            UpdatedAtPropertyInfo = props.SingleOrDefault(x => x.Name.Equals(nameof(EntityMetadata.UpdatedAt), StringComparison.Ordinal));
            if (UpdatedAtPropertyInfo != null && UpdatedAtPropertyInfo.PropertyType != typeof(DateTimeOffset))
            {
                throw new DatasyncException($"Property type '{type.Name}'.UpdatedAt must be a 'DateTimeOffset' type.");
            }

            VersionPropertyInfo = props.SingleOrDefault(x => x.Name.Equals(nameof(EntityMetadata.Version), StringComparison.Ordinal));
            if (VersionPropertyInfo != null && (VersionPropertyInfo.PropertyType != typeof(string) || VersionPropertyInfo.PropertyType != typeof(byte[])))
            {
                throw new DatasyncException($"Property type '{type.Name}'.Version must be either a string or byte array.");
            }
        }

        /// <summary>
        /// The PropertyInfo for the globally unique ID.
        /// </summary>
        internal PropertyInfo IdPropertyInfo { get; }

        /// <summary>
        /// The PropertyInfo for the timestamp this record was last updated.
        /// </summary>
        internal PropertyInfo? UpdatedAtPropertyInfo { get; }

        /// <summary>
        /// The PropertyInfo for the version property.
        /// </summary>
        internal PropertyInfo? VersionPropertyInfo { get; }

        /// <summary>
        /// Retrieves the datasync metadata for the given entity.
        /// </summary>
        /// <param name="entity">The entity to process.</param>
        /// <returns>The metadata for the entity.</returns>
        internal EntityMetadata GetEntityMetadata(object entity)
        {
            EntityMetadata metadata = new() { Id = (string?)IdPropertyInfo.GetValue(entity) };

            if (UpdatedAtPropertyInfo != null)
            {
                metadata.UpdatedAt = (DateTimeOffset?)UpdatedAtPropertyInfo.GetValue(entity);
            }

            if (VersionPropertyInfo != null)
            {
                if (VersionPropertyInfo.PropertyType == typeof(string))
                {
                    metadata.Version = (string?)VersionPropertyInfo.GetValue(entity);
                }

                if (VersionPropertyInfo.PropertyType == typeof(byte[]))
                {
                    byte[]? bVersion = (byte[]?)VersionPropertyInfo.GetValue(entity);
                    if (bVersion != null)
                    {
                        metadata.Version = Convert.ToBase64String(bVersion);
                    }
                }
            }

            return metadata;
        }
    }
}
