// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Avalonia.Database;

namespace TodoApp.Avalonia.ViewModels;
#if DEBUG
public static class DesingTimeViewModels
{
    
    public static TodoListViewModel TodoListDesignerViewModel =>
        (Application.Current as App)?.Services.GetRequiredService<TodoListViewModel>() ?? throw new InvalidOperationException();
}
#endif
