using System.Collections.ObjectModel;
using System.Threading.Tasks;
using JammerV1.Models;

public interface IFileService {
    ObservableCollection<AP> ParseCSV();
    void CleanupJammerFiles();
}