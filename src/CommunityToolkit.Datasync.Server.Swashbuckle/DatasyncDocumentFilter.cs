// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Filters;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommunityToolkit.Datasync.Server.Swashbuckle;

/// <summary>
/// An <see cref="IDocumentFilter"/> that adds the relevant schema and parameter definitions
/// to generate an OpenAPI v3.0.3 definition for Datasync <see cref="TableController{TEntity}"/>
/// controllers.
/// </summary>
/// <remarks>
/// Creates a new <see cref="DatasyncDocumentFilter"/>.
/// </remarks>
/// <param name="assemblyToQuery">The assembly to query for TableController instances, if any.  If none is provided, the calling assembly is queried.</param>
public class DatasyncDocumentFilter(Assembly? assemblyToQuery = null) : IDocumentFilter
{
    // The list of operation types.
    private enum OpType
    {
        Create,
        Delete,
        GetById,
        List,
        Replace
    }

    // The names of the QueryAsync() and CreateAsync() methods in the TableController<T>.
    private const string queryMethod = nameof(TableController<>.QueryAsync);
    private const string createMethod = nameof(TableController<>.CreateAsync);

    // The list of entity names that have already had their schema adjusted.
    private readonly List<string> processedEntityNames = [];

    /// <summary>
    /// Applies the necessary changes to the <see cref="OpenApiDocument"/>.
    /// </summary>
    /// <param name="document">The <see cref="OpenApiDocument"/> to edit.</param>
    /// <param name="context">The filter context.</param>
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (Type controller in GetAllTableControllers(assemblyToQuery))
        {
            if (TryGetTableEntityType(controller, out Type? entityType))
            {
                string? routePath = GetRoutePathFromContext(context, controller);
                if (routePath is not null)
                {
                    ProcessController(entityType!, routePath, document, context);
                }
            }
        }
    }

    /// <summary>
    /// Adds the schema reference for the entity type if it hasn't already been processed.
    /// </summary>
    /// <param name="context">The document filter context.</param>
    /// <param name="document">The OpenApi document.</param>
    /// <param name="entityType">The entity type</param>
    internal void AddSchemaReferenceForEntity(DocumentFilterContext context, OpenApiDocument document, Type entityType)
    {
        if (this.processedEntityNames.Contains(entityType.Name))
        {
            return;
        }

        // Generate a schema for the entity if it doesn't exist.
        if (context.SchemaRepository.Schemas.GetValueOrDefault(entityType.Name) is null)
        {
            _ = context.SchemaGenerator.GenerateSchema(entityType, context.SchemaRepository);
        }

        context.SchemaRepository.Schemas[entityType.Name].MakeSystemPropertiesReadonly();
        _ = document.AddComponent(entityType.Name, context.SchemaRepository.Schemas[entityType.Name]);
        this.processedEntityNames.Add(entityType.Name);
    }

    /// <summary>
    /// Applies the necessary changes to the <see cref="OpenApiDocument"/> for a single controller.
    /// </summary>
    /// <param name="entityType">The type of the entity being processed by the controller.</param>
    /// <param name="routePath">The path used to access the controller in a HTTP request.</param>
    /// <param name="document">The <see cref="OpenApiDocument"/> to edit.</param>
    /// <param name="context">The filter context.</param>
    internal void ProcessController(Type entityType, string routePath, OpenApiDocument document, DocumentFilterContext context)
    {
        // Get the base paths managed by this controller.
        string allEntitiesPath = $"/{routePath}";
        string singleEntityPath = $"/{routePath}/{{id}}";

        // Get the various operations
        Dictionary<OpType, OpenApiOperation> operations = [];
        AddOperationIfPresent(operations, OpType.Create, document, allEntitiesPath, HttpMethod.Post);
        AddOperationIfPresent(operations, OpType.Delete, document, singleEntityPath, HttpMethod.Delete);
        AddOperationIfPresent(operations, OpType.GetById, document, singleEntityPath, HttpMethod.Get);
        AddOperationIfPresent(operations, OpType.List, document, allEntitiesPath, HttpMethod.Get);
        AddOperationIfPresent(operations, OpType.Replace, document, singleEntityPath, HttpMethod.Put);

        // Add the schema for the entity type if it doesn't already exist.
        AddSchemaReferenceForEntity(context, document, entityType);
        IOpenApiSchema schema = new OpenApiSchemaReference(entityType.Name, document);

        // Create the schema for a list of entities.
        Type listEntityType = typeof(Page<>).MakeGenericType(entityType);
        IOpenApiSchema listSchemaRef = context.SchemaRepository.Schemas.GetValueOrDefault(listEntityType.Name)
            ?? context.SchemaGenerator.GenerateSchema(listEntityType, context.SchemaRepository);

        foreach (KeyValuePair<OpType, OpenApiOperation> operation in operations)
        {
            operation.Value.Responses ??= [];

            // Each operation also has certain modifications.
            switch (operation.Key)
            {
                case OpType.Create:
                    // Request Edits
                    operation.Value.AddConditionalHeader(true);
                    operation.Value.AddRequestWithContent(schema);

                    // Response Edits
                    operation.Value.AddResponseWithContent("201", "Created", schema);
                    operation.Value.Responses["400"] = new OpenApiResponse { Description = "Bad Request" };
                    operation.Value.AddConflictResponse(schema);
                    break;

                case OpType.Delete:
                    // Request Edits
                    operation.Value.AddConditionalHeader();

                    // Response Edits
                    operation.Value.Responses["204"] = new OpenApiResponse { Description = "No Content" };
                    operation.Value.Responses["404"] = new OpenApiResponse { Description = "Not Found" };
                    operation.Value.Responses["410"] = new OpenApiResponse { Description = "Gone" };
                    operation.Value.AddConflictResponse(schema);
                    break;

                case OpType.GetById:
                    // Request Edits
                    operation.Value.AddConditionalHeader(true);

                    // Response Edits
                    operation.Value.AddResponseWithContent("200", "OK", schema);
                    operation.Value.Responses["304"] = new OpenApiResponse { Description = "Not Modified" };
                    operation.Value.Responses["404"] = new OpenApiResponse { Description = "Not Found" };
                    break;

                case OpType.List:
                    // Request Edits
                    operation.Value.AddODataQueryParameters();

                    // Response Edits
                    operation.Value.AddResponseWithContent("200", "OK", listSchemaRef);
                    operation.Value.Responses["400"] = new OpenApiResponse { Description = "Bad Request" };
                    break;

                case OpType.Replace:
                    // Request Edits
                    operation.Value.AddConditionalHeader();
                    operation.Value.AddRequestWithContent(schema);

                    // Response Edits
                    operation.Value.AddResponseWithContent("200", "OK", schema);
                    operation.Value.Responses["400"] = new OpenApiResponse { Description = "Bad Request" };
                    operation.Value.Responses["404"] = new OpenApiResponse { Description = "Not Found" };
                    operation.Value.Responses["410"] = new OpenApiResponse { Description = "Gone" };
                    operation.Value.AddConflictResponse(schema);
                    break;
            }
        }
    }

    /// <summary>
    /// Adds the relevant operation to the dictionary if it is present.
    /// </summary>
    /// <remarks>
    /// A developer can override the <see cref="TableController{TEntity}"/> actions to disable
    /// any operation (e.g. to create a read-only controller).  So we need to check to ensure
    /// that every element is there.
    /// </remarks>
    /// <param name="operations">The operations dictionary to modify.</param>
    /// <param name="opType">The internal operation type (Create, Delete, Query, etc.)</param>
    /// <param name="document">The <see cref="OpenApiDocument"/> being processed.</param>
    /// <param name="path">The expected path for the operation type.</param>
    /// <param name="operationType">The operation type being processed.</param>
    private static void AddOperationIfPresent(Dictionary<OpType, OpenApiOperation> operations, OpType opType, OpenApiDocument document, string path, HttpMethod operationType)
    {
        if (document.Paths.TryGetValue(path, out IOpenApiPathItem? pathValue)
            && pathValue.Operations is not null
            && pathValue.Operations.TryGetValue(operationType, out OpenApiOperation? operation))
        {
            operations[opType] = operation!;
        }
    }

    /// <summary>
    /// Retrieves the entity type for a <see cref="TableController{TEntity}"/>.
    /// </summary>
    /// <param name="controllerType">The type for the controller.</param>
    /// <param name="entityType">The type for the entity, or <c>null</c> if the controller doesn't have an entity type.</param>
    /// <returns><c>true</c> if the table controller has an entity type.</returns>
    internal static bool TryGetTableEntityType(Type controllerType, out Type? entityType)
    {
        entityType = controllerType.BaseType?.GetGenericArguments().FirstOrDefault();
        return entityType != null;
    }

    /// <summary>
    /// Retrieves the route path for a controller.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <param name="controller">The controller to check.</param>
    /// <returns>The route path for the controller.</returns>
    internal static string? GetRoutePathFromContext(DocumentFilterContext context, Type controller)
        => context.ApiDescriptions.FirstOrDefault(m => IsApiDescriptionForController(m, controller))?.RelativePath;

    /// <summary>
    /// Determines if the controller type is represented by the API Description.
    /// </summary>
    /// <param name="description">The <see cref="ApiDescription"/> being handled.</param>
    /// <param name="controllerType">The type of the controller being used.</param>
    /// <returns><c>true</c> if the Api description represents the controller.</returns>
    internal static bool IsApiDescriptionForController(ApiDescription description, Type controllerType)
        => description.TryGetMethodInfo(out MethodInfo methodInfo) 
        && methodInfo.ReflectedType == controllerType
        && (methodInfo.Name.Equals(queryMethod) || methodInfo.Name.Equals(createMethod));

    /// <summary>
    /// Returns a list of all table controllers in the provided assembly.
    /// </summary>
    /// <param name="assembly">The assembly to query.  Be default, the calling assembly is queried.</param>
    /// <returns>The list of table controllers in the assembly.</returns>
    internal static List<Type> GetAllTableControllers(Assembly? assembly)
        => [.. (assembly ?? Assembly.GetCallingAssembly()).GetTypes().Where(IsTableController)];

    /// <summary>
    /// Determines if the controller type provided is a datasync table controller.
    /// </summary>
    /// <param name="type">The type of the table controller.</param>
    /// <returns><c>true</c> if the type is a datasync table controller.</returns>
    internal static bool IsTableController(Type type)
    {
        if (!type.IsAbstract && type.BaseType is not null && type.BaseType.IsGenericType == true)
        {
            if (type.GetCustomAttribute<DatasyncControllerAttribute>() is not null)
            {
                return true;
            }
        }

        return false;
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
