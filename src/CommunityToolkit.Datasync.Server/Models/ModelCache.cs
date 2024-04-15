// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Server.Private;

/// <summary>
/// Model cache for the <see cref="IEdmModel"/> needed for each table controller.
/// </summary>
internal static class ModelCache
{
    private static readonly ConcurrentDictionary<Type, IEdmModel> _cache = new();

    internal static IEdmModel GetEdmModel(Type type)
    {
        if (!_cache.TryGetValue(type, out IEdmModel? model))
        {
            ODataConventionModelBuilder modelBuilder = new();
            _ = modelBuilder.EnableLowerCamelCase().AddEntityType(type);
            model = modelBuilder.GetEdmModel();
            _ = _cache.TryAdd(type, model);
        }

        return model;
    }
}
