// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TodoApp.WinUI3.Database;

namespace TodoApp.WinUI3.ViewModels;

public partial class TodoListViewModel(AppDbContext ctx) : ObservableRecipient
{
    private readonly AppDbContext context = ctx;

    [ObservableProperty]
    private ObservableCollection<TodoItem> items = [];

    // TODO: Create the rest of the view model.
}
