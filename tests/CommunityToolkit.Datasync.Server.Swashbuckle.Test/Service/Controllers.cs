// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Datasync.Server.Swashbuckle.Test.Service;

[ExcludeFromCodeCoverage]
public abstract class ReadonlyTableController<TData> : TableController<TData> where TData : class, ITableData
{
    [NonAction]
    public override Task<IActionResult> CreateAsync(CancellationToken cancellationToken = default)
        => base.CreateAsync(cancellationToken);

    [NonAction]
    public override Task<IActionResult> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken = default)
        => base.DeleteAsync(id, cancellationToken);

    [NonAction]
    public override Task<IActionResult> ReplaceAsync([FromRoute] string id, CancellationToken cancellationToken = default)
        => base.ReplaceAsync(id, cancellationToken);
}

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

[Route("tables/[controller]")]
[ExcludeFromCodeCoverage]
public class TodoItemController : TableController<TodoItem>
{
    public TodoItemController(ServiceDbContext context) : base()
    {
        Repository = new EntityTableRepository<TodoItem>(context);
    }
}
