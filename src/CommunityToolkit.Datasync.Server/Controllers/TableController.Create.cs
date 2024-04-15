// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Datasync.Server;

public partial class TableController<TEntity> : ODataController where TEntity : class, ITableData
{
    /// <summary>
    /// Creates a new entity in the repository.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A <see cref="CreatedAtActionResult"/> for the created entity.</returns>
    /// <exception cref="HttpException">Thrown if there is an HTTP exception, such as unauthorized usage.</exception>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public virtual async Task<IActionResult> CreateAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("CreateAsync");
        TEntity entity = await DeserializeJsonContent(cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("CreateAsync: {entity}", entity.ToJsonString());

        await AuthorizeRequestAsync(TableOperation.Create, entity, cancellationToken).ConfigureAwait(false);

        await AccessControlProvider.PreCommitHookAsync(TableOperation.Create, entity, cancellationToken).ConfigureAwait(false);

        await Repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);

        await PostCommitHookAsync(TableOperation.Create, entity, cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("CreateAsync: created {entity}", entity.ToJsonString());
        return CreatedAtRoute(new { id = entity.Id }, entity);
    }
}
