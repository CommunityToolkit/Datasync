// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.Mvc;

namespace CommunityToolkit.Datasync.OpenApiService.Controllers;

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
