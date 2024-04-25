using JammerV1.Models;
using JammerV1.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls;

namespace JammerV1.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    // Constants
    private const string Green = "#2ec04f";
    private const string Red = "#f23f42";

    // Services
    private readonly IDeviceService _deviceService;
    private readonly IFileService _fileService;


    // Private Variables
    private ObservableCollection<AP> _accessPoints;
    private bool _isScanning;
    private AP _selectedAP;


    // Additional Variables
    public bool IsNotScanning => !IsScanning;
    public bool IsAPSelected => SelectedAP != null;
    public StreamGeometry DeviceConnectionIconGeometry => _deviceService.CaptureDevice != null
        ? Application.Current.FindResource("cellular_data_1_regular") as StreamGeometry
        : Application.Current.FindResource("cellular_off_regular") as StreamGeometry;

    public string DeviceConnectionIconColour => _deviceService.CaptureDevice != null ? "Green" : "Red";


    // Commands
    public ICommand JamCommand { get; }
    public ICommand ScanCommand { get; }
    public ICommand JamClientCommand { get; }
    public ICommand ToggleJammingCommand { get; }

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

    public MainWindowViewModel(IDeviceService deviceService, IFileService fileService)
    {
        // Register commands
        ScanCommand = new RelayCommand(o => ExecuteScan());
        ToggleJammingCommand = new RelayCommand(o => ToggleJamming(o));
        // Finish of DI for services
        _deviceService = deviceService;
        // Open the device and update the UI.
        _deviceService.OpenDevice();
        OnPropertyChanged(nameof(DeviceConnectionIconGeometry));
        OnPropertyChanged(nameof(DeviceConnectionIconGeometry));
        _fileService = fileService;
        // Instantiate a new observable collection for holding access points
        AccessPoints = new ObservableCollection<AP>();
    }

    private async void ToggleJamming(object parameter)
    {
        // Object will be passed on based on the item the button is clicked on (client, AP).
        switch (parameter)
        {
            case AP ap:
                ap.IsJammed = !ap.IsJammed;
                if (ap.IsJammed) await _deviceService.JamAP(ap);
                break;
            case Client client:
                client.IsJammed = !client.IsJammed;
                if (client.IsJammed) await _deviceService.JamClient(client);
                break;
        }
    }

    private async void ExecuteScan()
    {
        // This happens when the "SCAN" button is pressed
        AccessPoints.Clear();
        IsScanning = true; // Updates UI
        await _deviceService.Scan(5); // Performs the actual scan, runs for 5 seconds.
        AccessPoints = _fileService.ParseCSV(); // Parse resulting CSV
        _fileService.CleanupJammerFiles(); // Remove jammer files
        IsScanning = false; // Update UI again, this time it will show results from this.AccessPoints.
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}