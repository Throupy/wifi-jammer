using JammerV1.Models;
using JammerV1.Commands;
using JammerV1.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using SharpPcap;
using Avalonia.Controls.ApplicationLifetimes;

namespace JammerV1.ViewModels;

public class Sample2Model {
    public Sample2Model(int number) {
        Number = number;
    }
    public int Number { get; set; }
}

public class MainWindowViewModel : INotifyPropertyChanged
{
    // Constants
    private const string Green = "#2ec04f";
    private const string Red = "#f23f42";

    // Services
    private readonly IDeviceService _deviceService;

    // Private Variables
    private ObservableCollection<AP> _accessPoints;
    private bool _isScanning;
    private AP _selectedAP;


    // Additional Variables
    public bool IsNotScanning => !IsScanning;
    public bool IsAPSelected => SelectedAP != null;
    public bool IsDeviceConnected => _deviceService.CaptureDevice != null;
    public bool IsDeviceNotConnected => !IsDeviceConnected;

    // Commands
    public ICommand ScanCommand { get; }
    public ICommand ToggleJammingCommand { get; }
    public ICommand FindDeviceCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ViewClientInfoCommand { get; }

    // Getters and Setters
    public ObservableCollection<AP> AccessPoints
    {
        get => _accessPoints;
        set
        {
            if (_accessPoints != value)
            {
                _accessPoints = value;
                OnPropertyChanged(nameof(AccessPoints));
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            _isScanning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsScanning));
        }
    }

    public AP SelectedAP
    {
        get => _selectedAP;
        set
        {
            if (_selectedAP != value)
            {
                _selectedAP = value;
                OnPropertyChanged(nameof(SelectedAP));
                OnPropertyChanged(nameof(IsAPSelected));
            }
        }
    }

    public async Task InitializeAsync() {
        await _deviceService.OpenDevice();
        OnPropertyChanged(nameof(IsDeviceConnected));
        OnPropertyChanged(nameof(IsDeviceNotConnected));
        Console.WriteLine($"Finished, device name is {_deviceService.CaptureDevice.Name}");
    }

    public MainWindowViewModel(IDeviceService deviceService)
    {
        // Register commands
        ScanCommand = new RelayCommand(o => ExecuteScan());
        ToggleJammingCommand = new RelayCommand(o => ToggleJamming(o));
        ViewClientInfoCommand = new RelayCommand(o => ViewClientInfo(o));
        CloseCommand = new RelayCommand(ExecuteClose);
        // Finish off DI for services
        _deviceService = deviceService;
        _deviceService.LoadOuiDictionary("/usr/share/ieee-data/oui.txt");
        FindDeviceCommand = new RelayCommand(o => FindDevice());
        // Open the device and update the UI.
        FindDevice();
        // Instantiate a new observable collection for holding access points
        AccessPoints = new ObservableCollection<AP>();
    }

    private void ExecuteClose(object parameter) {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.Shutdown();
        }
    }

    private async Task FindDevice() {
        await _deviceService.OpenDevice();
        OnPropertyChanged(nameof(IsDeviceConnected));
        OnPropertyChanged(nameof(IsDeviceNotConnected));
    }

    private void ViewClientInfo(object _client) {
        Client client = _client as Client;
        var clientInfoWindow = new ClientInfoWindow {
            DataContext = new ClientInfoWindowViewModel(client)
        };
        clientInfoWindow.Show();
    }

    private async void ToggleJamming(object parameter)
    {
        // Object will be passed on based on the item the button is clicked on (client, AP).
        switch (parameter)
        {
            case AP ap:
                ap.IsJammed = !ap.IsJammed;
                if (ap.IsJammed) await _deviceService.Jam(ap);
                break;
            case Client client:
                client.IsJammed = !client.IsJammed;
                if (client.IsJammed) await _deviceService.Jam(client);
                break;
        }
    }

    private async void ExecuteScan()
    {
        // This happens when the "SCAN" button is pressed
        AccessPoints.Clear();
        IsScanning = true; // Updates UI
        AccessPoints = await _deviceService.Scan(5); // Perform the scan, runs for 5 seconds.
        IsScanning = false; // Update UI again, this time it will show results from this.AccessPoints.
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}