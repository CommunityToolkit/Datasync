// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.OpenApiService.Models;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CommunityToolkit.Datasync.OpenApiService.Controllers;

[Route("tables/kitchenreader")]
[ExcludeFromCodeCoverage]
public class KitchenReaderController : ReadonlyTableController<KitchenSink>
{
    public KitchenReaderController(ServiceDbContext context, ILogger<KitchenReaderController> logger) : base()
    {
        Repository = new EntityTableRepository<KitchenSink>(context);
        Logger = logger;
    }
}
