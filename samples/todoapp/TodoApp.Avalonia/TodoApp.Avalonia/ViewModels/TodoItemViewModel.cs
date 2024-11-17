// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
        get { return this._isChecked; }
        set
        {
            var oldValue = this._isChecked;
            if (SetProperty(ref this._isChecked, value))
            {
                SaveIsChecked(value, oldValue); 
            }
        }
    }
    
    private async void SaveIsChecked(bool value, bool oldValue)
    {
        await SaveIsCheckedAsync(value, oldValue);
    }
    
    // a counter that can be used to track save requests. Only used to demonstrate an exception after every third 
    // save operation. 
    int updateCounter = 0; 
    
    private async Task SaveIsCheckedAsync(bool newValue, bool oldValue, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? storedItem = await context.TodoItems.FindAsync([GetToDoItem().Id], cancellationToken);

            if (++this.updateCounter % 3 == 0)
            {
                throw new IOException("Unable to save the item a third time. Please try again.");
            }
            
            if (storedItem is not null)
            {
                storedItem.IsComplete = newValue;

                // Store the updated item in the database
                _ = context.TodoItems.Update(storedItem);
                _ = await context.SaveChangesAsync(cancellationToken);

                parent.ShowInfoAlert("Saved changes successfully");
            }
            else
            {
                throw new Exception("Item not found");
            }
        }
        catch (Exception ex)
        {
            SetProperty(ref this._isChecked, oldValue, nameof(IsChecked)); // Notify the UI about the current value 
            parent.ShowErrorAlert(ex.Message);
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