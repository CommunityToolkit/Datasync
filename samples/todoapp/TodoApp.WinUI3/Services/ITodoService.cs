// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.WinUI3.Services;

/// <summary>
/// A data service for managing the TodoItem entities in a data store.
/// </summary>
public interface ITodoService : IDataService<TodoItem>;
