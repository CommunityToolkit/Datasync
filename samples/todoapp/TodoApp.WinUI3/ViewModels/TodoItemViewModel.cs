// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using TodoApp.WinUI3.Database;

namespace TodoApp.WinUI3.ViewModels;

public partial class TodoItemViewModel(TodoItem todoItem) : ObservableObject
{
    public readonly TodoItem _todoItem = todoItem;

    public string Title
    {
        get => this._todoItem.Title;
        set => SetProperty(this._todoItem.Title, value, this._todoItem, (item, value) => item.Title = value);
    }

    public bool IsComplete
    {
        get => this._todoItem.IsComplete;
        set => SetProperty(this._todoItem.IsComplete, value, this._todoItem, (item, value) => item.IsComplete = value);
    }

    public string Version
    {
        get => this._todoItem.Version;
        set => SetProperty(this._todoItem.Version, value, this._todoItem, (item, value) => item.Version = value);
    }

    public string Id => this._todoItem.Id;

    public DateTimeOffset? UpdatedAt
    {
        get => this._todoItem.UpdatedAt;
        set => SetProperty(this._todoItem.UpdatedAt, value, this._todoItem, (item, value) => item.UpdatedAt = value);
    }

    public bool Deleted
    {
        get => this._todoItem.Deleted;
        set => SetProperty(this._todoItem.Deleted, value, this._todoItem, (item, value) => item.Deleted = value);
    }
}
