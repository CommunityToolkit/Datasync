// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Filters;
using CommunityToolkit.Datasync.Server.Private;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CommunityToolkit.Datasync.Common;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// The base controller type for a datasync table controller.  This exposes a "CRUD" endpoint set
/// and a list endpoint that uses OData query options.
/// </summary>
/// <typeparam name="TEntity">The type of the entity exposed to the client.</typeparam>
[DatasyncController]
public partial class TableController<TEntity> : ODataController where TEntity : class, ITableData
{
    #region Controller constructors
    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/>. The table options (such as repository,
    /// access control provider, etc.) are set via the upstream controller.
    /// </summary>
    public TableController()
        : this(new Repository<TEntity>(), new AccessControlProvider<TEntity>(), null, new TableControllerOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    public TableController(IRepository<TEntity> repository)
        : this(repository, new AccessControlProvider<TEntity>(), null, new TableControllerOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    /// <param name="model">The <see cref="IEdmModel"/> to use for OData interactions.</param>
    public TableController(IRepository<TEntity> repository, IEdmModel model)
        : this(repository, new AccessControlProvider<TEntity>(), model, new TableControllerOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    /// <param name="accessControlProvider">The access control provider to use for this controller.</param>
    public TableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> accessControlProvider)
        : this(repository, accessControlProvider, null, new TableControllerOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    /// <param name="options">The <see cref="TableControllerOptions"/> to use for configuring this controller.</param>
    public TableController(IRepository<TEntity> repository, TableControllerOptions options)
        : this(repository, new AccessControlProvider<TEntity>(), null, options)
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    /// <param name="accessControlProvider">The access control provider to use for this controller.</param>
    /// <param name="options">The <see cref="TableControllerOptions"/> to use for configuring this controller.</param>
    public TableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> accessControlProvider, TableControllerOptions options)
        : this(repository, accessControlProvider, null, options)
    {
    }

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/> with the specified repository.
    /// </summary>
    /// <param name="repository">The repository to use for this controller.</param>
    /// <param name="accessControlProvider">The access control provider to use for this controller.</param>
    /// <param name="model">The <see cref="IEdmModel"/> to use for OData interactions.</param>
    public TableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> accessControlProvider, IEdmModel model)
        : this(repository, accessControlProvider, model, new TableControllerOptions())
    {
    }
    #endregion

    /// <summary>
    /// Creates a new <see cref="TableController{TEntity}"/>.
    /// </summary>
    /// <param name="repository">The repository that will be used for data access operations.</param>
    /// <param name="accessControlProvider">The access control provider that will be used for authorizing requests.</param>
    /// <param name="model">The <see cref="IEdmModel"/> to use for OData interactions (instead of generating the model).</param>
    /// <param name="options">The options for this table controller.</param>
    public TableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> accessControlProvider, IEdmModel? model, TableControllerOptions options)
    {
        Repository = repository;
        AccessControlProvider = accessControlProvider;
        EdmModel = model ?? ModelCache.GetEdmModel(typeof(TEntity));
        Options = options;

        if (EdmModel.FindType(typeof(TEntity).FullName) is null)
        {
            throw new InvalidOperationException($"The entity type {typeof(TEntity).FullName} is not registered in the OData EdmModel.");
        }
    }

    /// <summary>
    /// The access control provider that will be used for authorizing requests.
    /// </summary>
    public IAccessControlProvider<TEntity> AccessControlProvider { get; set; }

    /// <summary>
    /// The <see cref="IEdmModel"/> that is constructed for the service.
    /// </summary>
    public IEdmModel EdmModel { get; init; }

    /// <summary>
    /// The logger that is used for logging request/response information.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// The options for this table controller.
    /// </summary>
    public TableControllerOptions Options { get; set; }

    /// <summary>
    /// The repository that will be used for data access operations.
    /// </summary>
    public IRepository<TEntity> Repository { get; set; }

    /// <summary>
    /// An event handler to use for receiving notifications when the repository is updated.
    /// </summary>
    public event EventHandler<RepositoryUpdatedEventArgs>? RepositoryUpdated;

    /// <summary>
    /// Checks that the requestor is authorized to perform the requested operation on the provided entity.
    /// </summary>
    /// <param name="operation">The operation to be performed.</param>
    /// <param name="entity">The entity (pre-modification) to be operated on (null for query).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the authorization check is finished.</returns>
    /// <exception cref="HttpException">Thrown if the requestor is not authorized to perform the operation.</exception>
    [NonAction]
    protected virtual async ValueTask AuthorizeRequestAsync(TableOperation operation, TEntity? entity, CancellationToken cancellationToken = default)
    {
        bool isAuthorized = await AccessControlProvider.IsAuthorizedAsync(operation, entity, cancellationToken).ConfigureAwait(false);
        if (!isAuthorized)
        {
            Logger.LogWarning("{operation} {entity} statusCode=401 unauthorized", operation, entity?.ToJsonString() ?? "");
            throw new HttpException(Options.UnauthorizedStatusCode);
        }
    }

    /// <summary>
    /// Handles post-commit operation event handlers.
    /// </summary>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="entity">The entity that was updated (except for a hard-delete, which is the entity before deletion)</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A taks the completes when the post-commit hook has been called.</returns>
    [NonAction]
    protected virtual ValueTask PostCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken = default)
    {
        RepositoryUpdatedEventArgs args = new(operation, typeof(TEntity).Name, entity);
        RepositoryUpdated?.Invoke(this, args);
        return AccessControlProvider.PostCommitHookAsync(operation, entity, cancellationToken);
    }

    /// <summary>
    /// Deserializes the body content when submitting an entity to the service, using the datasync serializer options.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that returns the entity when complete.</returns>
    /// <exception cref="HttpException">If the content is invalid.</exception>
    [NonAction]
    protected async ValueTask<TEntity> DeserializeJsonContent(CancellationToken cancellationToken = default)
    {
        IDatasyncServiceOptions options = HttpContext.RequestServices?.GetService<IDatasyncServiceOptions>() ?? new DatasyncServiceOptions();
        HttpContext.Request.EnableBuffering();
        if (HttpContext.Request.HasJsonContentType())
        {
            TEntity entity = await JsonSerializer.DeserializeAsync<TEntity>(HttpContext.Request.Body, options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new HttpException(StatusCodes.Status400BadRequest, "Invalid JSON content");

            List<ValidationResult> validationErrors = [];
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validationErrors, true))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid entity") { Payload = validationErrors };
            }

            return entity;
        }
        else
        {
            throw new HttpException(StatusCodes.Status415UnsupportedMediaType, "Unsupported media type");
        }
    }
}
