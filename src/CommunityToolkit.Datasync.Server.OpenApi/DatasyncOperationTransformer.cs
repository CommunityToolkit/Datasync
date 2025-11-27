// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;

namespace CommunityToolkit.Datasync.Server.OpenApi;

/// <summary>
/// The document transformer for the Datasync services.
/// </summary>
public class DatasyncOperationTransformer : IOpenApiOperationTransformer
{
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

    internal async Task<OpenApiSchemaReference> GetSchemaReferenceAsync(OpenApiOperationTransformerContext context, Type entityType)
    {
        if(context.Document is null)
        {
            throw new InvalidOperationException("The OpenAPI document is not available in the transformer context.");
        }

        if (!this.processedEntityNames.Contains(entityType.Name))
        {
            OpenApiSchema schema = await context.GetOrCreateSchemaAsync(entityType);

            // This is a Datasync schema, so update the schema for the datasync attributes.
            schema.MakeSystemPropertiesReadonly();

            _ = context.Document.AddComponent(entityType.Name, schema);

            Type pagedEntityType = typeof(Page<>).MakeGenericType(entityType);
            string pagedEntitySchemaName = $"{entityType.Name}Page";

            OpenApiSchema pagedEntitySchema = await context.GetOrCreateSchemaAsync(pagedEntityType);

            OpenApiSchema itemsProp = (OpenApiSchema)pagedEntitySchema.Properties!["items"];
            OpenApiSchema countProp = (OpenApiSchema)pagedEntitySchema.Properties!["count"];
            OpenApiSchema nextLinkProp = (OpenApiSchema)pagedEntitySchema.Properties!["nextLink"];
            itemsProp.ReadOnly = true;
            countProp.ReadOnly = true;
            //countProp.Type = JsonSchemaType.Integer;
            //countProp.Format = "int64";
            //countProp.Pattern = null;
            nextLinkProp.ReadOnly = true;
            //nextLinkProp.Type = JsonSchemaType.String;
            //nextLinkProp.Format = "uri";
            pagedEntitySchema.AdditionalPropertiesAllowed = false;
            //_ = pagedEntitySchema.Required?.Remove("items");

            itemsProp.Items = new OpenApiSchemaReference(entityType.Name, context.Document);

            _ = context.Document.AddComponent(pagedEntitySchemaName, pagedEntitySchema);

            this.processedEntityNames.Add(entityType.Name);
        }

        OpenApiSchemaReference schemaRef = new(entityType.Name, context.Document);

        return schemaRef;
    }
    private readonly List<string> processedEntityNames = [];

    /// <summary>
    /// Determines if a controller presented is a datasync controller.
    /// </summary>
    /// <param name="context">The transformer context.</param>
    /// <returns>true if the controller is a datasync controller; false otherwise.</returns>
    internal static bool IsDatasyncController(OpenApiOperationTransformerContext context)
        => context.Description.ActionDescriptor.FilterDescriptors.Any(fd => fd.Filter is DatasyncControllerAttribute);

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
    /// Transforms a create operation.
    /// </summary>
    /// <param name="operation">The operation to transform.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that resolves when the operation is complete.</returns>
    internal async Task TransformCreateAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        Type entityType = GetEntityType(context);

        OpenApiSchemaReference schemaReference = await GetSchemaReferenceAsync(context, entityType);

        operation.AddRequestBody(schemaReference);

        operation.Responses ??= [];

        operation.Responses.AddEntityResponse(StatusCodes.Status201Created,
            schemaReference, includeConditionalHeaders: true);
        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict,
            schemaReference, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed,
            schemaReference, includeConditionalHeaders: true);
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

        OpenApiSchemaReference schemaReference = await GetSchemaReferenceAsync(context, entityType);

        operation.Parameters ??= [];

        operation.Parameters.AddIfMatchHeader();
        operation.Parameters.AddIfUnmodifiedSinceHeader();

        operation.Responses ??= [];

        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddStatusCode(StatusCodes.Status404NotFound);
        operation.Responses.AddStatusCode(StatusCodes.Status410Gone);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict,
            schemaReference, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed,
            schemaReference, includeConditionalHeaders: true);
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

        OpenApiSchemaReference pagedEntitySchemaRef = new($"{entityType.Name}Page", context.Document);

        operation.Parameters ??= [];

        operation.Parameters.AddBooleanQueryParameter("$count", "Whether to include the total count of items matching the query in the result");
        operation.Parameters.AddStringQueryParameter("$filter", "The filter to apply to the query");
        operation.Parameters.AddStringQueryParameter("$orderby", "The comma-separated list of ordering instructions to apply to the query");
        operation.Parameters.AddStringQueryParameter("$select", "The comma-separated list of fields to return in the results");
        operation.Parameters.AddIntQueryParameter("$skip", "The number of items to skip", 0);
        operation.Parameters.AddIntQueryParameter("$top", "The number of items to return", 1);
        operation.Parameters.AddIncludeDeletedQuery();

        operation.Responses ??= [];

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK,
            pagedEntitySchemaRef, includeConditionalHeaders: false);
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

        OpenApiSchemaReference schemaReference = await GetSchemaReferenceAsync(context, entityType);

        operation.Parameters ??= [];

        operation.Parameters.AddIncludeDeletedQuery();
        operation.Parameters.AddIfNoneMatchHeader();
        operation.Parameters.AddIfModifiedSinceHeader();

        operation.Responses ??= [];

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK,
            schemaReference, includeConditionalHeaders: true);
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

        OpenApiSchemaReference schemaReference = await GetSchemaReferenceAsync(context, entityType);

        operation.Parameters ??= [];

        operation.AddRequestBody(schemaReference);
        operation.Parameters.AddIncludeDeletedQuery();
        operation.Parameters.AddIfMatchHeader();
        operation.Parameters.AddIfUnmodifiedSinceHeader();

        operation.Responses ??= [];

        operation.Responses.AddEntityResponse(StatusCodes.Status200OK,
            schemaReference, includeConditionalHeaders: true);
        operation.Responses.AddStatusCode(StatusCodes.Status400BadRequest);
        operation.Responses.AddStatusCode(StatusCodes.Status404NotFound);
        operation.Responses.AddStatusCode(StatusCodes.Status410Gone);
        operation.Responses.AddEntityResponse(StatusCodes.Status409Conflict,
            schemaReference, includeConditionalHeaders: true);
        operation.Responses.AddEntityResponse(StatusCodes.Status412PreconditionFailed,
            schemaReference, includeConditionalHeaders: true);
    }

    /// <summary>
    /// A type representing a single page of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    [ExcludeFromCodeCoverage(Justification = "Model class - coverage not needed")]
    internal class Page<T>
    {
        /// <summary>
        /// The list of entities in this page of the results.
        /// </summary>
        public IEnumerable<T> Items { get; } = [];

        /// <summary>
        /// The count of all the entities in the result set.
        /// </summary>
        public long? Count { get; }

        /// <summary>
        /// The URI to the next page of entities.
        /// </summary>
        public Uri? NextLink { get; }
    }
}
