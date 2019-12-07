using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using DirectStar.Model;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DirectStar.ViewModel
{
    class MainVM : DependencyObject
    {
        private static MainVM instanse;

        public string RecvPath
        {
            get { return (string)GetValue(RecvPathProperty); }
            set
            {
                SetValue(RecvPathProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for .  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RecvPathProperty =
            DependencyProperty.Register("RecvPath", typeof(string), typeof(MainVM), new PropertyMetadata(""));

        public string TransmPath
        {
            get { return (string)GetValue(TransmPathProperty); }
            set
            {
                SetValue(TransmPathProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for TransmPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TransmPathProperty =
            DependencyProperty.Register("TransmPath", typeof(string), typeof(MainVM), new PropertyMetadata(""));

        public Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> RecvLog
        {
            get { return (Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>)GetValue(RecvLogProperty); }
            set { SetValue(RecvLogProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DirGrid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RecvLogProperty =
            DependencyProperty.Register("RecvLog", typeof(Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>), typeof(MainVM), new PropertyMetadata(null));


        public ObservableCollection<string> DirContentDirs
        {
            get { return (ObservableCollection<string>)GetValue(DirContentDirsProperty); }
            set { SetValue(DirContentDirsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TransmDir.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirContentDirsProperty =
            DependencyProperty.Register("DirContentDirs", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));


        public ObservableCollection<string> DirContentFiles
        {
            get { return (ObservableCollection<string>)GetValue(DirContentFilesProperty); }
            set { SetValue(DirContentFilesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DirContentFiles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirContentFilesProperty =
            DependencyProperty.Register("DirContentFiles", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));



        public Dictionary<string, TransferTaskContainer> TransferTaskLog
        {
            get { return (Dictionary<string, TransferTaskContainer>)GetValue(TransferTaskLogProperty); }
            set { SetValue(TransferTaskLogProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TransferTaskLog.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TransferTaskLogProperty =
            DependencyProperty.Register("TransferTaskLog", typeof(Dictionary<string, TransferTaskContainer>), typeof(MainVM), new PropertyMetadata(null));


        public ObservableCollection<string> Devices
        {
            get { return (ObservableCollection<string>)GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Devices.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DevicesProperty =
            DependencyProperty.Register("Devices", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));



        public ObservableCollection<string> Favorites
        {
            get { return (ObservableCollection<string>)GetValue(FavoritesProperty); }
            set { SetValue(FavoritesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Favorites.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FavoritesProperty =
            DependencyProperty.Register("Favorites", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));



        public ObservableCollection<string> Linked
        {
            get { return (ObservableCollection<string>)GetValue(LinkedProperty); }
            set { SetValue(LinkedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Linked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkedProperty =
            DependencyProperty.Register("Linked", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));


        public ObservableCollection<string> LinkRequests
        {
            get { return (ObservableCollection<string>)GetValue(LinkRequestsProperty); }
            set { SetValue(LinkRequestsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkRequests.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkRequestsProperty =
            DependencyProperty.Register("LinkRequests", typeof(ObservableCollection<string>), typeof(MainVM), new PropertyMetadata(null));




        public ObservableCollection<TransferTaskContainer> transferTasks
        {
            get { return (ObservableCollection<TransferTaskContainer>)GetValue(transferTasksProperty); }
            set { SetValue(transferTasksProperty, value); }
        }

        // Using a DependencyProperty as the backing store for transferTasks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty transferTasksProperty =
            DependencyProperty.Register("transferTasks", typeof(ObservableCollection<TransferTaskContainer>), typeof(MainVM), new PropertyMetadata(null));

        public void AddToTasks(TransferTaskContainer transferTask)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                transferTasks.Add(transferTask);
            }));
        }


        public void AddToGroup(Group group, string adj_name)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                Add(group, ref adj_name);
            }));
        }

        public void RemoveFromGroup(Group group, string adj_name)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                Remove(group, ref adj_name);
            }));
        }

        public void AddListMembers(List<MemberInfoContainer> members_list)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                foreach (MemberInfoContainer member in members_list)
                {
                    string adjname = member.AdjustedName;
                    Add(member.Group, ref adjname);
                }
            }
            ));
        }

        public void ChangeDeviceGroup(string adj_name, Group old_group, Group new_group)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                Remove(old_group, ref adj_name);
                Add(new_group, ref adj_name);
            }));
        }

        private void Remove(Group old_group, ref string adj_name)
        {
            switch (old_group)
            {
                case Group.Favorite:
                    {
                        Favorites.Remove(adj_name);
                        //SetValue(FavoritesProperty, Favorites);
                        break;
                    }
                case Group.Linked:
                    {
                        Linked.Remove(adj_name);
                        //SetValue(LinkedProperty, Linked);
                        break;
                    }
                case Group.RequestReceived:
                    {
                        LinkRequests.Remove(adj_name);

                        // SetValue(LinkRequestsProperty, LinkRequests);
                        break;
                    }
                default:
                    {
                        Devices.Remove(adj_name);
                        // SetValue(DevicesProperty, Devices);
                        break;
                    }
            }
        }

        private void Add(Group group, ref string adj_name)
        {
            switch (group)
            {
                case Group.Favorite:
                    {
                        if (!Favorites.Contains(adj_name))
                            Favorites.Add(adj_name);
                        
                        break;
                    }
                case Group.Linked:
                    {
                        if (!Linked.Contains(adj_name))
                            Linked.Add(adj_name);
                        
                        break;
                    }
                case Group.RequestReceived:
                    {
                        if (!LinkRequests.Contains(adj_name))
                            LinkRequests.Add(adj_name);
                        
                        break;
                    }
                default:
                    {
                        if (!Devices.Contains(adj_name))
                            Devices.Add(adj_name);
                        
                        break;
                    }
            }
        }

        private MainVM()
        {
            Devices = new ObservableCollection<string>();
            LinkRequests = new ObservableCollection<string>();
            Linked = new ObservableCollection<string>();
            Favorites = new ObservableCollection<string>();
        }

        public static MainVM GetInstance
        {
            get
            {
                return instanse ?? (instanse = new MainVM());
            }
        }
    }
}
