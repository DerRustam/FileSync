using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using DirectStar.ViewModel;
using DirectStar.Model;

namespace DirectStar.Controllers
{
    class MemberInteractionC
    {
        private static MemberInteractionC _instance;
        private BlockingCollection<NetworkDeviceContainer> networkDevices;
        
        private ConcurrentDictionary<string, MemberInfoContainer> members;
        private ConcurrentDictionary<string, string> abandoned;


        public ConcurrentDictionary<string, MemberInfoContainer> Members
        {
            get { return members; }
        }

        public ConcurrentDictionary<string, string> Abandoned
        {
            get { return abandoned; }
        }

        private MemberInteractionC()
        {
            networkDevices = new BlockingCollection<NetworkDeviceContainer>();
            FileSystemC fs = FileSystemC.GetInstance();
            
            members = fs.GetMembersJSON();
            abandoned = fs.GetAbandonedJSON();
            MainVM vm = MainVM.GetInstance;
            vm.AddListMembers(members.Values.ToList());
            
            
        }

        public bool IsFilesOnSend(ref string usermachine, out TransferTaskContainer transfers)
        {
            transfers = null;
            return IsMemberAllowedTransfer(ref usermachine) && FileTransferC.GetInstance().IsTransferExists(usermachine, out transfers);
        }

        public void SendRequestLinkUp(string usermachine)
        {
            Task.Run(new Action(() =>
            {
                AddMember(ref usermachine, Group.RequestSended);
                if (!SendRequest(ref usermachine, Properties.Resources.msg_request_send))
                {
                    abandoned.TryAdd(usermachine, Properties.Resources.msg_request_send);
                }
            }));
            
        }

        public void SendRequestAcceptLink(string adj_name)
        {
            Task.Run(new Action(() =>
            {
                MemberInfoContainer member = FindAdjMember(ref adj_name);
                if (member != null)
                {
                    string prop_name = member.ProperName;
                    ChangeAdjMemGroup(ref adj_name, Group.Linked);
                    if (!SendRequest(ref adj_name, Properties.Resources.msg_request_accept))
                    {
                        abandoned.TryAdd(prop_name, Properties.Resources.msg_request_accept);
                    }
                }
            }));
        }

        public void SendStopLinking(string adj_name)
        {
            Task.Run(new Action(() =>
            {
                MemberInfoContainer member = FindAdjMember(ref adj_name);
                if (member != null)
                {
                    string prop_name = member.ProperName;
                    RemoveAdjMember(ref adj_name);
                    if (!SendRequest(ref adj_name, Properties.Resources.msg_link_stop))
                    {
                        abandoned.TryAdd(prop_name, Properties.Resources.msg_link_stop);
                    }
                    else
                    {
                        MainVM.GetInstance.AddToGroup(Group.None, prop_name);
                    }
                }
            }));
        }

        private bool SendRequest(ref string name, string message)
        {
            MemberInfoContainer member = FindAdjMember(ref name);
            if (member != null)
            {
                if (member.LocalAddress != null)
                {
                    return TransmissionC.GetInstance().SendMessageToUser(message, member.LocalAddress);
                }
            }
            else
            {
                NetworkDeviceContainer device = FindDevice(ref name);
                if (device != null)
                {
                    return TransmissionC.GetInstance().SendMessageToUser(message, device.LocalAddress);
                }
            }
            return false;
        }

        public void ProcessNewUser(string usermachine, System.Net.IPAddress address)
        {
            MemberInfoContainer member = FindMember(ref usermachine);
            if (member == null)
            {
                UpdateDeviceCollection(usermachine, address);
            }
            else
            {
                member.LocalAddress = address;
            }
        }

        public void ChangeAdjMemGroup(ref string adj_name, Group new_group)
        {
            MemberInfoContainer member = FindAdjMember(ref adj_name);
            if (member != null)
            {
                MainVM.GetInstance.ChangeDeviceGroup(adj_name, member.Group, new_group);
                member.Group = new_group;
                
            }
        }

        public void ChangeMemberGroup(ref string usermachine, Group new_group, System.Net.IPAddress address)
        {
            MemberInfoContainer member = FindMember(ref usermachine);
            if (member != null && ( new_group != Group.RequestSended || (member.Group == Group.RequestSended && new_group == Group.Linked)))
            {
                MainVM.GetInstance.ChangeDeviceGroup(member.AdjustedName, member.Group, new_group);
                member.Group = new_group;
                if (address != null)
                {
                    member.LocalAddress = address;
                }
            }
        }

