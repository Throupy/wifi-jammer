using System.Threading.Tasks;
using JammerV1.Models;

public interface IDeviceService {
    Task JamClient(Client client);
    Task JamAP(AP ap);
    Task Scan(int secondsToRun);
}