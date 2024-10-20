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

public partial class TodoItemViewModel : ViewModelBase
{
    private TodoItem? _todoItem;

    private TodoListViewModel _Parent;

    /// <summary>
    /// Creates a new blank ToDoItemViewModel
    /// </summary>
    public TodoItemViewModel(TodoListViewModel _parent)
    {
        this._Parent = _parent;
    }

    /// <summary>
    /// Creates a new ToDoItemViewModel for the given <see cref="Models.ToDoItem"/>
    /// </summary>
    /// <param name="item">The item to load</param>
    public TodoItemViewModel(TodoItem item, TodoListViewModel _parent) : this(_parent)
    {
        // Init the properties with the given values
        IsChecked = item.IsComplete;
        Content = item.Title;

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
            if (SetProperty(ref this._isChecked, value))
            {
                _ = UpdateIsCheckedAsync(); // TODO use Task instead here 
            }
        }
    }

    private async Task UpdateIsCheckedAsync()
    {
        await this._Parent.UpdateItemAsync(this);
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