using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Microsoft.VisualBasic.FileIO;
using CliWrap;
using JammerV1.Models;

public class FileService : IFileService {
    public ObservableCollection<AP> ParseCSV() {
        ObservableCollection<AP> AccessPoints = new ObservableCollection<AP>();
        List<Client> AllClients = new List<Client>();
        try
        {
            using (TextFieldParser parser = new TextFieldParser(Constants.Paths.CleanedJammerOutputFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[]? fields = parser.ReadFields();
                    if (fields == null) throw new NullReferenceException("No devices found");
                    if (fields.Length == 15) // APs have 15 length in CSV
                    {
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
                        // After, I will go through the list and add them to their parent AP's
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
                AP? parentAp = AccessPoints.Where(x => x.BSSID.Equals(client.BSSID)).FirstOrDefault();
                // ParentAp might be null here.
                // This is because we only add APs that we can get the name of
                // Client devices have BSSIDs of APs that don't exist in our ObservableCollection
                if (parentAp != null) { 
                    parentAp.Clients.Add(client); 
                    client.ParentAP = parentAp;
                }
            }
            AccessPoints = new ObservableCollection<AP>(AccessPoints.OrderByDescending(ap => ap.Clients.Count));
        }
        catch (System.FormatException) { }
        return AccessPoints;
    }

    public void CleanupJammerFiles() {
        var command = Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"rm -f {Constants.Paths.BaseDirectory}/jammer-0*\"")
            .ExecuteAsync();
    }
}