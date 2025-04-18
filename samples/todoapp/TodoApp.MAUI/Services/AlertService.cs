// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.MAUI.Services;
public interface IAlertService
{
    Task ShowErrorAlertAsync(string title, string message, string cancel = "OK");
}

public class AlertService : IAlertService
{
    public Task ShowErrorAlertAsync(string title, string message, string cancel = "OK")
        => Application.Current!.Windows[0].Page!.DisplayAlert(title, message, cancel);
}
