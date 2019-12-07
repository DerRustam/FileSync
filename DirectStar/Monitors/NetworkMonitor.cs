using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Configuration;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using DirectStar.ViewModel;
using System.Windows;
using System.Windows.Threading;
using NetFwTypeLib;


namespace DirectStar.Monitors
{
    public class NetworkStatus : EventArgs
    {
        public LANStatus Status { get; set; }
    }
    public enum LANStatus { Off, On }


    public class NetworkMonitor
    {
        public static event EventHandler<NetworkStatus> NetworkChanged;

        private static NetworkMonitor _instance;

        private HashSet<NetworkInterface> _connectedInterfaces;

        private string usermachine {get; set; }

        public HashSet<NetworkInterface> ConnectedInterfaces {
            get
            {
                return _connectedInterfaces;
            }
            set
            {
                _connectedInterfaces = value;
            }
        }

        private void GetInterfaces()
        {
            ObservableCollection<NetworkInterface> oc = new ObservableCollection<NetworkInterface>();
            HashSet<NetworkInterface> up_ifs = new HashSet<NetworkInterface>();
            bool isLanAvailible = false;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && !ni.Name.ToLower().Contains("loopback"))
                {
                    oc.Add(ni);
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        isLanAvailible = true;
                        up_ifs.Add(ni);
                    }
                }
            }
            NetworkVM vm = NetworkVM.GetInstance;
            vm.NetworkInterfaces = oc;
            ConnectedInterfaces = up_ifs;
            if (isLanAvailible)
            {
                vm.LANStatus = "Connected";
                NetworkChanged(this, new NetworkStatus { Status = LANStatus.On });
            }
            else
            {
                vm.LANStatus = "No connection";
                NetworkChanged(this, new NetworkStatus { Status = LANStatus.Off });
            }
            try
            {
                using (var wc = new WebClient())
                using (wc.OpenRead("http://clients3.google.com/generate_204"))
                {
                    vm.InetStatus = "Yes";
                }
            }
            catch { vm.InetStatus = "No"; }
            
        }


        private NetworkMonitor()
        {
            GetInterfaces();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NAC_Handler);
        }

        public void FirewallRulesCheck()
        {
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    CheckPortsOpen();
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        NetworkVM vm = NetworkVM.GetInstance;
                        vm.PortStatus = "Opened";
                    }));
                }
                catch
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        NetworkVM vm = NetworkVM.GetInstance;
                        vm.PortStatus = "Closed";
                    }));
                }
            }
            );
        } 

        private void NAC_Handler(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => GetInterfaces()));
        }

        private void CheckPortsOpen()
        {
            string[] ports = Properties.Settings.Default.fw_app_ports.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Type policytype = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 policy2 = (INetFwPolicy2)Activator.CreateInstance(policytype);
            int currentProfiles = policy2.CurrentProfileTypes;
            List<INetFwRule> RuleList = new List<INetFwRule>();
            bool isFoundIn;
            bool isFoundOut;
            string name = Properties.Settings.Default.fw_app_name;
            for (int i = 0; i < ports.Length; ++i)
            {
                isFoundIn = false;
                isFoundOut = false;
                foreach (INetFwRule rule in policy2.Rules)
                {
                    if (rule.Name.IndexOf(name) != -1 && rule.LocalPorts.IndexOf(ports[i]) != -1)
                    {
                        if (rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN)
                        {
                            isFoundIn = true;
                        }
                        else
                        {
                            isFoundOut = true;
                        }
                    }
                }
                if (!isFoundIn)
                {
                    ApplicationPortOpen(ports[i], true);
                }
                if (!isFoundOut)
                {
                    ApplicationPortOpen(ports[i], false);
                }
            }
        }
        
        private void ApplicationPortOpen(string port, bool isInput)
        {
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            rule.Direction = isInput ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN
                                     : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            rule.ApplicationName = Properties.Settings.Default.fw_app_name;
            rule.Name = Properties.Settings.Default.fw_app_name;
            rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            rule.LocalPorts = port;
            rule.Enabled = true;
            INetFwPolicy2 policy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            policy.Rules.Add(rule);
        }
        
        public static void InitService()
        {
            if (_instance == null)
            {
                _instance = new NetworkMonitor();
            }
        }

        public static NetworkMonitor GetInstance()
        {
            return _instance ?? (_instance = new NetworkMonitor());
        }
    }
}
