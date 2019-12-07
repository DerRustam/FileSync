using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectStar.Model
{
    public class RecvFilesLog : INotifyPropertyChanged
    {
       private Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> _recvLog;
       private Mutex mutex;

       public Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> RecvLog
       {
            get { return _recvLog; }
            set { _recvLog = value; }
       }

       public RecvFilesLog(Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>> recvLog)
       {
            RecvLog = new Dictionary<DateTime, Dictionary<string, ObservableCollection<string>>>();
            foreach (DateTime key in recvLog.Keys)
            {
                if (DateTime.Today.Date - key.Date < TimeSpan.FromDays(8) && recvLog.TryGetValue(key, out Dictionary<string, ObservableCollection<string>> nodes))
                {
                    RecvLog.Add(key, nodes);
                }
            }
            OnPropertyChanged();
            mutex = new Mutex();
       }

        public void AddNodeToLog(string path, string file)
        {
            Task.Run(new Action(() =>
            {
                mutex.WaitOne();
                if (_recvLog.TryGetValue(DateTime.Today, out Dictionary<string, ObservableCollection<string>> nodes))
                {
                    if (nodes.TryGetValue(path, out ObservableCollection<string> files))
                    {
                        if (!files.Contains(file))
                        {
                            files.Add(file);
                        }
                    }
                    else
                    {
                        ObservableCollection<string> new_node = new ObservableCollection<string>();
                        new_node.Add(file);
                        nodes.Add(path, new_node);
                        OnPropertyChanged();
                    }
                }
                else
                {
                    Dictionary<string, ObservableCollection<string>> new_nodes = new Dictionary<string, ObservableCollection<string>>();
                    ObservableCollection<string> new_node = new ObservableCollection<string>();
                    new_node.Add(file);
                    new_nodes.Add(path, new_node);
                    _recvLog.Add(DateTime.Today, new_nodes);
                    OnPropertyChanged();
                }
                mutex.ReleaseMutex();
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecvLog"));
        }
            
    }
}
