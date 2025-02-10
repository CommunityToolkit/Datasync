// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.OpenApi;

namespace CommunityToolkit.Datasync.Server.OpenApi;

/// <summary>
/// A set of extension methods to make it easier to configure the OpenAPI options for the Datasync services.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds the appropriate transformers to the OpenAPI options to support the Datasync services.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to modify.</param>
    /// <returns>The modified <see cref="OpenApiOptions"/> for chaining.</returns>
    public static OpenApiOptions AddDatasyncTransformers(this OpenApiOptions options)
    {
        _ = options
            .AddDocumentTransformer<DatasyncDocumentTransformer>()
            .AddOperationTransformer<DatasyncOperationTransformer>()
            .AddSchemaTransformer<DatasyncSchemaTransformer>();

        return options;
    }
}
