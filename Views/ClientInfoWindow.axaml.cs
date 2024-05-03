using System.Security.Principal;
using Avalonia.Controls;
using Avalonia.Interactivity;
using JammerV1.Models;
using Avalonia.Markup.Xaml;
using JammerV1.ViewModels;

namespace JammerV1.Views;

public partial class ClientInfoWindow : Window
{
    public ClientInfoWindow()
    {
        InitializeComponent();
        this.Width = 300;
        this.Height = 280;
        //this.WindowState = WindowState.FullScreen;
        //DataContext = new MainWindowViewModel();
    }

    public void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}