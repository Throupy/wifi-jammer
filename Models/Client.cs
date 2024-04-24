using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JammerV1.Models
{
    public class Client : INotifyPropertyChanged
    {
        public string JamButtonColor => IsJammed ? "#f23f42" : "#2ec04f"; // Red or Green
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
        public string StationMAC { get; set; }
        public DateTime FirstTimeSeen { get; set; }
        public DateTime LastTimeSeen { get; set; }
        public string Power { get; set; }
        public string Packets { get; set; }
        public string BSSID { get; set; }
        public string ProbedESSIDs { get; set; }

        // Event declared in INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;

        // Method to raise the PropertyChanged event
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
