// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.WinUI3.Services;

namespace TodoApp.WinUI3.ViewModels;

/// <summary>
/// The view model for the TodoListWindow.
/// </summary>
public partial class TodoListViewModel(ITodoService service) : ObservableRecipient
{
    internal ITodoService TodoService { get; } = service;
    internal event EventHandler<NotificationEventArgs> NotificationHandler;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItem> items = [];

    [ObservableProperty]
    private string title = string.Empty;

    [RelayCommand]
    public async Task AddItemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem addition = await TodoService.CreateAsync(new() { Title = Title }, cancellationToken);
            Items.Add(addition);
            Title = string.Empty;
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Item Added", "", false));
        }
    }

    [RelayCommand]
    public async Task EditItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem item = Items.Single(x => x.Id == itemId);
            item.IsComplete = !item.IsComplete;
            TodoItem replacement = await TodoService.UpdateAsync(item, cancellationToken);
            _ = Items.ReplaceIf(x => x.Id == itemId, replacement);
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Item Updated", "", false));
        }
    }

    [RelayCommand]
    public async Task LoadPageAsync(CancellationToken cancellationToken = default)
    {
        await RefreshItemsAsync(cancellationToken);
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsRefreshing = true;
            IEnumerable<TodoItem> itemsFromService = await TodoService.GetAllAsync(cancellationToken);
            Items.ReplaceAll(itemsFromService);
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            IsRefreshing = false;
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Items Refreshed", "", false));
        }
    }
}
