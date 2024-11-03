// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.Services;

namespace TodoApp.MAUI.ViewModels;


partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isRefreshing = false;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItem> items = [];
    private readonly AppDbContext _context;
    private readonly IAlertService _alertService;



    public MainViewModel(AppDbContext context, IAlertService alertService)
    {
        _context = context;
        _alertService = alertService;
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            await _context.SynchronizeAsync(cancellationToken);
            List<TodoItem> items = await _context.TodoItems.ToListAsync(cancellationToken);
            Items.ReplaceAll(items);
        }
        catch (Exception ex)
        {
            await _alertService.ShowErrorAlertAsync("RefreshItems", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task UpdateItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? item = await _context.TodoItems.FindAsync([itemId]);
            if (item is not null)
            {
                item.IsComplete = !item.IsComplete;
                _ = _context.TodoItems.Update(item);
                _ = Items.ReplaceIf(x => x.Id == itemId, item);
                _ = await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _alertService.ShowErrorAlertAsync("UpdateItem", ex.Message);
        }
    }
    [RelayCommand]
    public async Task AddItemAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem item = new() { Title = text };
            _ = _context.TodoItems.Add(item);
            _ = await _context.SaveChangesAsync(cancellationToken);
            Items.Add(item);
        }
        catch (Exception ex)
        {
            await _alertService.ShowErrorAlertAsync("AddItem", ex.Message);
        }
    }
}
