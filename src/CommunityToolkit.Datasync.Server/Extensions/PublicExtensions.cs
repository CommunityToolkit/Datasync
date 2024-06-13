// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A set of extension methods used to provide public APIs for the server-side components of the toolkit.
/// </summary>
public static class PublicExtensions
{
    /// <summary>
    /// Adds the services necessary to provide OData functionality for the Datasync service, building
    /// the <see cref="IEdmModel"/> from the entities configured via <see cref="TableController{TEntity}"/>.
    /// </summary>
    /// <param name="services">The current service collection.</param>
    /// <param name="model">The (optional) <see cref="IEdmModel"/> to use for configuring OData.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddDatasyncServices(this IServiceCollection services, IEdmModel? model = null)
    {
        _ = services.AddSingleton<IDatasyncServiceOptions, DatasyncServiceOptions>();
        if (model is not null)
        {
            _ = services.AddSingleton(model);
        }

        return services;
    }
}
