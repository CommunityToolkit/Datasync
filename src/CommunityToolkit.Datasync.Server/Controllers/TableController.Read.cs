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
    /// Retrieves an entity from the repository.
    /// </summary>
    /// <param name="id">The ID of the entity to be returned.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An <see cref="OkObjectResult"/> encapsulating the entity value.</returns>
    /// <exception cref="HttpException">Thrown if there is an HTTP exception, such as unauthorized usage.</exception>
    [HttpGet("{id}")]
    [ActionName("ReadAsync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> ReadAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("ReadAsync: {id}", id);
        TEntity entity = await Repository.ReadAsync(id, cancellationToken).ConfigureAwait(false);

        if (!AccessControlProvider.EntityIsInView(entity))
        {
            Logger.LogWarning("ReadAsync: {id} statusCode=404 not in view", id);
            throw new HttpException(StatusCodes.Status404NotFound);
        }

        await AuthorizeRequestAsync(TableOperation.Read, entity, cancellationToken).ConfigureAwait(false);

        if (Options.EnableSoftDelete && entity.Deleted && !Request.ShouldIncludeDeletedEntities())
        {
            Logger.LogWarning("ReadAsync: {id} statusCode=410 deleted", id);
            throw new HttpException(StatusCodes.Status410Gone);
        }

        Request.ParseConditionalRequest(entity, out _);

        Logger.LogInformation("ReadAsync: read {entity}", entity.ToJsonString());
        return Ok(entity);
    }
}
