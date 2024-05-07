using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IOuiLookupService
{
    string GetVendorByMacAddress(string macAddress);
    void LoadOuiDictionary();    
}
