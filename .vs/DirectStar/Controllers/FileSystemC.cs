using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Collections.Concurrent;
using System.Windows;
using System.Collections.ObjectModel;
using DirectStar.ViewModel;
using DirectStar.Model;
using form = System.Windows.Forms;
namespace DirectStar.Controllers
{
    public class FileSystemC
    {
        private static FileSystemC instance;
        private string recvPath;
        private string transmPath;
        private string appdata_folder;
        private RecvFilesLog _receive_log;

        public RecvFilesLog ReceiveLog
        {
            get { return _receive_log; }
        }

        public string RecvPath
        {
            get { return recvPath; }
            set
            {
                recvPath = value;
                Properties.Settings.Default.recv_path = value;
                Properties.Settings.Default.Save();
                MainVM vm = MainVM.GetInstance;
                vm.RecvPath = (value as string).Replace("//", @"/");
            }
        }

        public string TransmPath
        {
            get { return transmPath; }
            set
            {
                transmPath = value;
                Properties.Settings.Default.transm_path = value;
                Properties.Settings.Default.Save();
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainVM vm = MainVM.GetInstance;
                    vm.TransmPath = (value as string).Replace("//", @"/");
                    vm.DirContentDirs = TransmPathDirs;
                    vm.DirContentFiles = TransmPathFiles;
                }));
            }
        }

        public ObservableCollection<string> TransmPathDirs
        {
            get
            {
                ObservableCollection<string> directories = new ObservableCollection<string>();
                foreach (string dir in Directory.GetDirectories(transmPath))
                {
                    directories.Add(Path.GetFileName(dir));
                }
                return directories;
            }
        }

        public void FilterDirs(string filter)
        {
            ObservableCollection<string> dirs = MainVM.GetInstance.DirContentDirs;
            Task.Run(() => {
                ObservableCollection<string> filtered = ProvideFiltering(dirs, filter);
                dirs = null;
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainVM.GetInstance.DirContentDirs = filtered;
                }));
            });
        }

        public void FilterFiles(string filter)
        {
            ObservableCollection<string> files = MainVM.GetInstance.DirContentFiles;
            Task.Run(() => {
                ObservableCollection<string> filtered = ProvideFiltering(files, filter);
                files = null;
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainVM.GetInstance.DirContentFiles = filtered;
                }));
            });
        }

        public void FilterByFormat(List<FileFormat> formats)
        {
            ObservableCollection<string> files = new ObservableCollection<string>();
            Task.Run(() =>
            {
                foreach (string file in Directory.GetFiles(transmPath))
                {
                    if (formats.Contains(ImageConverter.GetFormat(file)))
                        files.Add(Path.GetFileName(file));
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainVM.GetInstance.DirContentFiles = files;
                }));
            });
        }

        private ObservableCollection<string> ProvideFiltering(ObservableCollection<string> content, string filter)
        {
            ObservableCollection<string> filtered = new ObservableCollection<string>();
            foreach (string dir in content)
            {
                if (dir.ToLower().Contains(filter))
                {
                    filtered.Add(dir);
                }
            }
            return filtered;
        }

        public ObservableCollection<string> TransmPathFiles
        {
            get
            {
                ObservableCollection<string> files = new ObservableCollection<string>();
                foreach (string file in Directory.GetFiles(transmPath))
                {
                    files.Add(Path.GetFileName(file));
                }
                return files;
            }
        }

        public bool ReturnBackTransmDirectory()
        {
            if (!TransmPath.Equals(Directory.GetDirectoryRoot(TransmPath)))
            {
                TransmPath = Path.GetDirectoryName(TransmPath);
                if (!TransmPath.Equals(Directory.GetDirectoryRoot(TransmPath))) return false;
                return true;
            }
            return false;
        }

        public void SelectTransmDirectory(ref string directory)
        {
            TransmPath = Path.Combine(TransmPath, directory);
        }

        public bool IsTransmPathRoot()
        {
            return TransmPath.Equals(Directory.GetDirectoryRoot(TransmPath));
        }

        private FileSystemC()
        {
            _receive_log = new RecvFilesLog(GetRecvLogJSON());
            MainVM.GetInstance.RecvLog = ReceiveLog.RecvLog;
            appdata_folder = GetAppDataFolder();
            string rp = Properties.Settings.Default.recv_path;
            try
            {
                FileAttributes attr = File.GetAttributes(rp);
                if (!attr.HasFlag(FileAttributes.Directory))
                {
                   RecvPath = setDefaultDownloadPath();
                }
                else
                {
                    RecvPath = rp;
                }
                rp = Properties.Settings.Default.transm_path;
                attr = File.GetAttributes(rp);
                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    TransmPath = setDefaultDownloadPath();
                }
                else
                {
                    TransmPath = rp;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is ArgumentException)
                   RecvPath = setDefaultDownloadPath();
                   TransmPath = recvPath;
            }
        }

        public FileStream GetStreamReceive(ref string filename, out string directory)
        {
            directory = recvPath;
            string file_path = Path.Combine(recvPath, filename);
            if (File.Exists(file_path))
            {
                string[] file_parts = filename.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                ushort index = 0;
                if (file_parts.Length == 1)
                {
                    do
                    {
                        index++;
                        filename = string.Format("{0} ({1})", file_parts[0], index.ToString());
                        file_path = Path.Combine(recvPath, filename);
                    }
                    while (File.Exists(file_path));
                }
                else
                {
                    do
                    {
                        index++;
                        filename = string.Format("{0} ({1}).{2}", file_parts[0], index.ToString(), file_parts[1]);
                        file_path = Path.Combine(recvPath, filename);
                    }
                    while (File.Exists(file_path));
                }
            }
            try
            {
                return File.Create(file_path);
            }
            catch
            {
                return null;
            }
        }

        public static FileSystemC GetInstance()
        {
            if (instance == null)
            {
                instance = new FileSystemC();
            }
            return instance;
        }

        private string setDefaultDownloadPath()
        {
            try
            {
                return getDownloadPath();
            }
            catch (ExternalException ex) { MessageBox.Show(ex.Message, "Alert", MessageBoxButton.OK, MessageBoxImage.Information); return ""; }
        }

        private static string GetAppDataFolder()
        {
            string f_name = Properties.Settings.Default.app_data_folder_name;
            if (string.IsNullOrEmpty(Properties.Settings.Default.app_data_folder_path))
            {
                string dstarfolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), f_name);
                try
                {
                    Directory.CreateDirectory(dstarfolder);
                }
                catch
                {
                    dstarfolder = getDownloadPath();
                    try
                    {
                        Directory.CreateDirectory(dstarfolder);
                    }
                    catch
                    {
                        dstarfolder = null;
                    }
                }
                if (dstarfolder != null)
                {
                    Properties.Settings.Default.app_data_folder_path = dstarfolder;
                    Properties.Settings.Default.Save();
                }
                return dstarfolder;
            }
            else
            {
                return Properties.Settings.Default.app_data_folder_path;
            }
        }

        public ConcurrentDictionary<string, MemberInfoContainer> GetMembersJSON()
        {
            try
            {
                if (appdata_folder != null)
                {
                    using (FileStream fstream = new FileStream(Path.Combine(appdata_folder, "memberlist.json"), FileMode.OpenOrCreate))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, MemberInfoContainer>));
                        try
                        {
                            return (ConcurrentDictionary<string, MemberInfoContainer>)jsonSerializer.ReadObject(fstream);
                        }
                        catch
                        {
                            ConcurrentDictionary<string, MemberInfoContainer> members = new ConcurrentDictionary<string, MemberInfoContainer>();
                            jsonSerializer.WriteObject(fstream, members);
                            return members;
                        }
                    }
                }
            }
            finally { }
            return new ConcurrentDictionary<string, MemberInfoContainer>();
        }

        public ConcurrentDictionary<string, string> GetAbandonedJSON()
        {
            try
            {
                if (appdata_folder != null)
                {
                    using (FileStream fstream = new FileStream(Path.Combine(appdata_folder, "abandoned.json"), FileMode.OpenOrCreate))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, string>));
                        try
                        {
                            return (ConcurrentDictionary<string, string>)jsonSerializer.ReadObject(fstream);
                        }
                        catch
                        {
                            ConcurrentDictionary<string, string> abandoned = new ConcurrentDictionary<string, string>();
                            jsonSerializer.WriteObject(fstream, abandoned);
                            return abandoned;
                        }
                    }
                }
            }
            finally { }
            return new ConcurrentDictionary<string, string>();
        }

        public Dictionary<string, TransferTaskContainer> GetTasksJSON()
        {
            try
            {
                if (appdata_folder != null)
                {
                    using (FileStream fstream = new FileStream(Path.Combine(appdata_folder, "tasks.json"), FileMode.OpenOrCreate))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, TransferTaskContainer>));
                        try
                        {
                            return (Dictionary<string, TransferTaskContainer>)jsonSerializer.ReadObject(fstream);
                        }
                        catch
                        {
                            Dictionary<string, TransferTaskContainer> tasks = new Dictionary<string, TransferTaskContainer>();
                            jsonSerializer.WriteObject(fstream, tasks);
                            return tasks;
                        }
                    }
                }
            }
            finally { }
            return new Dictionary<string, TransferTaskContainer>();
        }

        public Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> GetRecvLogJSON()
        {
            try
            {
                if (appdata_folder != null)
                {

                    using (FileStream fstream = new FileStream(Path.Combine(appdata_folder, "recvlog.json"), FileMode.OpenOrCreate))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>));
                        try
                        {
                            return (Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>)jsonSerializer.ReadObject(fstream);
                        }
                        catch
                        {
                            Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> log = new Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>();
                            jsonSerializer.WriteObject(fstream, log);
                            return log;
                        }
                    }
                }
            }
            finally { }
            return new Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>();
        }

        public void UpdateJSONData()
        {
            if (appdata_folder != null)
            {
                MemberInteractionC service = MemberInteractionC.GetInstance();
                using (FileStream fstream = new FileStream(Path.Combine(appdata_folder ,"memberlist.json"), FileMode.OpenOrCreate))
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, MemberInfoContainer>));
                    jsonSerializer.WriteObject(fstream, service.Members);
                }
                using (FileStream fstream = new FileStream(Path.Combine(appdata_folder ,"abandoned.json"), FileMode.OpenOrCreate))
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, string>));
                    jsonSerializer.WriteObject(fstream, service.Abandoned);
                }
                using (FileStream fstream = new FileStream(Path.Combine(appdata_folder,"transfertasks.json"), FileMode.OpenOrCreate))
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Dictionary<string, TransferTaskContainer>));
                    jsonSerializer.WriteObject(fstream, FileTransferC.GetInstance().Transfers);
                }
                using (FileStream fstream = new FileStream(Path.Combine(appdata_folder, "recvlog.json"), FileMode.OpenOrCreate))
                {
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>));
                    jsonSerializer.WriteObject(fstream, ReceiveLog.RecvLog);
                }
            }
            
        }

        private static string getDownloadPath()
        {
            bool def_user = false;
            string guid = ConfigurationManager.AppSettings["download_folder_guid"];
            int res = SHGetKnownFolderPath(new Guid(guid), 0x00004000, new IntPtr(def_user ? -1 : 0), out IntPtr res_path);
            if (res >= 0)
            {
                string path = Marshal.PtrToStringUni(res_path);
                Marshal.FreeCoTaskMem(res_path);
                return path;
            }
            else
            {
                throw new ExternalException("'Download' folder not found. Select folder for recieve files.");
            }
        }

        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken,
        out IntPtr ppszPath);

        public void OpenDirectoryDialog(bool recv_folder)
        {
            using (var dialog = new form.FolderBrowserDialog())
            {
                dialog.SelectedPath = recv_folder ? RecvPath : TransmPath;
                form.DialogResult result = dialog.ShowDialog();

                if (result == form.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    if (recv_folder)
                        RecvPath = dialog.SelectedPath;
                    else
                        TransmPath = dialog.SelectedPath;
                }
            }
        }
    }
}
