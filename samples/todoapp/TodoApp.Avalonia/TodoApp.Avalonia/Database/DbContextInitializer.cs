﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TodoApp.Avalonia.ViewModels;

namespace TodoApp.Avalonia.Database;

/// <summary>
/// Use this class to initialize the database. In this sample, we just create
/// the database using <see cref="DatabaseFacade.EnsureCreated"/>. However, you
/// may want to use migrations.
/// </summary>
/// <param name="context">The <see cref="AppDbContext"/> for the database.</param>
public class DbContextInitializer(AppDbContext context) : IDbInitializer
{
    /// <inheritdoc />
    public void Initialize()
    {
        _ = context.Database.EnsureCreated();
        Task.Run(async () => await context.SynchronizeAsync());
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);
        
        await context.AddSampleDataAsync(cancellationToken);
        
        // Make sure the ViewModel has fetched the latest changes and is up to date
        TodoListViewModel todoListViewModel = App.GetRequiredService<TodoListViewModel>();
        await todoListViewModel.RefreshItemsAsync(cancellationToken);
    }
}