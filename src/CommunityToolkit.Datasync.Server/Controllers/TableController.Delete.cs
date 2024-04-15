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
    /// Requests that the repository deletes an entity or marks an entity as deleted (depending on the EnableSoftDelete option).
    /// </summary>
    /// <param name="id">Tne ID of the entity to be deleted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A <see cref="NoContentResult"/> when complete.</returns>
    /// <exception cref="HttpException">Thrown if there is an HTTP exception, such as unauthorized usage.</exception>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<IActionResult> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("DeleteAsync: {id}", id);
        TEntity entity = await Repository.ReadAsync(id, cancellationToken).ConfigureAwait(false);

        if (!AccessControlProvider.EntityIsInView(entity))
        {
            Logger.LogWarning("DeleteAsync: {id} statusCode=404 not in view", id);
            throw new HttpException(StatusCodes.Status404NotFound);
        }

        await AuthorizeRequestAsync(TableOperation.Delete, entity, cancellationToken).ConfigureAwait(false);

        if (Options.EnableSoftDelete && entity.Deleted)
        {
            Logger.LogWarning("DeleteAsync: {id} statusCode=410 already deleted", id);
            throw new HttpException(StatusCodes.Status410Gone);
        }

        Request.ParseConditionalRequest(entity, out byte[] version);

        if (Options.EnableSoftDelete)
        {
            Logger.LogInformation("DeleteAsync: deleted {id} (soft-delete)", id);
            entity.Deleted = true;
            await AccessControlProvider.PreCommitHookAsync(TableOperation.Update, entity, cancellationToken).ConfigureAwait(false);
            await Repository.ReplaceAsync(entity, version, cancellationToken).ConfigureAwait(false);
            await PostCommitHookAsync(TableOperation.Update, entity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Logger.LogInformation("DeleteAsync: deleted {id} (hard-delete)", id);
            await Repository.DeleteAsync(id, version, cancellationToken).ConfigureAwait(false);
            await PostCommitHookAsync(TableOperation.Delete, entity, cancellationToken).ConfigureAwait(false);
        }

        return NoContent();
    }
}
