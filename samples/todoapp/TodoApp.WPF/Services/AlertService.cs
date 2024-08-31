// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace TodoApp.WPF.Services;

public interface IAlertService
{
    Task ShowErrorAlertAsync(string title, string message = "", string cancel = "OK");

    Task ShowInfoAlertAsync(string title, string message = "", string cancel = "OK");
}

public class AlertService : IAlertService
{
    public Task ShowErrorAlertAsync(string title, string message = "", string cancel = "OK")
    {
        _ = MessageBox.Show(
            messageBoxText: string.IsNullOrEmpty(message) ? title : message,
            caption: title,
            button: MessageBoxButton.OK,
            icon: MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task ShowInfoAlertAsync(string title, string message = "", string cancel = "OK")
    {
        _ = MessageBox.Show(
            messageBoxText: string.IsNullOrEmpty(message) ? title : message,
            caption: title,
            button: MessageBoxButton.OK,
            icon: MessageBoxImage.Information);
        return Task.CompletedTask;
    }
}
