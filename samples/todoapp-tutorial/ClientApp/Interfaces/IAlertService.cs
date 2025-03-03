namespace ClientApp.Interfaces;

/// <summary>
/// An interface for displaying alerts to the user.
/// </summary>
public interface IAlertService
{
    Task ShowErrorAlertAsync(string title, string message = "", string cancel = "OK");
    Task ShowInfoAlertAsync(string title, string message = "", string cancel = "OK");
}