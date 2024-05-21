// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private bool isRefreshing;

    [RelayCommand]
    public async Task AddItemAsync(string title, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    [RelayCommand]
    public async Task EditItemAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}
