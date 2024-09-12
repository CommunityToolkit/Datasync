// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Avalonia.Database;

namespace TodoApp.Avalonia.ViewModels;

public partial class TodoItemViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new blank ToDoItemViewModel
    /// </summary>
    public TodoItemViewModel()
    {
        // empty
    }
    
    /// <summary>
    /// Creates a new ToDoItemViewModel for the given <see cref="Models.ToDoItem"/>
    /// </summary>
    /// <param name="item">The item to load</param>
    public TodoItemViewModel(TodoItem item)
    {
        // Init the properties with the given values
        IsChecked = item.IsComplete;
        Content = item.Title;
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
        set { SetProperty(ref _isChecked, value); }
    }
    
    /// <summary>
    /// Gets or sets the content of the to-do item
    /// </summary>
    [ObservableProperty] 
    private string? _content;
    
    /// <summary>
    /// Gets a ToDoItem of this ViewModel
    /// </summary>
    /// <returns>The ToDoItem</returns>
    public TodoItem GetToDoItem()
    {
        return new TodoItem()
        {
            IsComplete = this.IsChecked,
            Title = this.Content
        };
    }
}