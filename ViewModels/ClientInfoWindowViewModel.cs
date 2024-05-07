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

        public Client client {
            get => _client;
            set {
                _client = value;
                OnPropertyChanged(nameof(client));
            }
        }

        public ClientInfoWindowViewModel(Client p_client)
        {
            client = p_client;
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private void ExecuteClose(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}