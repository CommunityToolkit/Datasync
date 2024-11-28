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

/// <summary>
/// A ViewModel used to represent a <see cref="TodoItem"/>.
/// </summary>
/// <param name="dialogBus">the <see cref="IDialogBus"/> to send notifications to the user</param>
/// <param name="context">the <see cref="AppDbContext"/> to use</param>
public partial class TodoItemViewModel(IDialogBus dialogBus, AppDbContext context) : ViewModelBase
{
    // A reference to the provided TodoItem
    private TodoItem? _todoItem;

    /// <summary>
    /// Creates a new ToDoItemViewModel for the given <see cref="Database.TodoItem"/>.
    /// </summary>
    /// <param name="item">The item to load</param>
    /// <param name="dialogBus">The <see cref="TodoListViewModel"/> which is the parent</param>
    /// <param name="context">The <see cref="AppDbContext"/> to use</param>
    public TodoItemViewModel(TodoItem item, IDialogBus dialogBus, AppDbContext context) : this(dialogBus, context)
    {
        // Init the properties with the given values.
        this._IsComplete = item.IsComplete;
        this._content = item.Content;

        this._todoItem = item;
    }

    // NOTE: This property is made without source generator. Uncomment the line below to use the source generator.
    // [ObservableProperty] 
    private bool _IsComplete;
    
    /// <summary>
    /// Gets or sets the checked status of the item.
    /// </summary>
    public bool IsComplete
    {
        get { return this._IsComplete; }
        set
        {
            // Store the old value in order to undo the changes, if the save operation failed.
            bool oldValue = this._IsComplete;
            
            if (SetProperty(ref this._IsComplete, value))
            {
                // save the item in case we have an updated value
                SaveIsChecked(value, oldValue); 
            }
        }
    }
    
    /// <summary>
    /// Saves the new <see cref="IsComplete"/> value to the database
    /// </summary>
    /// <param name="newValue">the new value</param>
    /// <param name="oldValue">the old value</param>
    private async void SaveIsChecked(bool newValue, bool oldValue)
    {
        await SaveIsCheckedAsync(newValue, oldValue);
    }
    
    // a counter that can be used to track save requests.
    // Only used to demonstrate an exception after every third save operation. 
    int updateCounter = 0; 
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="newValue">the new value</param>
    /// <param name="oldValue">the old value</param>
    /// <param name="cancellationToken">the <see cref="CancellationToken"/> to use</param>
    /// <exception cref="IOException">If the item wasn't saved correctly</exception>
    /// <exception cref="NullReferenceException">If the item to update wasn't found in the database.</exception>
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
                dialogBus.ShowInfoAlert("Saved changes successfully");
            }
            else
            {
                // If no item was found, throw an exection
                throw new NullReferenceException("Item not found");
            }
        }
        catch (Exception ex)
        {
            // Set the Property back to it's old value in case of any exception.
            SetProperty(ref this._IsComplete, oldValue, nameof(IsComplete)); 
            if (storedItem is not null) storedItem.IsComplete = oldValue;
            dialogBus.ShowErrorAlert(ex.Message);
        }
    }
    
    /// <summary>
    /// Gets or sets the content of the to-do item
    /// </summary>
    [ObservableProperty] private string? _content;

    /// <summary>
    /// Gets a <see cref="TodoItem"/> for this Item-ViewModel
    /// </summary>
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