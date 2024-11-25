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
        this._IsComplete = item.IsComplete;
        this._content = item.Content;

        this._todoItem = item;
    }

    // NOTE: This property is made without source generator. Uncomment the line below to use the source generator
    // [ObservableProperty] 
    private bool _IsComplete;
    
    /// <summary>
    /// Gets or sets the checked status of each item
    /// </summary>
    public bool IsComplete
    {
        get { return this._IsComplete; }
        set
        {
            // Stpre the old value in order to undo the changes, if the save operation failed
            bool oldValue = this._IsComplete;
            
            if (SetProperty(ref this._IsComplete, value))
            {
                // save the item in case the have an updated value
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
        TodoItem? storedItem = null;
        try
        {
            // lookup the stored item
            storedItem = await context.TodoItems.FindAsync([GetToDoItem().Id], cancellationToken);

            // this is just to show how errors are handled. Feel free to comment it.
            if (++this.updateCounter % 3 == 0)
            {
                await Task.Delay(500);
                throw new IOException("Unable to save the item a third time. Please try again.");
            }
            
            if (storedItem is not null)
            {
                // update the stored item
                storedItem.IsComplete = newValue;

                // Store the updated item in the database
                _ = context.TodoItems.Update(storedItem);
                _ = await context.SaveChangesAsync(cancellationToken);

                // Show an info to the user
                parent.ShowInfoAlert("Saved changes successfully");
            }
            else
            {
                // If no item was found, throw an exection
                throw new NullReferenceException("Item not found");
            }
        }
        catch (Exception ex)
        {
            // Set the Property back to it's old value
            SetProperty(ref this._IsComplete, oldValue, nameof(IsComplete)); 
            if (storedItem is not null) storedItem.IsComplete = oldValue;
            parent.ShowErrorAlert(ex.Message);
        }
    }
    

    /// <summary>
    /// Gets or sets the content of the to-do item
    /// </summary>
    [ObservableProperty] private string? _content;

    /// <summary>
    /// Gets a ToDoItem for this Item-ViewModel
    /// </summary>
    /// <returns>The ToDoItem</returns>
    public TodoItem GetToDoItem()
    {
        if (this._todoItem is not null)
        {
            this._todoItem.Content = Content ?? string.Empty;
            this._todoItem.IsComplete = IsComplete;
            return this._todoItem;
        }

        // if no todoItem model is found, return a new one
        return new TodoItem()
        {
            IsComplete = this.IsComplete, 
            Content = this.Content ?? string.Empty
        };
    }
}