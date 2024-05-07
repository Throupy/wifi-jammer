using System.Threading.Tasks;
using JammerV1.Models;
using SharpPcap;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Microsoft.VisualBasic.FileIO;

public interface IDeviceService {
    IInjectionDevice CaptureDevice { get; }
    Task<bool> IsDeviceInMonitorMode(string deviceName);
    Task<bool> SetMonitorMode(string deviceName);
    Task OpenDevice();
    Task Jam(IJammableDevice victim);
    Task<bool> ChangeChannel(int channel);
    Task<ObservableCollection<AP>> Scan(int secondsToRun);
}