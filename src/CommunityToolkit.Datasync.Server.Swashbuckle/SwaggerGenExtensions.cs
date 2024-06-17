// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace CommunityToolkit.Datasync.Server.Swashbuckle;

/// <summary>
/// Extensions to enable the developer to wire in the Datasync Controllers into
/// a Swashbuckle SwaggerGen() pipeline.
/// </summary>
public static class SwaggerGenExtensions
{
    /// <summary>
    /// Adds the Datasync <see cref="IDocumentFilter"/> to the SwaggerGen pipeline.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> representing the Swashbuckle configuration pipeline.</param>
    public static void AddDatasyncControllers(this SwaggerGenOptions options)
    {
        options.DocumentFilter<DatasyncDocumentFilter>(Assembly.GetCallingAssembly());
    }
}
