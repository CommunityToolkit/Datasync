// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A set of extensions for integrating the Datasync Toolkit into the ASP.NET Core
/// application pipeline.
/// </summary>
public static class AspNetCoreExtensions
{
    public static IServiceCollection AddDatasyncServices(this IServiceCollection services, IEdmModel? model = null)
    {
        services.AddSingleton<IDatasyncServiceOptions, DatasyncServiceOptions>();
        if (model != null) 
        {
            services.AddSingleton<IEdmModel>(model);
        }
        return services;
    }
}
