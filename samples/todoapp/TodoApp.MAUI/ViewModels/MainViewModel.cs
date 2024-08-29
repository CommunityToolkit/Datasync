// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Windows.Input;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.Utils;

namespace TodoApp.MAUI.ViewModels;

public class MainViewModel(AppDbContext context) : INotifyPropertyChanged
{
    private bool _isRefreshing = false;

    public IMVVMHelper? MvvmHelper { get; set; }

    public ICommand AddItemCommand
        => new Command<Entry>(async (Entry entry) => await AddItemAsync(entry.Text));

    public ICommand RefreshItemsCommand
        => new Command(async () => await RefreshItemsAsync());

    public ICommand SelectItemCommand
        => new Command<TodoItem>(async (TodoItem item) => await UpdateItemAsync(item.Id, !item.IsComplete));

    public ConcurrentObservableCollection<TodoItem> Items { get; } = new();

    public bool IsRefreshing
    {
        get => this._isRefreshing;
        set => SetProperty(ref this._isRefreshing, value, nameof(IsRefreshing));
    }

    public async void OnActivated()
    {
        await RefreshItemsAsync();
    }

    public async Task RefreshItemsAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            await context.SynchronizeAsync();
            List<TodoItem> items = await context.TodoItems.ToListAsync();
            await MvvmHelper!.RunOnUiThreadAsync(() =>
            {
                Items.ReplaceAll(items);
            });
        }
        catch (Exception ex)
        {
            await MvvmHelper!.DisplayErrorAlertAsync("RefreshItems", ex.Message);
        }
        finally
        {
            await SetRefreshing(false);
        }
    }

    public async Task UpdateItemAsync(string itemId, bool isComplete)
    {
        try
        {
            TodoItem? item = await context.TodoItems.FindAsync([itemId]);
            if (item is not null)
            {
                item.IsComplete = isComplete;
                _ = context.TodoItems.Update(item);
                _ = Items.ReplaceIf(x => x.Id == itemId, item);
                _ = await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            await MvvmHelper!.DisplayErrorAlertAsync("UpdateItem", ex.Message);
        }
    }

    public async Task AddItemAsync(string text)
    {
        try
        {
            TodoItem item = new() { Title = text };
            _ = context.TodoItems.Add(item);
            _ = await context.SaveChangesAsync();
            Items.Add(item);
        }
        catch (Exception ex)
        {
            await MvvmHelper!.DisplayErrorAlertAsync("AddItem", ex.Message);
        }
    }

    private Task SetRefreshing(bool value)
        => MvvmHelper!.RunOnUiThreadAsync(() => IsRefreshing = value);

    #region INotifyPropertyChanged
    /// <summary>
    /// The event handler required by <see cref="INotifyPropertyChanged"/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets a backing store value and notify watchers of the change.  The type must
    /// implement <see cref="IEquatable{T}"/> for proper comparisons.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="storage">The backing store</param>
    /// <param name="value">The new value</param>
    /// <param name="propertyName"></param>
    protected void SetProperty<T>(ref T storage, T value, string? propertyName = null) where T : notnull
    {
        if (!storage.Equals(value))
        {
            storage = value;
            NotifyPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// Notifies the data context that the property named has changed value.
    /// </summary>
    /// <param name="propertyName">The name of the property</param>
    protected void NotifyPropertyChanged(string? propertyName = null)
    {
        if (propertyName != null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
}
