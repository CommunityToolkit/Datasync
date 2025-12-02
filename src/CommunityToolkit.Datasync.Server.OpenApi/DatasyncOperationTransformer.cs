// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CommunityToolkit.Datasync.Server.OpenApi;

/// <summary>
/// The document transformer for the Datasync services.
/// </summary>
public class DatasyncOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>
    /// The list of processed entity names (which are those we have added to the schema already).
    /// </summary>
    private readonly List<string> processedEntityNames = [];

    /// <inheritdoc />
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Determine if this operation is in scope.
        if (!IsDatasyncController(context))
        {
            return;
        }

        // Ensure the operation is set up for modification.
        operation.Parameters ??= [];

        string? actionName = context.Description.ActionDescriptor.RouteValues["action"];
        if (actionName?.StartsWith("Create", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await TransformCreateAsync(operation, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (actionName?.StartsWith("Delete", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await TransformDeleteAsync(operation, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (actionName?.StartsWith("Query", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await TransformQueryAsync(operation, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (actionName?.StartsWith("Read", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await TransformReadAsync(operation, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (actionName?.StartsWith("Replace", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await TransformReplaceAsync(operation, context, cancellationToken).ConfigureAwait(false);
            return;
        }

        return;
    }

    /// <summary>
    /// Determines if a controller presented is a datasync controller.
    /// </summary>
    /// <param name="context">The transformer context.</param>
    /// <returns>true if the controller is a datasync controller; false otherwise.</returns>
    internal static bool IsDatasyncController(OpenApiOperationTransformerContext context)
        => context.Description.ActionDescriptor.FilterDescriptors.Any(fd => fd.Filter is DatasyncControllerAttribute);

    /// <summary>
    /// Adds a schema for the given type if it is not already processed.
    /// </summary>
    /// <param name="context">The transformer context holding the OpenApi document.</param>
    /// <param name="entityType">The entity type to add.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    internal async Task AddSchemaIfNotProcessedAsync(OpenApiOperationTransformerContext context, Type entityType, CancellationToken cancellationToken)
    {
        if (this.processedEntityNames.Contains(entityType.Name))
        {
            return;
        }

        OpenApiSchema schema = await context.GetOrCreateSchemaAsync(entityType, cancellationToken: cancellationToken).ConfigureAwait(false);
        schema.SetSystemPropertiesReadonly();
        _ = context.Document!.AddComponent(entityType.Name, schema);

        Type pagedEntityType = typeof(PagedResult<>).MakeGenericType(entityType);
        OpenApiSchema pagedSchema = await context.GetOrCreateSchemaAsync(pagedEntityType, cancellationToken: cancellationToken).ConfigureAwait(false);
        pagedSchema.SetSchemaPropertyReadonly("items");
        pagedSchema.SetSchemaItemsReference("items", new OpenApiSchemaReference(entityType.Name, context.Document));
        pagedSchema.SetSchemaPropertyReadonly("count");
        pagedSchema.SetSchemaPropertyReadonly("nextLink");
        pagedSchema.AdditionalPropertiesAllowed = false;
        _ = context.Document.AddComponent($"{entityType.Name}Page", pagedSchema);

        this.processedEntityNames.Add(entityType.Name);
    }

    /// <summary>
    /// Retrieves the entity type for the controller.
    /// </summary>
    /// <param name="context">The transformer context.</param>
    /// <returns>The type of the entity being served.</returns>
    internal static Type GetEntityType(OpenApiOperationTransformerContext context)
    {
        Type? baseType = (context.Description.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.AsType();
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(TableController<>))
            {
                Type? entityType = baseType.GetGenericArguments().FirstOrDefault();
                if (entityType is not null && typeof(ITableData).IsAssignableFrom(entityType))
                {
                    return entityType;
                }

                throw new InvalidOperationException("Expecting the entity type to implement ITableData.");
            }

            baseType = baseType.BaseType;
        }

        throw new InvalidOperationException("Expecting the controller to be derived from TableController<T>.");
    }

    /// <summary>
    /// Gets a reference to the schema for the given entity type, adding it to the document if needed.
    /// </summary>
    /// <param name="context">the transformer context holding the document.</param>
    /// <param name="entityType">The entity type being referenced.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the schema reference when resolved.</returns>
    internal async Task<OpenApiSchemaReference> GetSchemaReferenceAsync(OpenApiOperationTransformerContext context, Type entityType, CancellationToken cancellationToken)
    {
        context.ThrowIfDocumentIsUnavailable();
        await AddSchemaIfNotProcessedAsync(context, entityType, cancellationToken).ConfigureAwait(false);
        return new OpenApiSchemaReference(entityType.Name, context.Document);
    }

    /// <summary>
    /// Transforms a create operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformCreateAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);
        OpenApiSchemaReference schemaRef = await GetSchemaReferenceAsync(context, entityType, cancellationToken).ConfigureAwait(false);

        operation.AddRequestBody(schemaRef);

        operation.Responses ??= [];
        operation.Responses.AddEntityResponse(StatusCodes.Status201Created, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed, schemaRef, includeConditionalHeaders: true);
    }

    /// <summary>
    /// Transforms a delete operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformDeleteAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);
        OpenApiSchemaReference schemaRef = await GetSchemaReferenceAsync(context, entityType, cancellationToken).ConfigureAwait(false);

        operation.Parameters ??= [];
        operation.Responses ??= [];

        operation.Parameters.AddIfMatchHeader();
        operation.Parameters.AddIfUnmodifiedSinceHeader();

        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddStatusCode(StatusCodes.Status404NotFound);
        operation.Responses.AddStatusCode(StatusCodes.Status410Gone);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed, schemaRef, includeConditionalHeaders: true);
    }

    /// <summary>
    /// Transforms a query operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformQueryAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);
        await AddSchemaIfNotProcessedAsync(context, entityType, cancellationToken).ConfigureAwait(false);
        OpenApiSchemaReference pagedSchemaRef = new($"{entityType.Name}Page", context.Document!);

        operation.Parameters ??= [];
        operation.Responses ??= [];

        operation.Parameters.AddBooleanQueryParameter("$count", "Whether to include the total count of items matching the query in the result");
        operation.Parameters.AddStringQueryParameter("$filter", "The filter to apply to the query");
        operation.Parameters.AddStringQueryParameter("$orderby", "The comma-separated list of ordering instructions to apply to the query");
        operation.Parameters.AddStringQueryParameter("$select", "The comma-separated list of fields to return in the results");
        operation.Parameters.AddIntQueryParameter("$skip", "The number of items to skip", 0);
        operation.Parameters.AddIntQueryParameter("$top", "The number of items to return", 1);
        operation.Parameters.AddIncludeDeletedQuery();

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK, pagedSchemaRef, includeConditionalHeaders: false);
        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Transforms a read operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformReadAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);
        OpenApiSchemaReference schemaRef = await GetSchemaReferenceAsync(context, entityType, cancellationToken).ConfigureAwait(false);

        operation.Parameters ??= [];
        operation.Responses ??= [];

        operation.Parameters.AddIncludeDeletedQuery();
        operation.Parameters.AddIfNoneMatchHeader();
        operation.Parameters.AddIfModifiedSinceHeader();

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddStatusCode(StatusCodes.Status304NotModified);
        operation.Responses.AddStatusCode(StatusCodes.Status404NotFound);
        operation.Responses.AddStatusCode(StatusCodes.Status410Gone);
    }

    /// <summary>
    /// Transforms a replace operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformReplaceAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);
        OpenApiSchemaReference schemaRef = await GetSchemaReferenceAsync(context, entityType, cancellationToken).ConfigureAwait(false);

        operation.Parameters ??= [];
        operation.Responses ??= [];

        operation.AddRequestBody(schemaRef);
        operation.Parameters.AddIncludeDeletedQuery();
        operation.Parameters.AddIfMatchHeader();
        operation.Parameters.AddIfUnmodifiedSinceHeader();

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddStatusCode(StatusCodes.Status404NotFound);
        operation.Responses.AddStatusCode(StatusCodes.Status410Gone);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict, schemaRef, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed, schemaRef, includeConditionalHeaders: true);
    }
}
