using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using DirectStar.Controllers;
using DirectStar.ViewModel;
using DirectStar.Views;

namespace DirectStar
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ContextMenu items1_cm;
        private ContextMenu items2_cm;
        private ContextMenu items3_cm;
        private FileSystemC fs;
        private HashSet<ListViewItem> selectedFiles;
        private SortedSet<string> selected_names;
        private Label selected_l;
        private TextBox edit_tb;
        private bool focused_dir;
        private List<FileFormat> filteredformats;
        private bool img_filer_selected;
        private bool mus_filer_selected;
        private bool vid_filer_selected;
        public MainWindow()
        {
            InitializeComponent();
            filteredformats = new List<FileFormat>();
            img_filer_selected = mus_filer_selected = vid_filer_selected = false;
            focused_dir = true;
            MemberInteractionC.Init();
            fs = FileSystemC.GetInstance();
            FileTransferC.Init();
            TransmissionC.InitService();
            Monitors.NetworkMonitor.InitService();
            edit_tb = new TextBox();
            selected_names = new SortedSet<string>();
            selectedFiles = new HashSet<ListViewItem>();
        }

        private void main_window_Loaded(object sender, RoutedEventArgs e)
        {
            directoryBack_b.IsEnabled = !fs.IsTransmPathRoot();
            items1_cm = FindResource("items1_cm") as ContextMenu;
            items2_cm = FindResource("items2_cm") as ContextMenu;
            items3_cm = FindResource("items3_cm") as ContextMenu;
        }

        private void recv_ch_dir_b_Click(object sender, RoutedEventArgs e)
        {
            fs.OpenDirectoryDialog(true);
        }

        private void transm_ch_dir_b_Click(object sender, RoutedEventArgs e)
        {
            fs.OpenDirectoryDialog(false);
            directoryBack_b.IsEnabled = !fs.IsTransmPathRoot();
            ClearSelectedFiles();
        }

        private void TransmFile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem sp = sender as ListViewItem;
            ClearSelectedFiles();
            sp.Background = Brushes.LightBlue;
            selectedFiles.Add(sp);
            selected_names.Add(sp.Content.ToString());
        }

        private void ClearSelectedFiles()
        {
            foreach(ListViewItem s in selectedFiles)
            {
                s.Background = Brushes.White;
            }
            selectedFiles.Clear();
            selected_names.Clear();
        }

        private void TransmFile_MouseEnter(object sender, MouseEventArgs e)
        {
            ListViewItem sp = sender as ListViewItem;
            if (e.LeftButton == MouseButtonState.Pressed && !selectedFiles.Contains(sp))
            {
                sp.Background = Brushes.LightBlue;
                selectedFiles.Add(sp);
                selected_names.Add(sp.Content.ToString());
            }
        }

        private void recv_open_explorer_b_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(fs.RecvPath);
            }
            catch { }
        }

        private void Network_Menu_Item_Click(object sender, RoutedEventArgs e)
        {
            NetworkView nw = new NetworkView();
            nw.ShowDialog();
        }

        private void userlist_b_Click(object sender, RoutedEventArgs e)
        {
            
            Button user_b = (Button)sender;
            if (int.Parse(user_b.Tag.ToString()) == 0)
            {
                UsersList_g.Width = 200;
                user_b.Tag = 1;
            }
            else
            {
                UsersList_g.Width = 0;
                user_b.Tag = 0;
            }   
        }

        private void device_Click(object sender, MouseButtonEventArgs e)
        {

            selected_l = sender as Label;

            var p = selected_l.Parent;
            switch (int.Parse(selected_l.Tag.ToString()))
            {
                case 0:
                    {
                        ItemCollection col = items3_cm.Items;
                        (col.GetItemAt(0) as MenuItem).Header = "Переименовать";
                        MenuItem item = col.GetItemAt(1) as MenuItem;
                        item.Header = "Удалить из избранных";
                        item.Tag = 0;
                        (col.GetItemAt(2) as MenuItem).Header = "Удалить из доверенных";
                        items3_cm.PlacementTarget = selected_l;
                        items3_cm.IsOpen = true;
                        break;
                    }
                case 1:
                    {
                        ItemCollection col = items3_cm.Items;
                        (col.GetItemAt(0) as MenuItem).Header = "Переименовать";
                        MenuItem item = col.GetItemAt(1) as MenuItem;
                        item.Header = "Добавить в избранные";
                        item.Tag = 1;
                        (col.GetItemAt(2) as MenuItem).Header = "Удалить из доверенных";
                        items3_cm.PlacementTarget = selected_l;
                        items3_cm.IsOpen = true;
                        break;
                    }
                case 2:
                    {
                        ItemCollection col = items2_cm.Items;
                        (col.GetItemAt(0) as MenuItem).Header = "Подтвердить запрос";
                        (col.GetItemAt(1) as MenuItem).Header = "Отвергнуть запрос";
                        items2_cm.PlacementTarget = selected_l;
                        items2_cm.IsOpen = true;
                        break;
                    }
                case 3:
                    {
                        ItemCollection col = items1_cm.Items;
                        (col.GetItemAt(0) as MenuItem).Header = "Запросить добавление в доверенные";
                        items1_cm.PlacementTarget = selected_l;
                        items1_cm.IsOpen = true;
                        break;
                    }
            }
            
        }

        private void cm1_item1_Click(object sender, RoutedEventArgs e)
        {
           MemberInteractionC.GetInstance().SendRequestLinkUp(selected_l.Content.ToString());
        }

        private void cm2_item1_Click(object sender, RoutedEventArgs e)
        {
            MemberInteractionC.GetInstance().SendRequestAcceptLink(selected_l.Content.ToString());
        }

        private void cm2_item2_Click(object sender, RoutedEventArgs e)
        {
            string tmp = selected_l.Content.ToString();
            MemberInteractionC.GetInstance().ChangeAdjMemGroup(ref tmp, Model.Group.None);
        }

        private void cm3_item1_Click(object sender, RoutedEventArgs e)
        {
            string name;
            name = selected_l.Content.ToString();
            
        }

        private void cm3_item2_Click(object sender, RoutedEventArgs e)
        {
            string tmp;
            tmp = selected_l.Content.ToString();
            if (int.Parse((sender as MenuItem).Tag.ToString()) == 0)
            {
                MemberInteractionC.GetInstance().ChangeAdjMemGroup(ref tmp, Model.Group.Linked);
            }
            else
            {
                MemberInteractionC.GetInstance().ChangeAdjMemGroup(ref tmp, Model.Group.Favorite);
            }
        }

        private void cm3_item3_Click(object sender, RoutedEventArgs e)
        {
            MemberInteractionC.GetInstance().SendStopLinking(selected_l.Content.ToString());
        }

        private void main_window_Closed(object sender, EventArgs e)
        {
            fs.UpdateJSONData();
        }

        private void TransmFile_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedFiles.Count != 0)
            {
                MembersToSendView view = new MembersToSendView();
                List<string> selected = new List<string>();
                if (view.ShowDialog() == false)
                {
                    selected = view.Selected;
                }
                if (selected.Count != 0)
                {
                    FileTransferC tts = FileTransferC.GetInstance();
                    foreach (string adj_name in selected)
                    {
                        if (MemberInteractionC.GetInstance().TryGetPropName(adj_name, out string usermachine))
                        {
                            tts.ProvideNewTransfer(usermachine, fs.TransmPath, selected_names);
                            TransmissionC.GetInstance().TrySend(usermachine);
                        }
                    }
                }
            }
        }

        private void tasks_b_Click(object sender, RoutedEventArgs e)
        {
            Button tasks_b = (Button)sender;
            if (int.Parse(tasks_b.Tag.ToString()) == 0)
            {
                TasksLog_g.Width = 350;
                tasks_b.Tag = 1;
            }
            else
            {
                TasksLog_g.Width = 0;
                tasks_b.Tag = 0;
            }
        }

        private void directoryBack_b_Click(object sender, RoutedEventArgs e)
        {
            if (fs.ReturnBackTransmDirectory())
                directoryBack_b.IsEnabled = false;

        }

        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            if(e.Delta > 0)
            {
                for (byte i= 0; i < 5; ++i)
                {
                    sv.LineLeft();
                }
            }
            else
            {
                for (byte i = 0; i < 5; ++i)
                {
                    sv.LineRight();
                }
            }
            e.Handled = true;
        }

        private void filter_content_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((sender as TextBox).Text))
            {
                if (focused_dir)
                {
                    fs.FilterDirs(filter_dir_tb.Text);
                }
                else
                {
                    fs.FilterFiles(filter_files_tb.Text);
                }
            }
        }

        private void Transm_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox filtertb = focused_dir ? filter_dir_tb : filter_files_tb;
            char ch = char.ToLower((char)KeyInterop.VirtualKeyFromKey(e.Key));
            if (char.IsLetterOrDigit(ch))
            {
                filtertb.Select(0, 0);
                filtertb.Text += ch;
                filtertb.SelectAll();
            }
            else if (e.Key == Key.Back || e.Key == Key.Tab)
            {
                ClearFilter();
            }
            e.Handled = true;
        }

        private void ClearFilter()
        {
            TextBox filtertb = focused_dir ? filter_dir_tb : filter_files_tb;
            filtertb.Text = string.Empty;
            if (focused_dir)
            {
                MainVM.GetInstance.DirContentDirs = fs.TransmPathDirs;
            }
            else
            {
                MainVM.GetInstance.DirContentFiles = fs.TransmPathFiles;
            }
        }

        private void DirScrollBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!focused_dir)
                focused_dir = true;
        }

        private void Files_MouseEnter(object sender, MouseEventArgs e)
        {
            if (focused_dir)
                focused_dir = false;
        }

        private void Directory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string dir = (sender as Control).DataContext.ToString();
           fs.SelectTransmDirectory(ref dir);
            directoryBack_b.IsEnabled = !fs.IsTransmPathRoot();
        }

        private void Filter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ClearFilter();
        }

        private void img_filter_b_Click(object sender, RoutedEventArgs e)
        {
            if (img_filer_selected)
            {
                img_filter_b.Background = Brushes.White;
                filteredformats.Remove(FileFormat.Image);
                img_filer_selected = false;
            }
            else if (filteredformats.Count != 2)
            {
                img_filter_b.Background = Brushes.LightBlue;
                filteredformats.Add(FileFormat.Image);
                img_filer_selected = true;
            }
            else
            {
                ResetFormatFilters();
            }
            CheckFormatFilter();
        }

        private void mus_filter_b_Click(object sender, RoutedEventArgs e)
        {
            if (mus_filer_selected)
            {
                mus_filter_b.Background = Brushes.White;
                filteredformats.Remove(FileFormat.Music);
                mus_filer_selected = false;
            }
            else if (filteredformats.Count != 2)
            {
                mus_filter_b.Background = Brushes.LightBlue;
                filteredformats.Add(FileFormat.Music);
                mus_filer_selected = true;
            }
            else
            {
                ResetFormatFilters();
            }
            CheckFormatFilter();
        }

        private void vid_filter_b_Click(object sender, RoutedEventArgs e)
        {
            if (vid_filer_selected)
            {
                vid_filter_b.Background = Brushes.White;
                filteredformats.Remove(FileFormat.Video);
                vid_filer_selected = false;
            }
            else if (filteredformats.Count != 2)
            {
                vid_filter_b.Background = Brushes.LightBlue;
                filteredformats.Add(FileFormat.Video);
                vid_filer_selected = true;
            }
            else
            {
                ResetFormatFilters();
            }
            CheckFormatFilter();
        }

        private void ResetFormatFilters()
        {
            img_filer_selected = vid_filer_selected = mus_filer_selected = false;
            img_filter_b.Background = vid_filter_b.Background = mus_filter_b.Background = Brushes.White;
            filteredformats.Clear();
            CheckFormatFilter();
        }

        private void CheckFormatFilter()
        {
            if (filteredformats.Count != 0)
            {
                fs.FilterByFormat(filteredformats);
            }
            else
            {
                MainVM.GetInstance.DirContentFiles = fs.TransmPathFiles;
            }
        }

        private void Recv_File_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Label file_l = sender as Label;
            VirtualizingStackPanel vsp = FindParent<VirtualizingStackPanel>(file_l);
            if (vsp != null)
            {
                try
                {
                    KeyValuePair<string, System.Collections.ObjectModel.ObservableCollection<string>> pair = (KeyValuePair<string, System.Collections.ObjectModel.ObservableCollection<string>>)vsp.DataContext;
                    Process.Start(Path.Combine(pair.Key, file_l.Content.ToString()));
                }
                catch { }
            }
        }

        private T FindParent<T>(DependencyObject elem) where T: DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(elem);
            if (parent == null) return null;
            T par_elem = parent as T;
            if (par_elem != null)
            {
                return par_elem;
            }
            return FindParent<T>(parent);
        }

        private void ListViewItem_PreviewDragEnter(object sender, DragEventArgs e)
        {

        }
    }
}
