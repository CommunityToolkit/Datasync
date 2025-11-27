// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Mime;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace CommunityToolkit.Datasync.Server.OpenApi;

/// <summary>
/// A set of internal extensions for code readability.
/// </summary>
internal static class InternalExtensions
{
    /// <summary>
    /// Adds a request body to the operation.
    /// </summary>
    /// <param name="operation">The operation to modify.</param>
    /// <param name="bodySchema">The schema for the entity in the body.</param>
    internal static void AddRequestBody(this OpenApiOperation operation, IOpenApiSchema bodySchema)
    {
        OpenApiRequestBody requestBody = new();
        requestBody.Content ??= new Dictionary<string, OpenApiMediaType>();
        requestBody.Content.Add(MediaTypeNames.Application.Json, new OpenApiMediaType
        {
            Schema = bodySchema
        });
        requestBody.Description = "The entity to process.";
        requestBody.Required = true;
        operation.RequestBody ??= requestBody;
    }

    /// <summary>
    /// Adds a boolean query parameter to the operation parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    internal static void AddBooleanQueryParameter(this IList<IOpenApiParameter> parameters, string paramName, string description)
    {
        parameters.Add(new OpenApiParameter
        {
            Name = paramName,
            In = ParameterLocation.Query,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Boolean
            }
        });
    }

    /// <summary>
    /// Adds a date-time header to the parameter list.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <param name="headerName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    internal static void AddDateTimeHeader(this IList<IOpenApiParameter> parameters, string headerName, string description)
    {
        parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            In = ParameterLocation.Header,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" }
        });
    }

    /// <summary>
    /// Adds an entity response to the collection of responses.
    /// </summary>
    /// <param name="responses">The responses collection.</param>
    /// <param name="statusCode">The status code for the response.</param>
    /// <param name="schema">The schema of the entity.</param>
    /// <param name="includeConditionalHeaders">If true, include the headers for conditional access.</param>
    internal static void AddEntityResponse(this OpenApiResponses responses,
        int statusCode, IOpenApiSchema schema, bool includeConditionalHeaders = true)
    {
        OpenApiResponse response = new()
        {
            Description = ReasonPhrases.GetReasonPhrase(statusCode),
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            },
            Headers = includeConditionalHeaders ? new Dictionary<string, IOpenApiHeader>
            {
                ["ETag"] = new OpenApiHeader
                {
                    Description = "The ETag value for the entity",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                },
                ["Last-Modified"] = new OpenApiHeader
                {
                    Description = "The last modified timestamp for the entity",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" }
                }
            } : null
        };
        responses.Upsert(statusCode.ToString(), response);
    }

    /// <summary>
    /// Adds the <c>__includeDeleted</c> query parameter to the operation.
    /// </summary>
    /// <param name="parameters">The parameters list to modify.</param>
    internal static void AddIncludeDeletedQuery(this IList<IOpenApiParameter> parameters)
        => parameters.AddBooleanQueryParameter("__includeDeleted", "Include deleted items in the response ('true' or 'false').");

    /// <summary>
    /// Adds the <c>If-Modified-Since</c> header to the operation.
    /// </summary>
    /// <param name="parameters">The parameters list to modify.</param>
    internal static void AddIfModifiedSinceHeader(this IList<IOpenApiParameter> parameters)
        => parameters.AddDateTimeHeader("If-Modified-Since", "Timestamp to conditionally fetch the entity");

    /// <summary>
    /// Adds the <c>If-Modified-Since</c> header to the operation.
    /// </summary>
    /// <param name="parameters">The parameters list to modify.</param>
    internal static void AddIfUnmodifiedSinceHeader(this IList<IOpenApiParameter> parameters)
        => parameters.AddDateTimeHeader("If-Unmodified-Since", "Timestamp to conditionally fetch the entity");

    /// <summary>
    /// Adds the <c>If-None-Match</c> header to the operation.
    /// </summary>
    /// <param name="parameters">The parameters list to modify.</param>
    internal static void AddIfNoneMatchHeader(this IList<IOpenApiParameter> parameters)
        => parameters.AddStringHeader("If-None-Match", "ETag value to conditionally fetch the entity");

    /// <summary>
    /// Adds the <c>If-Match</c> header to the operation.
    /// </summary>
    /// <param name="parameters">The parameters list to modify.</param>
    internal static void AddIfMatchHeader(this IList<IOpenApiParameter> parameters)
        => parameters.AddStringHeader("If-Match", "ETag value to conditionally fetch the entity");

    /// <summary>
    /// Adds an int query parameter to the operation parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    /// <param name="minValue">The minimum value for the parameter.</param>
    internal static void AddIntQueryParameter(this IList<IOpenApiParameter> parameters, string paramName, string description, int minValue = 0)
    {
        parameters.Add(new OpenApiParameter
        {
            Name = paramName,
            In = ParameterLocation.Query,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Minimum = minValue.ToString() }
        });
    }

    /// <summary>
    /// Adds a status code with no content to the responses collection.
    /// </summary>
    /// <param name="responses">The responses collection.</param>
    /// <param name="statusCode">The status code to add.</param>
    internal static void AddStatusCode(this OpenApiResponses responses, int statusCode)
        => responses.Upsert(statusCode.ToString(), new OpenApiResponse { Description = ReasonPhrases.GetReasonPhrase(statusCode) });

    /// <summary>
    /// Adds a string header to the parameter list.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <param name="headerName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    internal static void AddStringHeader(this IList<IOpenApiParameter> parameters, string headerName, string description)
    {
        parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            In = ParameterLocation.Header,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }

    /// <summary>
    /// Adds a string query parameter to the operation parameters.
    /// </summary>
    /// <param name="parameters">The parameters collection.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="description">The parameter description.</param>
    internal static void AddStringQueryParameter(this IList<IOpenApiParameter> parameters, string paramName, string description)
    {
        parameters.Add(new OpenApiParameter
        {
            Name = paramName,
            In = ParameterLocation.Query,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }

    private static readonly string[] SystemProperties = ["updatedAt", "version", "deleted"];
    internal static void MakeSystemPropertiesReadonly(this OpenApiSchema schema)
    {
        foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (SystemProperties.Contains(property.Key) && property.Value is OpenApiSchema prop)
            {
                prop.ReadOnly = true;
            }
        }
    }

    /// <summary>
    /// Converts the property name to JSON casing.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions object being used.</param>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name.</returns>
    internal static string ToJsonCasing(this IDatasyncServiceOptions options, string name)
        => options.JsonSerializerOptions.PropertyNamingPolicy?.ConvertName(name) ?? name;

    /// <summary>
    /// Upserts a response into the responses collection, overwriting the old version if it is there.
    /// </summary>
    /// <param name="responses">The responses collection.</param>
    /// <param name="key">The key - generally the status code.</param>
    /// <param name="response">The response object.</param>
    internal static void Upsert(this OpenApiResponses responses, string key, OpenApiResponse response)
    {
        _ = responses.Remove(key);
        responses.Add(key, response);
    }
}
