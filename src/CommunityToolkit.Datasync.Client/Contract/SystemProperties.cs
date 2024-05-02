// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Contract;

/// <summary>
/// Provides services around the system properties that are used by the datasync service.
/// </summary>
internal class SystemProperties
{
    /// <summary>
    /// Creates a new <see cref="SystemProperties"/> instance for the given entity type.
    /// </summary>
    /// <param name="entityType">The entity type being processed.</param>
    internal SystemProperties(Type entityType)
    {
        EntityTypeName = entityType.Name;

        IdPropertyInfo = entityType.GetProperty(IdPropertyName);
        HasIdProperty = IdPropertyInfo?.PropertyType == typeof(string);

        IncrementalSyncPropertyInfo = entityType.GetProperty(IncrementalSyncPropertyName);
        HasIncrementalSyncProperty = IncrementalSyncPropertyInfo?.PropertyType == typeof(DateTimeOffset) || IncrementalSyncPropertyInfo?.PropertyType == typeof(DateTimeOffset?);

        OptimisticConcurrencyPropertyInfo = entityType.GetProperty(OptimisticConcurrencyPropertyName);
        HasOptimisticConcurrencyProperty = OptimisticConcurrencyPropertyInfo?.PropertyType == typeof(string);
    }

    /// <summary>
    /// The name of the entity type.
    /// </summary>
    internal string EntityTypeName { get; }

    #region Id Property
    /// <summary>
    /// The name of the Identity property that an entity must have.
    /// </summary>
    internal const string IdPropertyName = "Id";

    /// <summary>
    /// If <c>true</c>, then the Id property is present and valid on this entity type.
    /// </summary>
    internal bool HasIdProperty { get; }

    /// <summary>
    /// The <see cref="PropertyInfo"/> object for the Id property.
    /// </summary>
    internal PropertyInfo IdPropertyInfo { get; }

    /// <summary>
    /// Retrieves the value of the Id property for the given entity.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The value of the Id property.</returns>
    /// <exception cref="InvalidEntityException">If there is not a valid Id Property on the object</exception>
    internal string GetIdProperty(object entity)
    {
        if (!HasIdProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{IdPropertyName}' property", EntityTypeName);
        }

        return IdPropertyInfo.GetValue(entity) as string;
    }
    #endregion

    #region Incremental Sync Property
    /// <summary>
    /// The name of the Incremental Sync property that an offline-enabled entity must have.
    /// </summary>
    internal const string IncrementalSyncPropertyName = "UpdatedAt";

    /// <summary>
    /// If <c>true</c>, then the Incremental Sync property is present and valid on this entity type.
    /// </summary>
    internal bool HasIncrementalSyncProperty { get; }

    /// <summary>
    /// The <see cref="PropertyInfo"/> object for the Incremental Sync property.
    /// </summary>
    internal PropertyInfo IncrementalSyncPropertyInfo { get; }

    /// <summary>
    /// Retrieves the value of the Incremental Sync property for the given entity.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The value of the Incremental Sync property.</returns>
    /// <exception cref="InvalidEntityException">If there is not a valid Incremental Sync Property on the object</exception>
    internal DateTimeOffset? GetIncrementalSyncProperty(object entity)
    {
        if (!HasIncrementalSyncProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{IncrementalSyncPropertyName}' property", EntityTypeName);
        }

        return IncrementalSyncPropertyInfo.GetValue(entity) as DateTimeOffset?;
    }
    #endregion

    #region Optimistic Concurrency Property
    /// <summary>
    /// The name of the Optimistic Concurrency property that an offline-enabled entity must have.
    /// </summary>
    internal const string OptimisticConcurrencyPropertyName = "Version";

    /// <summary>
    /// If <c>true</c>, then the Optimistic Concurrency property is present and valid on this entity type.
    /// </summary>
    internal bool HasOptimisticConcurrencyProperty { get; }

    /// <summary>
    /// The <see cref="PropertyInfo"/> object for the Optimistic Concurrency property.
    /// </summary>
    internal PropertyInfo OptimisticConcurrencyPropertyInfo { get; }

    /// <summary>
    /// Retrieves the value of the Optimistic Concurrency property for the given entity.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The value of the Optimistic Concurrency property.</returns>
    /// <exception cref="InvalidEntityException">If there is not a valid Optimistic Concurrency Property on the object</exception>
    internal string GetOptimisticConcurrencyProperty(object entity)
    {
        if (!HasOptimisticConcurrencyProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{OptimisticConcurrencyPropertyName}' property", EntityTypeName);
        }

        return OptimisticConcurrencyPropertyInfo.GetValue(entity) as string;
    }
    #endregion
}
