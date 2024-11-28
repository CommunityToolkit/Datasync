// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Filters;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Net;
using System.Reflection;

namespace CommunityToolkit.Datasync.Server.NSwag;

/// <summary>
/// Implements an <see cref="IOperationProcessor"/> for handling datasync table controllers.
/// </summary>
public class DatasyncOperationProcessor : IOperationProcessor
{
    /// <summary>Processes the specified method information.</summary>
    /// <param name="context">The processor context.</param>
    /// <returns>true if the operation should be added to the Swagger specification.</returns>
    public bool Process(OperationProcessorContext context)
    {
        if (IsTableController(context.ControllerType))
        {
            ProcessDatasyncOperation(context);
        }

        return true;
    }

    /// <summary>
    /// Determines if the controller type provided is a datasync table controller.
    /// </summary>
    /// <param name="type">The type of the table controller.</param>
    /// <returns><c>true</c> if the type is a datasync table controller.</returns>
    internal static bool IsTableController(Type type)
    {
        if (!type.IsAbstract && type.BaseType != null && type.BaseType.IsGenericType == true)
        {
            if (type.GetCustomAttribute<DatasyncControllerAttribute>() != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the entity type being handled by the controller type.
    /// </summary>
    /// <param name="controllerType">The <see cref="Type"/> of the controller.</param>
    /// <returns>The Type for the entity.</returns>
    /// <exception cref="ArgumentException">If the controller type is not a generic type.</exception>
    internal static Type GetTableEntityType(Type controllerType)
        => controllerType.BaseType?.GetGenericArguments().FirstOrDefault()
        ?? throw new ArgumentException("Unable to retrieve generic entity type");

    private static void ProcessDatasyncOperation(OperationProcessorContext context)
    {
        OpenApiOperation operation = context.OperationDescription.Operation;
        string method = context.OperationDescription.Method;
        string path = context.OperationDescription.Path;
        Type entityType = GetTableEntityType(context.ControllerType);
        JsonSchema entitySchemaRef = GetEntityReference(context, entityType);
        AddMissingSchemaProperties(entitySchemaRef.Reference);

        if (method.Equals("DELETE", StringComparison.InvariantCultureIgnoreCase))
        {
            operation.AddConditionalRequestSupport(entitySchemaRef);
            operation.SetResponse(HttpStatusCode.NoContent);
            operation.SetResponse(HttpStatusCode.NotFound);
            operation.SetResponse(HttpStatusCode.Gone);
        }

        if (method.Equals("GET", StringComparison.InvariantCultureIgnoreCase) && path.EndsWith("/{id}"))
        {
            operation.AddConditionalRequestSupport(entitySchemaRef, true);
            operation.SetResponse(HttpStatusCode.OK, entitySchemaRef);
            operation.SetResponse(HttpStatusCode.NotFound);
        }

        if (method.Equals("GET", StringComparison.InvariantCultureIgnoreCase) && !path.EndsWith("/{id}"))
        {
            operation.AddODataQueryParameters();
            operation.SetResponse(HttpStatusCode.OK, CreateListSchema(entitySchemaRef, entityType.Name), false);
            operation.SetResponse(HttpStatusCode.BadRequest);
        }

        if (method.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
        {
            operation.AddConditionalRequestSupport(entitySchemaRef, true);
            operation.TryAddConsumes("application/json");
            operation.Parameters.Add(new OpenApiParameter { Schema = entitySchemaRef, Kind = OpenApiParameterKind.Body });
            operation.SetResponse(HttpStatusCode.Created, entitySchemaRef);
            operation.SetResponse(HttpStatusCode.BadRequest);
        }

        if (method.Equals("PUT", StringComparison.InvariantCultureIgnoreCase))
        {
            operation.AddConditionalRequestSupport(entitySchemaRef);
            operation.TryAddConsumes("application/json");
            operation.Parameters.Add(new OpenApiParameter { Schema = entitySchemaRef, Kind = OpenApiParameterKind.Body });
            operation.SetResponse(HttpStatusCode.OK, entitySchemaRef);
            operation.SetResponse(HttpStatusCode.BadRequest);
            operation.SetResponse(HttpStatusCode.NotFound);
            operation.SetResponse(HttpStatusCode.Gone);
        }
    }

    internal static void AddMissingSchemaProperties(JsonSchema? schema)
    {
        if (schema is null)
        {
            return;
        }

        if (schema.Properties.ContainsKey("id") && schema.Properties.ContainsKey("updatedAt") && schema.Properties.ContainsKey("version"))
        {
            // Nothing to do - the correct properties are already in the schma.
            return;
        }

        _ = schema.Properties.TryAdd("id", new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Description = "The globally unique ID for the entity",
            IsRequired = true
        });
        _ = schema.Properties.TryAdd("updatedAt", new JsonSchemaProperty
        { 
            Type = JsonObjectType.String,
            Description = "The ISO-8601 date/time string describing the last time the entity was updated with ms accuracy.",
            IsRequired = false
        });
        _ = schema.Properties.TryAdd("version", new JsonSchemaProperty
        {
            Type = JsonObjectType.String, 
            Description = "An opaque string that changes whenever the entity changes.", 
            IsRequired = false 
        });

        return;
    }

    /// <summary>
    /// Either reads or generates the required entity type schema.
    /// </summary>
    /// <param name="context">The context for the operation processor.</param>
    /// <param name="entityType">The entity type needed.</param>
    /// <returns>A reference to the entity schema.</returns>
    private static JsonSchema GetEntityReference(OperationProcessorContext context, Type entityType)
    {
        string schemaName = context.SchemaGenerator.Settings.SchemaNameGenerator.Generate(entityType);
        if (!context.Document.Definitions.TryGetValue(schemaName, out JsonSchema? value))
        {
            JsonSchema newSchema = context.SchemaGenerator.Generate(entityType);
            value = newSchema;
            context.Document.Definitions.Add(schemaName, value);
        }

        JsonSchema actualSchema = value;
        return new JsonSchema { Reference = actualSchema };
    }

    /// <summary>
    /// Creates the paged item schema reference.
    /// </summary>
    /// <param name="entitySchema">The entity schema reference.</param>
    /// <param name="entityName">The name of the entity handled by the list operation.</param>
    /// <returns>The list schema reference</returns>
    private static JsonSchema CreateListSchema(JsonSchema entitySchema, string entityName)
    {
        JsonSchema listSchemaRef = new()
        {
            Description = $"A page of {entityName} entities",
            Type = JsonObjectType.Object
        };
        listSchemaRef.Properties["items"] = new JsonSchemaProperty
        {
            Description = "The entities in this page of results",
            Type = JsonObjectType.Array,
            Item = entitySchema,
            IsReadOnly = true,
            IsNullableRaw = true
        };
        listSchemaRef.Properties["count"] = new JsonSchemaProperty
        {
            Description = "The count of all entities in the result set",
            Type = JsonObjectType.Integer,
            IsReadOnly = true,
            IsNullableRaw = true
        };
        listSchemaRef.Properties["nextLink"] = new JsonSchemaProperty
        {
            Description = "The URI to the next page of entities",
            Type = JsonObjectType.String,
            Format = "uri",
            IsReadOnly = true,
            IsNullableRaw = true
        };
        return listSchemaRef;
    }
}
