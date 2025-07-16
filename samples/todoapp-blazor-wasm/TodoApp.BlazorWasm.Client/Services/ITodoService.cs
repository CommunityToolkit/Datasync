// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TodoApp.BlazorWasm.Shared.Models;

namespace TodoApp.BlazorWasm.Client.Services;

/// <summary>
/// Defines the contract for todo item management services in the Blazor WebAssembly application.
/// This interface provides an abstraction layer for CRUD operations on todo items, enabling
/// dependency injection and testability while supporting datasync capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ITodoService"/> interface establishes a service layer contract that abstracts
/// todo item operations from the underlying datasync implementation. This design enables:
/// <list type="bullet">
/// <item><description>Dependency injection and inversion of control in Blazor components</description></item>
/// <item><description>Unit testing with mock implementations</description></item>
/// <item><description>Flexibility to swap datasync implementations without changing consumer code</description></item>
/// <item><description>Clear separation of concerns between UI components and data access logic</description></item>
/// </list>
/// </para>
/// <para>
/// Implementations of this interface should provide:
/// <list type="bullet">
/// <item><description>Asynchronous operations suitable for responsive user interfaces</description></item>
/// <item><description>Proper error handling with meaningful exception messages</description></item>
/// <item><description>Support for offline-first scenarios when using datasync frameworks</description></item>
/// <item><description>Optimistic concurrency control for data consistency</description></item>
/// <item><description>Soft delete functionality for proper synchronization</description></item>
/// </list>
/// </para>
/// <para>
/// This interface is typically consumed by Blazor components and pages that need to perform
/// todo item operations, such as the TodoList component which displays and manages the user's todo items.
/// </para>
/// </remarks>
/// <seealso cref="TodoService"/>
/// <seealso cref="TodoItemDto"/>
/// <seealso cref="DatasyncDto"/>
public interface ITodoService
{
    /// <summary>
    /// Retrieves all active (non-deleted) todo items.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains an <see cref="IEnumerable{T}"/> of <see cref="TodoItemDto"/> 
    /// representing all active todo items available to the current user.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method should return only active todo items, automatically filtering out any
    /// items that have been soft-deleted. The implementation should leverage caching
    /// and synchronization capabilities when available to provide the most up-to-date
    /// data while supporting offline scenarios.
    /// </para>
    /// <para>
    /// The returned collection should be suitable for display in user interface components
    /// and may be used for filtering, sorting, and other client-side operations.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service encounters an error during the retrieval operation,
    /// such as network connectivity issues or server errors.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current user is not authorized to access todo items.
    /// </exception>
    Task<IEnumerable<TodoItemDto>> GetTodoItemsAsync();

    /// <summary>
    /// Creates a new todo item with the specified title.
    /// </summary>
    /// <param name="title">The title or description of the new todo item.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the newly created <see cref="TodoItemDto"/> with
    /// all server-assigned properties populated, including ID, timestamps, and version information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method creates a new todo item with the provided title. The implementation
    /// should automatically generate appropriate default values for other properties
    /// such as the completion status (typically false for new items) and unique identifier.
    /// </para>
    /// <para>
    /// The returned todo item should contain all properties populated by the server,
    /// including timestamps for creation and modification, version information for
    /// concurrency control, and any other metadata required by the datasync framework.
    /// </para>
    /// </remarks>
    /// <param name="title">
    /// The title for the new todo item. Should not be null, empty, or consist only of whitespace.
    /// The title length should comply with validation rules defined in <see cref="TodoItemDto"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is empty, consists only of whitespace,
    /// or violates length restrictions.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service fails to create the todo item due to server errors,
    /// network issues, or other operational problems.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the todo item data fails validation on the server side.
    /// </exception>
    Task<TodoItemDto> CreateTodoItemAsync(string title);

    /// <summary>
    /// Updates an existing todo item with the provided data.
    /// </summary>
    /// <param name="item">The <see cref="TodoItemDto"/> containing the updated data.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the updated <see cref="TodoItemDto"/> with
    /// refreshed server-managed properties such as timestamps and version information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a complete update of the specified todo item. The implementation
    /// should use optimistic concurrency control based on version information to detect
    /// and handle concurrent modifications by other clients.
    /// </para>
    /// <para>
    /// The method should update all user-modifiable properties of the todo item while
    /// preserving system-managed properties. The returned item will contain updated
    /// timestamps and version information reflecting the successful modification.
    /// </para>
    /// <para>
    /// If the item has been modified by another client since it was last retrieved,
    /// the implementation should detect this conflict and handle it appropriately,
    /// either by raising an exception or providing conflict resolution mechanisms.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the update operation fails due to server errors, network issues,
    /// concurrent modifications, or when the item no longer exists on the server.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the updated todo item data fails server-side validation.
    /// </exception>
    /// <exception cref="ConcurrencyException">
    /// Thrown when the item has been modified by another client and the update
    /// cannot be completed due to version conflicts.
    /// </exception>
    Task<TodoItemDto> UpdateTodoItemAsync(TodoItemDto item);

    /// <summary>
    /// Deletes the todo item with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the todo item to delete.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous delete operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a deletion of the specified todo item. The implementation
    /// should typically use soft delete functionality, marking the item as deleted
    /// rather than physically removing it from storage. This approach ensures proper
    /// synchronization of delete operations across multiple clients and supports
    /// offline scenarios.
    /// </para>
    /// <para>
    /// After successful deletion, the item should no longer appear in results from
    /// <see cref="GetTodoItemsAsync"/> but may still exist in the underlying storage
    /// for synchronization purposes.
    /// </para>
    /// <para>
    /// The method should handle cases where the item has already been deleted or
    /// no longer exists, typically treating such scenarios as successful operations
    /// since the desired end state (item not available) has been achieved.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the delete operation fails due to server errors, network connectivity
    /// issues, or when the operation cannot be completed for other reasons.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current user is not authorized to delete the specified todo item.
    /// </exception>
    Task DeleteTodoItemAsync(string id);
}
