using Avalonia.Interactivity;
using Avalonia.Media;
using JammerV1.Models;
using JammerV1.Views;
using JammerV1.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Globalization;
using System.Threading;
using System;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Threading.Tasks;
using SharpPcap;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.VisualBasic;
using System.Net.WebSockets;
using System.IO;

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
    public List<Client> AllClients { get; set; } = new List<Client>();
    public bool IsAPSelected => SelectedAP != null;

    // Commands
    public ICommand JamCommand { get; }
    public ICommand ScanCommand { get; }
    public ICommand JamClientCommand { get; }

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



    public ICommand ToggleJammingCommand { get; }

    // Constructor - for now, just populate some sample data.
    public MainWindowViewModel(IDeviceService deviceService, IFileService fileService)
    {
        // Register commands
        ScanCommand = new RelayCommand(o => ExecuteScan());
        ToggleJammingCommand = new RelayCommand(o => ToggleJamming(o));

        _deviceService = deviceService;
        _fileService = fileService;

        // Instantiate a new observable collection
        AccessPoints = new ObservableCollection<AP>();
    }


    private async void ToggleJamming(object parameter)
    {
        switch (parameter)
        {
            case AP ap:
                ap.IsJammed = !ap.IsJammed;
                if (ap.IsJammed) {
                    await _deviceService.JamAP(ap);
                }
                break;
            case Client client:
                client.IsJammed = !client.IsJammed;
                if (client.IsJammed) {
                    await _deviceService.JamClient(client);
                }
                break;
        }
    }

    private async void ExecuteScan()
    {
        // TODO: Relative paths
        // This happens when the "SCAN" button is pressed
        // First need to clear previous APs and Clients (if any).
        AccessPoints.Clear();
        AllClients.Clear();
        // Begin scanning - TBH the 2000 delay is just for effect right now :D
        IsScanning = true;
        await _deviceService.Scan();
        ParseCSV();
        IsScanning = false;
    }



    private void ParseCSV()
    {
        AccessPoints = _fileService.ParseCSV();
        _fileService.CleanupJammerFiles();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}