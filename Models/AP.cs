using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JammerV1.Models
{
    public class AP : INotifyPropertyChanged
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
