using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

public class OuiLookupService : IOuiLookupService
{
    private Dictionary<string, string> _ouiDictionary;

    public OuiLookupService()
    {
        _ouiDictionary = new Dictionary<string, string>();
    }

    public string GetVendorByMacAddress(string macAddress)
    {
        var oui = macAddress.Substring(0, 8).Replace(":", ""); // Get the first three octets of the MAC address
        if (_ouiDictionary.TryGetValue(oui, out var vendor))
        {
            return vendor;
        }
        return "Unknown Vendor";
    }

    public void LoadOuiDictionary()
    {
        var lines = File.ReadAllLines(Constants.Paths.OuiFilePath);
        foreach (var line in lines)
        {
            if (line.Contains("(base 16)")) // Common format in the file
            {
                var parts = line.Split(new[] { "(base 16)" }, StringSplitOptions.RemoveEmptyEntries);
                var oui = parts[0].Trim().Replace("-", ":");
                var vendorName = parts[1].Trim();
                _ouiDictionary[oui] = vendorName;
            }
        }
    }
}
