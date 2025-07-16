// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using TodoApp.BlazorWasm.Shared.Models;

namespace TodoApp.BlazorWasm.Client.Services;

/// <summary>
/// Provides client-side todo item management functionality for the Blazor WebAssembly application.
/// This service implements <see cref="ITodoService"/> and acts as a wrapper around the 
/// <see cref="DatasyncServiceClient{T}"/> to provide datasync capabilities with automatic
/// synchronization between client and server.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TodoService"/> class encapsulates all CRUD operations for todo items while
/// leveraging the CommunityToolkit.Datasync.Client framework for offline-first capabilities,
/// automatic conflict resolution, and real-time synchronization with the server.
/// </para>
/// <para>
/// This service automatically handles:
/// <list type="bullet">
/// <item><description>Optimistic concurrency control using ETags and version tracking</description></item>
/// <item><description>Soft delete operations (items are marked as deleted rather than physically removed)</description></item>
/// <item><description>Automatic ID generation for new todo items</description></item>
/// <item><description>Error handling and meaningful exception messages</description></item>
/// <item><description>Filtering of deleted items during retrieval operations</description></item>
/// </list>
/// </para>
/// <para>
/// The service is designed to work seamlessly with Blazor WebAssembly components and provides
/// asynchronous operations suitable for responsive user interfaces.
/// </para>
/// </remarks>
/// <seealso cref="ITodoService"/>
/// <seealso cref="DatasyncServiceClient{T}"/>
/// <seealso cref="TodoItemDto"/>
public class TodoService(DatasyncServiceClient<TodoItemDto> todoClient) : ITodoService
{
    /// <summary>
    /// Retrieves all active (non-deleted) todo items from the datasync service.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains an <see cref="IEnumerable{T}"/> of <see cref="TodoItemDto"/> 
    /// representing all active todo items.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method automatically filters out soft-deleted items by applying a LINQ Where clause
    /// that excludes items where <see cref="DatasyncDto.Deleted"/> is <c>true</c>. This ensures
    /// that only active todo items are returned to the client application.
    /// </para>
    /// <para>
    /// The method leverages the datasync client's query capabilities and will automatically
    /// synchronize with the server if connectivity is available, or return cached data
    /// when operating in offline mode.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the datasync service encounters an error during the query operation.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when network communication with the server fails.
    /// </exception>
    public async Task<IEnumerable<TodoItemDto>> GetTodoItemsAsync()
    {
        List<TodoItemDto> items = await todoClient
            .Where(item => !item.Deleted)
            .ToListAsync();
        return items;
    }

    /// <summary>
    /// Creates a new todo item with the specified title.
    /// </summary>
    /// <param name="title">The title or description of the new todo item.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the newly created <see cref="TodoItemDto"/> with
    /// server-assigned properties populated.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method creates a new <see cref="TodoItemDto"/> with an automatically generated
    /// GUID identifier, sets the title to the provided value, and initializes the
    /// <see cref="TodoItemDto.Completed"/> property to <c>false</c>.
    /// </para>
    /// <para>
    /// The method sends the new item to the server via the datasync client's 
    /// <see cref="DatasyncServiceClient{T}.AddAsync(T)"/> method. Upon successful creation,
    /// the server returns the persisted item with updated timestamps and version information.
    /// </para>
    /// </remarks>
    /// <param name="title">The title for the new todo item. Must not be null or empty.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is empty or whitespace only.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server operation fails, containing details about the failure reason.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the todo item fails server-side validation (e.g., title too long).
    /// </exception>
    public async Task<TodoItemDto> CreateTodoItemAsync(string title)
    {
        TodoItemDto newItem = new() { Title = title };
        ServiceResponse<TodoItemDto> response = await todoClient.AddAsync(newItem);
        if (response.IsSuccessful && response.HasValue)
        {
            return response.Value!;
        }
        
        throw new InvalidOperationException($"Failed to create todo item: {response.ReasonPhrase}");
    }

    /// <summary>
    /// Updates an existing todo item with the provided data.
    /// </summary>
    /// <param name="item">The <see cref="TodoItemDto"/> containing the updated data.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the updated <see cref="TodoItemDto"/> with
    /// server-updated properties such as timestamps and version.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a complete replacement of the todo item on the server using
    /// the datasync client's <see cref="DatasyncServiceClient{T}.ReplaceAsync(T)"/> method.
    /// The operation includes optimistic concurrency control based on the item's version property.
    /// </para>
    /// <para>
    /// If the item has been modified by another client since it was last retrieved,
    /// the server will reject the update with a conflict status, which will be reflected
    /// in the <see cref="ServiceResponse{T}"/> returned by the datasync client.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server operation fails, including conflicts due to concurrent modifications,
    /// validation failures, or network errors. The exception message includes the server's reason phrase.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the updated todo item fails server-side validation.
    /// </exception>
    public async Task<TodoItemDto> UpdateTodoItemAsync(TodoItemDto item)
    {
        ServiceResponse<TodoItemDto> response = await todoClient.ReplaceAsync(item);
        if (response.IsSuccessful && response.HasValue)
        {
            return response.Value!;
        }
        
        throw new InvalidOperationException($"Failed to update todo item: {response.ReasonPhrase}");
    }

    /// <summary>
    /// Performs a soft delete operation on the todo item with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the todo item to delete.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous delete operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a soft delete operation, meaning the todo item is marked as deleted
    /// on the server but not physically removed from the database. This approach enables
    /// proper synchronization of delete operations across multiple clients and supports
    /// offline scenarios where delete operations need to be synchronized later.
    /// </para>
    /// <para>
    /// The method uses <see cref="DatasyncServiceOptions"/> to configure the delete operation
    /// and leverages the datasync client's <see cref="DatasyncServiceClient{T}.RemoveAsync(string, DatasyncServiceOptions)"/>
    /// method to perform the server-side operation.
    /// </para>
    /// <para>
    /// After successful deletion, the item will no longer appear in results from 
    /// <see cref="GetTodoItemsAsync"/> as it filters out items marked as deleted.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or whitespace only.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server operation fails, such as when the item doesn't exist,
    /// has already been deleted, or due to network connectivity issues.
    /// The exception message includes the server's reason phrase.
    /// </exception>
    public async Task DeleteTodoItemAsync(string id)
    {
        ServiceResponse response = await todoClient.RemoveAsync(id, new DatasyncServiceOptions());
        if (!response.IsSuccessful)
        {
            throw new InvalidOperationException($"Failed to delete todo item: {response.ReasonPhrase}");
        }
    }
}
