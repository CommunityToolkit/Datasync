using ClientApp.Models;
using ClientApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TodoListViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        this.viewModel = App.GetRequiredService<TodoListViewModel>();
        DataContext = this.viewModel;
    }

    protected void ListViewItem_DoubleClickEventHandler(object sender, MouseButtonEventArgs e)
    {
        TodoItem? item = ((ListViewItem)sender).Content as TodoItem;
        this.viewModel.UpdateItemCommand.Execute(item);
    }
}