        public void AddMember(ref string name, Group group)
        {
            MemberInfoContainer member = FindMember(ref name);
            if (member != null)
            {
                ChangeMemberGroup(ref name, Group.RequestReceived, null);
            }
            NetworkDeviceContainer device = FindDevice(ref name);
            if (device != null && (group == Group.RequestSended || group == Group.RequestReceived))
            {
                if (members.TryGetValue(name, out MemberInfoContainer val))
                {
                    MainVM.GetInstance.ChangeDeviceGroup(name, val.Group, group);
                    val.LocalAddress = device.LocalAddress;
                    val.Group = group;
                }
                else
                {
                    members.TryAdd(name, new MemberInfoContainer(name, name, group, device.LocalAddress));
                    MainVM.GetInstance.ChangeDeviceGroup(name, Group.None, group);
                }
                
            }
        }

        public void RemoveAdjMember(ref string adj_name)
        {
            MemberInfoContainer member = FindAdjMember(ref adj_name);
            if (member != null && members.TryRemove(member.ProperName, out MemberInfoContainer val))
            {
                UpdateDeviceCollection(val.ProperName, val.LocalAddress);
                MainVM.GetInstance.RemoveFromGroup(val.Group, val.AdjustedName);
            }
        }

        public void RemoveMember(ref string usermachine)
        {
            if (members.TryRemove(usermachine, out MemberInfoContainer val))
            {
                MainVM.GetInstance.ChangeDeviceGroup(val.AdjustedName, val.Group, Group.None);
                
            }
        }

        private NetworkDeviceContainer FindDevice(ref string usermachine)
        {
            foreach (var registered in networkDevices)
            {
                if (registered.DeviceUserName.Equals(usermachine))
                {
                    return registered;
                }
            }
            return null;
        }

        private void UpdateDeviceCollection(string usermachine, System.Net.IPAddress ip_addr)
        {
            NetworkDeviceContainer device = FindDevice(ref usermachine);
            if (device != null)
            {
                device.LocalAddress = ip_addr;
            }
            else
            {
                networkDevices.Add(new NetworkDeviceContainer(usermachine, ip_addr));
                MainVM vm = MainVM.GetInstance;
                vm.AddToGroup(Group.None, usermachine);
            }
        }

        public bool RenameMember(string adj_old, string adj_new)
        {
            var member = FindAdjMember(ref adj_old);
            if (member != null && FindAdjMember( ref adj_new) == null)
            {
                member.AdjustedName = adj_new;
                return true;
            }
            return false;
        }

        public bool IsMemberAllowedTransfer(ref string username)
        {
            MemberInfoContainer member = FindMember(ref username);
            return (member != null) && (member.Group == Group.Favorite || member.Group == Group.Linked);
        }

        private MemberInfoContainer FindMember(ref string username)
        {
            foreach (var member in members)
            {
                if (member.Key.Equals(username))
                {
                    return member.Value;
                }
            }
            return null;
        }

        private MemberInfoContainer FindAdjMember(ref string username)
        {
            foreach (var member in members.Values)
            {
                if (member.AdjustedName.Equals(username))
                {
                    return member;
                }
            }
            return null;
        }

        public bool TryGetPropName(string adj_name, out string prop_name)
        {
            prop_name = null;
            foreach (var member in members.Values)
            {
                if (member.AdjustedName.Equals(adj_name))
                {
                    prop_name = member.ProperName;
                    return true;
                }
            }
            return false;
        }

        public bool IsFindAbandonedMessage(ref string username,out string message)
        {
            message = "";
            return abandoned.TryGetValue(username, out message);
        }

        private System.Net.IPAddress TryGetIP(string usermachine)
        {
            foreach (var registered in networkDevices)
            {
                if (registered.DeviceUserName.Equals(usermachine))
                {
                    return registered.LocalAddress;
                }
            }
            return null;
        }

        public System.Net.IPAddress TryGetMemberIP(string usermachine)
        {
            foreach (var member in members)
            {
                if (member.Key.Equals(usermachine))
                {
                    return member.Value.LocalAddress;
                }
            }
            return null;
        }

        public static void Init()
        {
            if (_instance == null)
            {
                _instance = new MemberInteractionC();
            }
        }

        public static MemberInteractionC GetInstance()
        {
            return _instance ?? (_instance = new MemberInteractionC());
        }
    }
}
