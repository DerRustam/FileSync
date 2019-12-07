using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using DirectStar.ViewModel;
using DirectStar.Model;

namespace DirectStar.Controllers
{
    class FileTransferC : INotifyPropertyChanged
    {
        private static FileTransferC _instance;
        private Dictionary<string,TransferTaskContainer> _transfers;

        public Dictionary<string, TransferTaskContainer> Transfers
        {
            get { return _transfers; }
        }

        private FileTransferC()
        {
            _transfers = FileSystemC.GetInstance().GetTasksJSON();
            MainVM.GetInstance.TransferTaskLog = Transfers;
        }

        public void ProvideNewTransfer(string usermachine, string directory, SortedSet<string> files)
        {
            if (Transfers.TryGetValue(usermachine, out TransferTaskContainer taskContainer))
            {
                taskContainer.AddNewFiles(directory,files);
            }
            else
            {
                Transfers.Add(usermachine, new TransferTaskContainer(directory, files));
                OnPropertyChanged();
            }
        }

        

        public void ClearCompleted()
        {
            foreach(KeyValuePair<string, TransferTaskContainer> pair in Transfers)
            {
                if (pair.Value.CheckClearCompleted())
                {
                    Transfers.Remove(pair.Key);
                }
            }
        }

        public bool IsTransferExists(string usermachine, out TransferTaskContainer tasks)
        {
            return Transfers.TryGetValue(usermachine, out tasks);
        }

        public void RemoveUserTransfer(string usermachine)
        {
           if (Transfers.Remove(usermachine))
            {
                OnPropertyChanged();
            }
        }

        public static void Init()
        {
            if (_instance == null)
            {
                _instance = new FileTransferC();
            }
        }

        public static FileTransferC GetInstance()
        {
            return _instance ?? (_instance = new FileTransferC());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TransferTaskLog"));
        }
    }
}
