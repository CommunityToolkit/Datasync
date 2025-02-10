// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CommunityToolkit.Datasync.Server.OpenApi;

/// <summary>
/// The document transformer for the Datasync services.
/// </summary>
public class DatasyncSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
