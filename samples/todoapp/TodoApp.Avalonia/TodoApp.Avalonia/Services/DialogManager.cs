// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace TodoApp.Avalonia.Services;

/// <summary>
/// The DialogManager is used to connect a given <see cref="IDialogBus"/> (most likely your ViewModel) to
/// a <see cref="Visual"/>.
/// </summary>
/// <remarks>
/// See also: Avalonia.Samples → <see href="https://github.com/AvaloniaUI/Avalonia.Samples?tab=readme-ov-file#view-interaction-samples" >ViewInteraction-Sample</see> 
/// </remarks>
public class DialogManager
{
    
    /// <summary>
    /// This is a dictionary which stores the mappings between the <see cref="IDialogBus"/> and <see cref="Visual"/>
    /// </summary>
    private static readonly Dictionary<IDialogBus, Visual> RegistrationMapper =
        new Dictionary<IDialogBus, Visual>();

    static DialogManager()
    {
        RegisterProperty.Changed.AddClassHandler<Visual>(RegisterChanged);
    }
    
    /// <summary>
    /// This property handles the registration of Views and ViewModel
    /// </summary>
    public static readonly AttachedProperty<IDialogBus?> RegisterProperty =
        AvaloniaProperty.RegisterAttached<DialogManager, Visual, IDialogBus?>(
            "Register");

    /// <summary>
    /// Accessor for Attached property <see cref="RegisterProperty"/>.
    /// </summary>
    public static IDialogBus? GetRegister(AvaloniaObject element)
    {
        return element.GetValue(RegisterProperty);
    }

    /// <summary>
    /// Accessor for Attached property <see cref="RegisterProperty"/>.
    /// </summary>
    public static void SetRegister(AvaloniaObject element, IDialogBus value)
    {
        element.SetValue(RegisterProperty, value);
    }

    /// <summary>
    /// Called when a new Visual is registered
    /// </summary>
    /// <exception cref="InvalidOperationException">If no Visual was provided</exception>
    private static void RegisterChanged(Visual sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is null)
        {
            throw new InvalidOperationException("The DialogManager can only be registered on a Visual");
        }

        // Unregister any old registered context
        if (e.OldValue != null)
        {
            RegistrationMapper.Remove((IDialogBus)e.OldValue);
        }

        // Register any new context
        if (e.NewValue != null)
        {
            RegistrationMapper.Add((IDialogBus)e.NewValue, sender);
        }
    }

    /// <summary>
    /// Gets the associated <see cref="Visual"/> for a given context. Returns null, if none was registered
    /// </summary>
    /// <param name="context">The context to lookup</param>
    /// <returns>The registered Visual for the context or null if none was found</returns>
    public static Visual? GetVisualForContext(IDialogBus context)
    {
        return RegistrationMapper.TryGetValue(context, out Visual? result) ? result : null;
    }

    /// <summary>
    /// Gets the parent <see cref="TopLevel"/> for the given context. Returns null, if no TopLevel was found
    /// </summary>
    /// <param name="context">The context to lookup</param>
    /// <returns>The registered TopLevel for the context or null if none was found</returns>
    public static TopLevel? GetTopLevelForContext(IDialogBus context)
    {
        return TopLevel.GetTopLevel(GetVisualForContext(context));
    }
}

/// <summary>
/// A helper class to manage dialogs via extension methods. Add more on your own
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Displays a message to the user with an error-style.
    /// </summary>
    /// <param name="context">The context to find the mapped Visual</param>
    /// <param name="message">The message to display</param>
    public static void ShowErrorAlert(this IDialogBus context, string message)
    {
        ShowAlert(context, message, NotificationType.Error);
    }

    /// <summary>
    /// Displays a message to the user with an info-style.
    /// </summary>
    /// <param name="context">The context to find the mapped Visual</param>
    /// <param name="message">The message to display</param>
    public static void ShowInfoAlert(this IDialogBus context, string message)
    {
        ShowAlert(context, message, NotificationType.Information);
    }

    /// <summary>
    /// Displays a message to the user with an success-style.
    /// </summary>
    /// <param name="context">The context to find the mapped Visual</param>
    /// <param name="message">The message to display</param>
    public static void ShowSuccessAlert(this IDialogBus context, string message)
    {
        ShowAlert(context, message, NotificationType.Success);
    }

    /// <summary>
    /// Helper method to display messages
    /// </summary>
    /// <param name="context">The context to find the mapped Visual</param>
    /// <param name="message">The message to display</param>
    /// <param name="type">The <see cref="NotificationType"/> to use</param>
    public static void ShowAlert(this IDialogBus context, string message, NotificationType type)
    {
        if (Design.IsDesignMode) return;
        INotificationService view = (INotificationService)DialogManager.GetVisualForContext(context)!;

        view.NotificationManager.Show(message, type, TimeSpan.FromSeconds(3));
    }
}