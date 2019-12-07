using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DirectStar.Model
{
    public enum TransferStatus
    {
        Waiting,
        Transmitting,
        Complete
    }
    [DataContract]
    public class TransferTaskContainer : INotifyPropertyChanged
    {
        [DataMember]
        private Dictionary<string, ObservableCollection<TransmitInfoContainer>> _files_status_ui;
        [DataMember]
        private bool _isCompleted;

        private TransferStatus _transfer_status;
        private Dictionary<string, Queue<string>> _transfer_queues;

        public bool IsCompleted
        {
            get { return _isCompleted; }
            set { _isCompleted = value; }
        }

        public TransferStatus TransferStatus
        {
            get
            {
                return _transfer_status;
            }
            set
            {
                _transfer_status = value;
            }
        }

        public Dictionary<string, ObservableCollection<TransmitInfoContainer>> FilesStatusUI
        {
            get { return _files_status_ui; }
        }

        public void ResetTransferQueue()
        {
            _transfer_queues = new Dictionary<string, Queue<string>>();
            foreach(string dir in FilesStatusUI.Keys)
            {
                if (FilesStatusUI.TryGetValue(dir, out ObservableCollection<TransmitInfoContainer> files))
                {
                    Queue<string> queue = new Queue<string>();
                    foreach (TransmitInfoContainer tic in files)
                    {
                        if (!tic.Status.Equals(Properties.Resources.status_complete))
                        {
                            queue.Enqueue(tic.FileName);
                        }
                    }
                    _transfer_queues.Add(dir, queue);
                }
            }
        }

        public bool TryGetNextTransfer(out string dir, out string filename)
        {
            dir = null;
            filename = null;
            if (_transfer_queues.Count != 0)
            {
                dir = _transfer_queues.Keys.First();
                _transfer_queues.TryGetValue(dir, out Queue<string> files);
                filename = files.Dequeue();
                if (files.Count == 0)
                {
                    _transfer_queues.Remove(dir);
                }
                return true;
            }
            return false;
        }

        public void SetStatus(ref string dir, ref string filename, string status)
        {
            if (FilesStatusUI.TryGetValue(dir, out ObservableCollection<TransmitInfoContainer> tconts))
            {
                foreach(TransmitInfoContainer tic in tconts)
                {
                    if (tic.FileName.Equals(filename))
                    {
                        tic.Status = status;
                        break;
                    }
                }
            }
        }

        public TransferTaskContainer(string directory, SortedSet<string> files)
        {
            _files_status_ui = new Dictionary<string, ObservableCollection<TransmitInfoContainer>>();
            ObservableCollection<TransmitInfoContainer> transmitInfoCol = new ObservableCollection<TransmitInfoContainer>();
            foreach (string file in files)
            {
                transmitInfoCol.Add(new TransmitInfoContainer(file));
            }
            FilesStatusUI.Add(directory, transmitInfoCol);
            OnPropertyChanged("FilesStatusUI");
        }

        public void AddNewFiles(string directory, SortedSet<string> new_files)
        {
            if (FilesStatusUI.TryGetValue(directory, out ObservableCollection<TransmitInfoContainer> files))
            {
                bool isExist;
                foreach (string file in new_files)
                {
                    isExist = false;
                    foreach (TransmitInfoContainer tic in files)
                    {
                        if (tic.FileName.Equals(file))
                        {
                            tic.Status = string.Empty;
                            isExist = true;
                        }
                    }
                    if (!isExist)
                    {
                        files.Add(new TransmitInfoContainer(file));
                    }
                }
            }
            else
            {
                ObservableCollection<TransmitInfoContainer> new_dir_files = new ObservableCollection<TransmitInfoContainer>();
                foreach (string file in new_files)
                {
                    new_dir_files.Add(new TransmitInfoContainer(file));
                }
                FilesStatusUI.Add(directory, new_dir_files);
                OnPropertyChanged("FilesStatusUI");
            }
        }

        public bool CheckClearCompleted()
        {
            foreach (KeyValuePair<string, ObservableCollection<TransmitInfoContainer>> pair in FilesStatusUI)
            {
                ObservableCollection<TransmitInfoContainer> dir_files = pair.Value;
                foreach (TransmitInfoContainer cont in dir_files)
                {
                    if (cont.Status.Equals(Properties.Resources.status_complete))
                    {
                        dir_files.Remove(cont);
                    }
                }
                if (dir_files.Count == 0)
                {
                    FilesStatusUI.Remove(pair.Key);
                }
            }
            return FilesStatusUI.Count == 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
        }

        public class TransmitInfoContainer : INotifyPropertyChanged
        {
            private string _filename;
            private string _status;

            public string FileName
            {
                get { return _filename; }
            }

            public string Status
            {
                get { return _status; }
                set
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }

            public TransmitInfoContainer(string filename)
            {
                _filename = filename;
                _status = string.Empty;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propname)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
            }
        }

    }
}
