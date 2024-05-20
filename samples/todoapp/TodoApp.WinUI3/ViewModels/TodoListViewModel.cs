// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.WinUI3.Services;

namespace TodoApp.WinUI3.ViewModels;

/// <summary>
/// The view model for the TodoListWindow.
/// </summary>
public partial class TodoListViewModel(ITodoService service) : ObservableRecipient
{
    /// <summary>
    /// The implementation of the <see cref="ITodoService"/> that should be used as the data store.
    /// </summary>
    internal ITodoService TodoService { get; } = service;

    [ObservableProperty]
    private readonly ConcurrentObservableCollection<TodoItem> items = [];

    /// <summary>
    /// Refreshes the list of items from the data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is finished.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<TodoItem> currentItemList = await TodoService.GetAllAsync(cancellationToken);
        Items.ReplaceAll(currentItemList);
    }
}
