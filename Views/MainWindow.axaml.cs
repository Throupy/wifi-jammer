using System.Security.Principal;
using Avalonia.Controls;
using Avalonia.Interactivity;
using JammerV1.Models;
using JammerV1.ViewModels;

namespace JammerV1.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //this.Width = 800;
        //this.Height = 480;
        this.WindowState = WindowState.FullScreen;
        //DataContext = new MainWindowViewModel();
    }

    private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}