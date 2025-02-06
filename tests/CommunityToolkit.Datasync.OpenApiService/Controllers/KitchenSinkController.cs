// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.OpenApiService.Models;
using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CommunityToolkit.Datasync.OpenApiService.Controllers;

[Route("tables/kitchensink")]
[ExcludeFromCodeCoverage]
public class KitchenSinkController : TableController<KitchenSink>
{
    public KitchenSinkController(ServiceDbContext context, ILogger<KitchenSinkController> logger) : base()
    {
        Repository = new EntityTableRepository<KitchenSink>(context);
        Logger = logger;
    }
}
