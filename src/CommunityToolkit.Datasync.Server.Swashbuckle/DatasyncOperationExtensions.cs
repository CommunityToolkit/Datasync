// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.OpenApi.Models;

namespace CommunityToolkit.Datasync.Server.Swashbuckle;

internal static class DatasyncOperationExtensions
{
    private const string JsonMediaType = "application/json";
    private static readonly string[] SystemProperties = ["updatedAt", "version", "deleted"];

    /// <summary>
    /// Adds an appropriate conditional header (either <c>If-Match</c> or <c>If-None-Match</c> to the list of
    /// allowed headers.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> reference.</param>
    /// <param name="ifNoneMatch">If <c>true</c>, add a <c>If-None-Match</c> header.</param>
    internal static void AddConditionalHeader(this OpenApiOperation operation, bool ifNoneMatch = false)
    {
        string headerName = ifNoneMatch ? "If-None-Match" : "If-Match";
        string description = ifNoneMatch
            ? "Conditionally execute only if the entity version does not match the provided string (RFC 9110 13.1.2)."
            : "Conditionally execute only if the entity version matches the provided string (RFC 9110 13.1.1).";

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            Description = description,
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" }
        });
    }

    /// <summary>
    /// Adds the OData query parameter to the operation.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> reference.</param>
    /// <param name="parameterName">The name of the query parameter.</param>
    /// <param name="parameterType">The OpenAPI type for the query parameter.</param>
    /// <param name="description">The OpenAPI description for the query parameter.</param>
    internal static void AddODataQueryParameter(this OpenApiOperation operation, string parameterName, string parameterType, string description)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = parameterName,
            Description = description,
            In = ParameterLocation.Query,
            Required = false,
            Schema = new OpenApiSchema { Type = parameterType }
        });
    }

    /// <summary>
    /// Adds the OData query parameters for the <c>GET list</c> operation.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> reference.</param>
    internal static void AddODataQueryParameters(this OpenApiOperation operation)
    {
        operation.AddODataQueryParameter("$count", "boolean", "If true, return the total number of items matched by the filter");
        operation.AddODataQueryParameter("$filter", "string", "An OData filter describing the entities to be returned");
        operation.AddODataQueryParameter("$orderby", "string", "A comma-separated list of ordering instructions.  Each ordering instruction is a field name with an optional direction (asc or desc).");
        operation.AddODataQueryParameter("$select", "string", "A comma-separated list of fields to be returned in the result set.");
        operation.AddODataQueryParameter("$skip", "integer", "The number of items in the list to skip for paging support.");
        operation.AddODataQueryParameter("$top", "integer", "The number of items in the list to return for paging support.");
        operation.AddODataQueryParameter("__includedeleted", "boolean", "If true, soft-deleted items are returned as well as non-deleted items.");
    }

    /// <summary>
    /// Adds or replaces a response with JSON content and an ETag.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> to modify.</param>
    /// <param name="statusCode">The HTTP status code to model.</param>
    /// <param name="description">The description of the HTTP status code.</param>
    /// <param name="schema">The schema of the entity to return.</param>
    internal static void AddResponseWithContent(this OpenApiOperation operation, string statusCode, string description, OpenApiSchema schema)
    {
        OpenApiResponse response = new()
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [JsonMediaType] = new OpenApiMediaType { Schema = schema }
            }
        };
        string etagDescription = statusCode is "409" or "412"
            ? "The opaque versioning identifier of the conflicting entity"
            : "The opaque versioning identifier of the entity";
        response.Headers.Add("ETag", new OpenApiHeader
        {
            Schema = new OpenApiSchema { Type = "string" },
            Description = $"{etagDescription}, per RFC 9110 8.8.3."
        });
        operation.Responses[statusCode] = response;
    }
    /// <summary>
    /// Adds the content type and schema to the request body.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> to modify.</param>
    /// <param name="schema">The schema of the entity in the request.</param>
    internal static void AddRequestWithContent(this OpenApiOperation operation, OpenApiSchema schema)
    {
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [JsonMediaType] = new OpenApiMediaType()
                {
                    Schema = schema
                }
            }
        };
    }

    /// <summary>
    /// Adds or replaces the 409/412 Conflict/Precondition Failed response.
    /// </summary>
    /// <param name="operation">The <see cref="OpenApiOperation"/> to modify.</param>
    /// <param name="schema">The schema of the entity to return.</param>
    internal static void AddConflictResponse(this OpenApiOperation operation, OpenApiSchema schema)
    {
        operation.AddResponseWithContent("409", "Conflict", schema);
        operation.AddResponseWithContent("412", "Precondition failed", schema);
    }

    /// <summary>
    /// Makes the system properties in the schema read-only.
    /// </summary>
    /// <param name="schema">The <see cref="OpenApiSchema"/> to edit.</param>
    public static void MakeSystemPropertiesReadonly(this OpenApiSchema schema)
    {
        foreach (KeyValuePair<string, OpenApiSchema> property in schema.Properties)
        {
            if (SystemProperties.Contains(property.Key))
            {
                property.Value.ReadOnly = true;
            }
        }
    }
}
