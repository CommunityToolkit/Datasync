// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TodoApp.WinUI3.Database;

namespace TodoApp.WinUI3.Services;

/// <summary>
/// Implementation of the <see cref="ITodoService"/> that uses a local data store.
/// </summary>
/// <param name="context">The database context to use.</param>
public class LocalTodoService(AppDbContext context) : LocalDataService<TodoItem>(context), ITodoService
{
}
