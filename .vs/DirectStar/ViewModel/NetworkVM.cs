using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DirectStar.ViewModel
{
    class NetworkVM : DependencyObject
    {
        private static NetworkVM _instance;

        public ObservableCollection<NetworkInterface> NetworkInterfaces
        {
            get { return (ObservableCollection<NetworkInterface>)GetValue(NetworkInterfacesProperty); }
            set { SetValue(NetworkInterfacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NetworkInterfaces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NetworkInterfacesProperty =
            DependencyProperty.Register("NetworkInterfaces", typeof(ObservableCollection<NetworkInterface>), typeof(NetworkVM), new PropertyMetadata(null));


        public string PortStatus
        {
            get { return (string)GetValue(PortStatusProperty); }
            set { SetValue(PortStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PortStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PortStatusProperty =
            DependencyProperty.Register("PortStatus", typeof(string), typeof(NetworkVM), new PropertyMetadata(""));


        public string LANStatus
        {
            get { return (string)GetValue(LANStatusProperty); }
            set { SetValue(LANStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLANConnected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LANStatusProperty =
            DependencyProperty.Register("LANStatus", typeof(string), typeof(NetworkVM), new PropertyMetadata(""));

        public string InetStatus
        {
            get { return (string)GetValue(InetStatusProperty); }
            set { SetValue(InetStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InetStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InetStatusProperty =
            DependencyProperty.Register("InetStatus", typeof(string), typeof(NetworkVM), new PropertyMetadata(""));

        private NetworkVM()
        {

        }

        public static NetworkVM GetInstance
        {
            get
            {
                return _instance ?? (_instance = new NetworkVM());
            }
        }
    }
}
