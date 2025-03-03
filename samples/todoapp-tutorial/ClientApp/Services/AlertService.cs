using ClientApp.Interfaces;
using System.Windows;

namespace ClientApp.Services;

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