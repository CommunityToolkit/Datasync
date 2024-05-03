// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Server.Abstractions.Guards;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Contract;

/// <summary>
/// The <see cref="EntityContractService"/> provides services for the serialization and deserialization of
/// entities for use with the datasync service.
/// </summary>
/// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
internal class EntityContractService(JsonSerializerOptions serializerOptions)
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of entities.
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; } = serializerOptions;

    /// <summary>
    /// Retrieves the Id property value from an entity.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entity">The value of the entity.</param>
    /// <returns>The entity Id value.</returns>
    /// <exception cref="InvalidEntityException">Thrown if the entity does not have an Id property.</exception>
    internal static string GetId(Type entityType, object entity)
    {
        _ = EntityTypeCache.TryGetSystemProperties(entityType, out SystemProperties properties);

        if (!properties.HasIdProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.IdPropertyName}' property", entityType.Name);
        }

        return properties.GetIdProperty(entity);
    }

    /// <summary>
    /// Retrieves the OptimisticConcurrency token property value from an entity.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entity">The value of the entity.</param>
    /// <returns>The entity Optimistic Concurrency Token value.</returns>
    /// <exception cref="InvalidEntityException">Thrown if the entity does not have an Optimistic Concurrency Token property.</exception>
    internal static string GetOptimisticConcurrencyToken(Type entityType, object entity)
    {
        _ = EntityTypeCache.TryGetSystemProperties(entityType, out SystemProperties properties);

        if (!properties.HasOptimisticConcurrencyProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.OptimisticConcurrencyPropertyName}' property", entityType.Name);
        }

        return properties.GetOptimisticConcurrencyProperty(entity);
    }

    /// <summary>
    /// Serializes an entity into a JSON string.
    /// </summary>
    /// <param name="entity">The entity to be serialized.</param>
    /// <returns>The serialized entity.</returns>
    /// <exception cref="JsonException">Thrown if the entity cannot be serialized.</exception>
    internal string SerializeEntity(object entity)
        => JsonSerializer.Serialize(entity, JsonSerializerOptions);

    /// <summary>
    /// Validates that the entity provided is suitable for submitting to a datasync service.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entity">The value of the entity.</param>
    /// <param name="allowNullIdentity">If <c>true</c>, the Id property is allowed to be null.</param>
    internal static void ValidateEntity(Type entityType, object entity, bool allowNullIdentity)
    {
        _ = EntityTypeCache.TryGetSystemProperties(entityType, out SystemProperties properties);

        if (!properties.HasIdProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.IdPropertyName}' property", entityType.Name);
        }

        string idProperty = properties.GetIdProperty(entity);
        if (idProperty is null)
        {
            if (allowNullIdentity)
            {
                return;
            }
            else
            {
                throw new InvalidEntityException($"Entity has a null '{SystemProperties.IdPropertyName}' property", entityType.Name);
            }
        }

        if (!RegexpConstants.EntityIdentity.IsMatch(idProperty))
        {
            throw new InvalidEntityException($"Entity has an invalid '{SystemProperties.IdPropertyName}' property", entityType.Name);
        }
    }

    /// <summary>
    /// Validates that the entity type is suitable for use with the datasync service.
    /// </summary>
    /// <param name="entityType">The type of the entity being checked.</param>
    /// <param name="offlineEnabled">If set to <c>true</c>, ensures that the incremental sync and optimistic concurrency properties are available.</param>
    /// <exception cref="InvalidEntityException">Thrown if the entity type is invalid for datasync services.</exception>
    internal static void ValidateEntityType(Type entityType, bool offlineEnabled)
    {
        _ = EntityTypeCache.TryGetSystemProperties(entityType, out SystemProperties properties);

        if (!properties.HasIdProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.IdPropertyName}' property", entityType.Name);
        }

        // If you have an Incremental Sync property, it must be the right type; if offline is enabled, it must exist.
        if ((offlineEnabled || properties.IncrementalSyncPropertyInfo is not null) && !properties.HasIncrementalSyncProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.IncrementalSyncPropertyName}' property", entityType.Name);
        }

        // If you have an Optimistic Concurrency property, it must be the right type; if offline is enabled, it must exist.
        if ((offlineEnabled || properties.OptimisticConcurrencyPropertyInfo is not null) && !properties.HasOptimisticConcurrencyProperty)
        {
            throw new InvalidEntityException($"Entity type does not have a valid '{SystemProperties.OptimisticConcurrencyPropertyName}' property", entityType.Name);
        }
    }
}

// If we mark the methods as static, then the linter complains that the class is not static, so we can't win and disable the warning.
#pragma warning disable CA1822 // Mark members as static

/// <summary>
/// The <see cref="EntityContractService{Task}"/> provides typed services for the serialization and deserialization of
/// entities for use with the datasync service.
/// </summary>
/// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
internal class EntityContractService<T>(JsonSerializerOptions serializerOptions) : EntityContractService(serializerOptions)
{
    /// <summary>
    /// Deserializes an entity from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="JsonException">Thrown if the entity cannot be deserialized.</exception>
    internal T DeserializeEntity(string json)
        => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);

    /// <summary>
    /// Deserializes a page of entities from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="JsonException">Thrown if the entity cannot be deserialized.</exception>
    internal Page<T> DeserializePageOfEntities(string json)
        => JsonSerializer.Deserialize<Page<T>>(json, JsonSerializerOptions);

    /// <summary>
    /// Retrieves the Id property value from an entity.
    /// </summary>
    /// <param name="entity">The value of the entity.</param>
    /// <returns>The entity Id value.</returns>
    /// <exception cref="InvalidEntityException">Thrown if the entity does not have an Id property.</exception>
    internal string GetId(T entity) => GetId(typeof(T), entity);

    /// <summary>
    /// Retrieves the Id property value from an entity.
    /// </summary>
    /// <param name="entity">The value of the entity.</param>
    /// <returns>The entity Id value.</returns>
    /// <exception cref="InvalidEntityException">Thrown if the entity does not have an Id property.</exception>
    internal string GetOptimisticConcurrencyToken(T entity) => GetOptimisticConcurrencyToken(typeof(T), entity);

    /// <summary>
    /// Validates that the entity provided is suitable for submitting to a datasync service.
    /// </summary>
    /// <param name="entity">The value of the entity.</param>
    /// <param name="allowNullIdentity">If <c>true</c>, the Id property is allowed to be null.</param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ValidateEntity(T entity, bool allowNullIdentity = false) => ValidateEntity(typeof(T), entity, allowNullIdentity);

    /// <summary>
    /// Validates that the entity type is suitable for use with the datasync service.
    /// </summary>
    /// <param name="offlineEnabled">If set to <c>true</c>, ensures that the incremental sync and optimistic concurrency properties are available.</param>
    /// <exception cref="InvalidEntityException">Thrown if the entity type is invalid for datasync services.</exception>
    internal void ValidateEntityType(bool offlineEnabled = false) => ValidateEntityType(typeof(T), offlineEnabled);
}
