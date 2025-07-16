// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace TodoApp.BlazorWasm.Server.Database;

/// <summary>
/// Represents the Entity Framework Core database context for the Todo application.
/// This class manages the database connection and provides access to the TodoItems table
/// for the Blazor WebAssembly server-side data operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TodoContext"/> class is configured to use an in-memory database for development
/// and testing purposes. It inherits from <see cref="DbContext"/> and provides the necessary
/// infrastructure for Entity Framework Core operations including database creation, data seeding,
/// and change tracking.
/// </para>
/// <para>
/// This context is registered as a service in the dependency injection container and is used
/// by the datasync framework to provide real-time synchronization capabilities between
/// server and client applications.
/// </para>
/// </remarks>
/// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
/// <seealso cref="DbContext"/>
/// <seealso cref="TodoItem"/>
/// <seealso cref="TodoApp.BlazorWasm.Server.Controllers.TodoItemsController"/>
public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="TodoItem"/>.
    /// </summary>
    /// <value>
    /// A <see cref="DbSet{TodoItem}"/> that provides access to the TodoItems table in the database.
    /// </value>
    /// <remarks>
    /// This property represents the TodoItems table in the database and provides methods
    /// for querying, adding, updating, and deleting todo items. The DbSet is automatically
    /// configured by Entity Framework Core based on the <see cref="TodoItem"/> entity definition.
    /// </remarks>
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    /// <summary>
    /// Initializes the database by ensuring it exists and populating it with sample data if empty.
    /// This method is typically called during application startup to prepare the database for use.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous database initialization operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item><description>Ensures the database is created using <see cref="DatabaseFacade.EnsureCreatedAsync"/>.</description></item>
    /// <item><description>Checks if the TodoItems table is empty using <see cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource})"/>.</description></item>
    /// <item><description>If empty, clears the change tracker to avoid entity tracking conflicts.</description></item>
    /// <item><description>Adds three sample todo items with predefined titles.</description></item>
    /// <item><description>Saves the sample data to the database.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The sample data includes three todo items with titles related to learning Blazor WebAssembly,
    /// building applications, and deployment. All sample items are created with their default
    /// completion status (not completed).
    /// </para>
    /// <para>
    /// The method uses <see cref="ChangeTracker.Clear"/> before adding sample data to prevent
    /// entity tracking conflicts that could occur if entities with duplicate keys are being tracked.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database operations fail, such as when the database cannot be created
    /// or when saving changes fails due to validation or constraint violations.
    /// </exception>
    /// <exception cref="DbUpdateException">
    /// Thrown when an error occurs while saving changes to the database during the sample data insertion.
    /// </exception>
    public async Task InitializeDatabaseAsync()
    {
        _ = await Database.EnsureCreatedAsync();
        
        // Add some sample data if the database is empty
        if (!await TodoItems.AnyAsync())
        {
            // Clear any existing tracked entities to avoid conflicts
            ChangeTracker.Clear();
            
            TodoItem[] sampleItems = 
            [
                new TodoItem { Title = "Learn Blazor WASM" },
                new TodoItem { Title = "Build awesome apps" },
                new TodoItem { Title = "Deploy to production" }
            ];
            
            TodoItems.AddRange(sampleItems);
            _ = await SaveChangesAsync();
        }
    }
}
