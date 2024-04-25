using System.Threading.Tasks;
using JammerV1.Models;
using SharpPcap;

public interface IDeviceService {
    IInjectionDevice CaptureDevice { get; }
    Task<bool> IsDeviceInMonitorMode(string deviceName);
    Task<bool> SetMonitorMode(string deviceName);
    Task OpenDevice();
    Task Jam(IJammableDevice victim);
    Task<bool> ChangeChannel(int channel);
    Task Scan(int secondsToRun);
}