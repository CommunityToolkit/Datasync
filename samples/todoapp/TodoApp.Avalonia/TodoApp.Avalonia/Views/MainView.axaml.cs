using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using TodoApp.Avalonia.Services;

namespace TodoApp.Avalonia.Views;

public partial class MainView : UserControl, INotificationService
{
    public MainView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    public WindowNotificationManager NotificationManager => this.WindowNotificationManager;
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Configure the view to handle soft-keyboard appearance. This needs to happen after control is loaded.
        // See: https://docs.avaloniaui.net/docs/concepts/services/input-pane 
        IInputPane? inputPane = TopLevel.GetTopLevel(this)?.InputPane;
        
        if (inputPane != null)
        {
            inputPane.StateChanged += InputPaneOnStateChanged;
        }
    }

    /// <summary>
    /// This method is being called whenever an <see cref="IInputPane"/> is opened or closed
    /// </summary>
    private void InputPaneOnStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        // We need to add a Padding to the bottom where the sof InputPane will occupy our App. 
        // Note: There may be more things to consider, like different rotations and Keyboard positions. 
		double bottomPadding = e.NewState == InputPaneState.Open 
            ? Math.Max(Bounds.Height - e.EndRect.Top, 0)
            : 0; 
        
		this.Padding = new Thickness(0, 0, 0, bottomPadding); 
    }
}