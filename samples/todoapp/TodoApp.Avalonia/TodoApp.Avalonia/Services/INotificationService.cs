// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Avalonia.Controls.Notifications;

namespace TodoApp.Avalonia.Services;

/// <summary>
/// An interface which a View needs to implement in order to show Notifications sent from any <see cref="IDialogBus"/>
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets the NotificationManager which is used to display a NotificationMessage
    /// </summary>
    public WindowNotificationManager NotificationManager { get; }
}