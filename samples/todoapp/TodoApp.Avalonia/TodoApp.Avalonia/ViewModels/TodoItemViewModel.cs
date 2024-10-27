// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Avalonia.Database;
using TodoApp.Avalonia.Services;

namespace TodoApp.Avalonia.ViewModels;

public partial class TodoItemViewModel(TodoListViewModel parent, AppDbContext context) : ViewModelBase
{
    private TodoItem? _todoItem;

    /// <summary>
    /// Creates a new ToDoItemViewModel for the given <see cref="Database.TodoItem"/>
    /// </summary>
    /// <param name="item">The item to load</param>
    /// <param name="parent">The <see cref="TodoListViewModel"/> which is the parent</param>
    /// <param name="context">The <see cref="AppDbContext"/> to use</param>
    public TodoItemViewModel(TodoItem item, TodoListViewModel parent, AppDbContext context) : this(parent, context)
    {
        // Init the properties with the given values
        this._isChecked = item.IsComplete;
        this._content = item.Title;

        this._todoItem = item;
    }

    /// <summary>
    /// Gets or sets the checked status of each item
    /// </summary>
    // NOTE: This property is made without source generator. Uncomment the line below to use the source generator
    // [ObservableProperty] 
    private bool _isChecked;

    public bool IsChecked
    {
        get { return _isChecked; }
        set
        {
            UpdateIsChecked(value); 
        }
    }

    private async void UpdateIsChecked(bool value)
    {
        await UpdateIsCheckedAsync(value);
    }
    
    private async Task UpdateIsCheckedAsync(bool newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? storedItem = await context.TodoItems.FindAsync([GetToDoItem().Id], cancellationToken);
            
            if (storedItem is not null)
            {
                storedItem.IsComplete = newValue;

                // Store the updated item in the database
                _ = context.TodoItems.Update(storedItem);
                _ = await context.SaveChangesAsync(cancellationToken);

                parent.ShowInfoAlert("Saved changes successfully");
                SetProperty(ref this._isChecked, storedItem.IsComplete);
            }
            else
            {
                parent.ShowErrorAlert("Item not found");
                OnPropertyChanged(nameof(IsChecked)); // Notify the UI about the current value 
            }
        }
        catch (Exception ex)
        {
            parent.ShowErrorAlert(ex.Message);
            OnPropertyChanged(nameof(IsChecked)); // Notify the UI about the current value 
        }
    }
    

    /// <summary>
    /// Gets or sets the content of the to-do item
    /// </summary>
    [ObservableProperty] private string? _content;

    /// <summary>
    /// Gets a ToDoItem of this ViewModel
    /// </summary>
    /// <returns>The ToDoItem</returns>
    public TodoItem GetToDoItem()
    {
        if (this._todoItem is not null)
        {
            this._todoItem.Title = Content;
            return this._todoItem;
        }

        // if no todoItem model is found, return a new one
        return new TodoItem() { IsComplete = this.IsChecked, Title = this.Content };
    }
}