// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.Test;

[ExcludeFromCodeCoverage]
internal class ExposedTableController<TEntity> : TableController<TEntity> where TEntity : class, ITableData
{
    public ExposedTableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> provider) : base(repository, provider)
    {
        ObjectValidator = new ObjectValidator();
    }

    public ExposedTableController(IRepository<TEntity> repository, IAccessControlProvider<TEntity> provider, TableControllerOptions options) : base(repository, provider, options)
    {
        ObjectValidator = new ObjectValidator();
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used to indicate protected to public conversion")]
    public ValueTask __AuthorizeRequestAsync(TableOperation operation, TEntity entity, CancellationToken cancellationToken) => AuthorizeRequestAsync(operation, entity, cancellationToken);

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Used to indicate protected to public conversion")]
    public ValueTask __PostCommitHookAsync(TableOperation operation, TEntity entity, CancellationToken cancellationTken) => PostCommitHookAsync(operation, entity, cancellationTken);
}
