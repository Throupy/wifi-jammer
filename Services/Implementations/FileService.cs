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

public class FileService : IFileService {
    public ObservableCollection<AP> ParseCSV() {
        ObservableCollection<AP> AccessPoints = new ObservableCollection<AP>();
        List<Client> AllClients = new List<Client>();
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
        return AccessPoints;
    }

    public void CleanupJammerFiles() {
        var command = Cli.Wrap("/bin/bash")
            .WithArguments("-c \"rm -f /home/kali/Desktop/JammerV1/jammer-0*\"")
            .ExecuteAsync();
    }
}