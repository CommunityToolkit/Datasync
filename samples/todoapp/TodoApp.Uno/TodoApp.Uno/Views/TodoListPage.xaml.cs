using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TodoApp.Uno.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TodoApp.Uno.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TodoListPage : Page
{
    public TodoListViewModel? ViewModel => DataContext as TodoListViewModel;

    public TodoListPage()
    {
        this.InitializeComponent();


        Loaded += TodoListPage_Loaded;
    }

    // on loaded override
    private async void TodoListPage_Loaded(object sender, RoutedEventArgs e)
    {
        //if (ViewModel.LoadPageCommand.CanExecute(null))
        //{
        //    await ViewModel.LoadPageCommand.ExecuteAsync(null);
        //}
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (ViewModel.AddItemCommand.CanExecute(null))
            {
                ViewModel.AddItemCommand.Execute(null);
            }
            e.Handled = true; // Prevents further processing of the Enter key
        }
    }
}
