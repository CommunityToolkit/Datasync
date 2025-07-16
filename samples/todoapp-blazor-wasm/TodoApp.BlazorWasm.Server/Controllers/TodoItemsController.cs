// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TodoApp.BlazorWasm.Server.Database;

namespace TodoApp.BlazorWasm.Server.Controllers;

/// <summary>
/// Provides RESTful API endpoints and OData query capabilities for managing todo items in the Blazor WebAssembly application.
/// This controller inherits from <see cref="TableController{TEntity}"/> to provide full CRUDL (Create, Read, Update, Delete, List)
/// operations with datasync capabilities from the CommunityToolkit.Datasync.Server framework.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TodoItemsController"/> exposes the following HTTP endpoints for todo item operations:
/// <list type="bullet">
/// <item><description><c>GET /tables/todoitems</c> - Retrieves a list of todo items with optional OData query parameters for filtering, sorting, and paging</description></item>
/// <item><description><c>GET /tables/todoitems/{id}</c> - Retrieves a specific todo item by its unique identifier</description></item>
/// <item><description><c>POST /tables/todoitems</c> - Creates a new todo item</description></item>
/// <item><description><c>PUT /tables/todoitems/{id}</c> - Updates an existing todo item (full replacement)</description></item>
/// <item><description><c>PATCH /tables/todoitems/{id}</c> - Partially updates an existing todo item</description></item>
/// <item><description><c>DELETE /tables/todoitems/{id}</c> - Deletes a todo item (supports both soft and hard delete based on configuration)</description></item>
/// </list>
/// </para>
/// <para>
/// The controller supports OData query operations including <c>$filter</c>, <c>$orderby</c>, <c>$skip</c>, <c>$top</c>, 
/// and <c>$select</c> for advanced querying capabilities. It also provides automatic ETag support for optimistic 
/// concurrency control and delta synchronization features for offline-capable applications.
/// </para>
/// <para>
/// All operations are performed through the <see cref="EntityTableRepository{TEntity}"/> which provides 
/// Entity Framework Core integration for data persistence and change tracking.
/// </para>
/// </remarks>
/// <seealso cref="TableController{TEntity}"/>
/// <seealso cref="TodoItem"/>
/// <seealso cref="TodoContext"/>
/// <seealso cref="EntityTableRepository{TEntity}"/>
[Route("tables/todoitems")]
public class TodoItemsController : TableController<TodoItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoItemsController"/> class.
    /// </summary>
    /// <param name="context">
    /// The <see cref="TodoContext"/> that provides access to the todo items database.
    /// This context is used to create the underlying <see cref="EntityTableRepository{TEntity}"/>
    /// for data access operations.
    /// </param>
    /// <remarks>
    /// <para>
    /// The constructor configures the controller with an <see cref="EntityTableRepository{TEntity}"/>
    /// that uses the provided <see cref="TodoContext"/> for Entity Framework Core operations.
    /// This setup enables the controller to perform database operations while maintaining
    /// compatibility with the datasync framework's requirements.
    /// </para>
    /// <para>
    /// The repository handles all CRUD operations, change tracking, and optimistic concurrency
    /// control through Entity Framework Core, while the base <see cref="TableController{TEntity}"/>
    /// provides the REST API surface and OData query capabilities.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    public TodoItemsController(TodoContext context) : base()
    {
        Repository = new EntityTableRepository<TodoItem>(context);
    }
}
