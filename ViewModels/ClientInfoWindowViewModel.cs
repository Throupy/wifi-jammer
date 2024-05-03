using JammerV1.Models;
using JammerV1.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using SharpPcap;
using Avalonia.Controls.ApplicationLifetimes;



using JammerV1.Models;

namespace JammerV1.ViewModels
{
    public class ClientInfoWindowViewModel : INotifyPropertyChanged
    {
        private Client _client;
        public ICommand CloseCommand { get; }

        // Class name the same.. use an _ i guess!
        public Client _Client {
            get => _client;
            set {
                _client = value;
                OnPropertyChanged(nameof(_Client));
            }
        }

        public ClientInfoWindowViewModel(Client client)
        {
            _Client = client;
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private void ExecuteClose(object parameter)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}