using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DirectStar.Monitors;
using DirectStar.Model;
using Settings = DirectStar.Properties.Settings;

namespace DirectStar.Controllers
{
    class NetInterface
    {
        private uint _ip_local;
        private string _interface_id;

        public uint IpLocal
        {
            get { return _ip_local; }
            set { _ip_local = value; }
        }

        public string InterfaceId
        {
            get { return _interface_id; }
            set { _interface_id = value; }
        }

        public NetInterface(uint ip_local, string interface_id)
        {
            _ip_local = ip_local;
            _interface_id = interface_id;
        }
    }

    class TransmissionC
    {
        private static TransmissionC _instance;
        private List<NetInterface> if_list;
        private UdpClient udp_sender;
        private TcpClient tcp_msg_sender;
        private Mutex mutex;
        private string pc_name;
        private int recv_port;
        private int send_port;

        public TransmissionC()
        {
            mutex = new Mutex();
            string[] user_pc = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            pc_name = string.Format("{0} ({1})", user_pc[0], user_pc[1]);
            tcp_msg_sender = new TcpClient();
            recv_port = Settings.Default.req_port_recv;
            send_port = Settings.Default.req_port_send;
            NetworkMonitor.NetworkChanged += new EventHandler<NetworkStatus>(Restart);
            if (if_list == null)
            {
                if_list = new List<NetInterface>();
            }
            ListenNewMemebers();
            ListenTcpConnection();
        }

        public static void InitService()
        {
            if (_instance == null)
            {
                _instance = new TransmissionC();
            }
        }

        public static TransmissionC GetInstance()
        {
            return _instance ?? (_instance = new TransmissionC());
        }

        private NetInterface FindInterface(string if_id)
        {
            foreach (NetInterface iface in if_list)
            {
                if (iface.InterfaceId == if_id) return iface;
            }
            return null;
        }

        private void Restart(object sender, NetworkStatus status)
        {
            if (status.Status == LANStatus.On)
            {
                HashSet<NetworkInterface> ifs_to_send = new HashSet<NetworkInterface>();
                foreach (var ni in (sender as NetworkMonitor).ConnectedInterfaces)
                {
                    foreach (var address in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            uint local_ip = BitConverter.ToUInt32(address.Address.GetAddressBytes(), 0);
                            NetInterface iface = FindInterface(ni.Id);
                            if (iface == null)
                            {
                                if_list.Add(new NetInterface(local_ip, ni.Id));
                                ifs_to_send.Add(ni);
                            }
                            else if (iface.IpLocal != local_ip)
                            {
                                iface.IpLocal = local_ip;
                                ifs_to_send.Add(ni);
                            }
                            break;
                        }
                    }
                }
                if (ifs_to_send.Count != 0)
                {
                    Task.Run(() => SendAttention(ifs_to_send));
                }
            }
            else
            {
                if_list.Clear();
            }
        }

        private void ListenNewMemebers()
        {
            UdpClient listener = new UdpClient(recv_port);
            listener.BeginReceive(new AsyncCallback(ProvideAcceptNewMember), listener);
        }

        private void ProvideAcceptNewMember(IAsyncResult asyncResult)
        {
            UdpClient listener = (UdpClient)asyncResult.AsyncState;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, recv_port);
            byte[] data = listener.EndReceive(asyncResult, ref ipep);
            IPAddress ip_client = ipep.Address;
            listener.BeginReceive(new AsyncCallback(ProvideAcceptNewMember), listener);
            if (!IsIpThisMachine(BitConverter.ToUInt32(ipep.Address.GetAddressBytes(), 0)) && IsMessageValid(ref data, out string usermachine, out string message))
            {
                if (message.Equals("MemCon") && SendMessageToUser("MemOnline", ip_client))
                {
                    MemberInteractionC iser = MemberInteractionC.GetInstance();
                    if (iser.IsFindAbandonedMessage(ref usermachine, out message) && SendMessageToUser(message, ip_client))
                    {
                        iser.Abandoned.TryRemove(usermachine, out string val);
                    }
                    iser.ProcessNewUser(usermachine, ip_client);
                    if (iser.IsFilesOnSend(ref usermachine, out TransferTaskContainer transfers))
                    {
                        ProcessSending(usermachine, ip_client, transfers);
                    }
                }
            }
        }

        public void TrySend(string usermachine)
        {
            MemberInteractionC iser = MemberInteractionC.GetInstance();
            IPAddress local_ip = iser.TryGetMemberIP(usermachine);
            if (iser.IsFilesOnSend(ref usermachine, out TransferTaskContainer transfers) && local_ip != null)
            {
                ProcessSending(usermachine, local_ip, transfers);
            }

        }

        private void ProcessSending(string usermachine, IPAddress local_ip, TransferTaskContainer transfers)
        {
            Task.Run(() =>
            {
                transfers.ResetTransferQueue();
                TcpClient client = new TcpClient();
                byte[] response = new byte[2];
                byte[] context = new byte[512];
                NetworkStream networkStream = null;
                FileInfo fi;
                string full_path;
                bool isFirstSend = true;
                while (transfers.TryGetNextTransfer(out string directory, out string filename))
                {
                    try
                    {
                        full_path = Path.Combine(directory, filename);
                        using (FileStream fileStream = File.Open(full_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fi = new FileInfo(full_path);
                            if (isFirstSend)
                            {
                                try
                                {
                                    client.Connect(new IPEndPoint(local_ip, recv_port));
                                    networkStream = client.GetStream();
                                    networkStream.Write(ConvertRequestString(string.Format("{0}{1}//{2}", Properties.Resources.msg_file, fi.Name, fi.Length), true), 0, 512);
                                    fileStream.CopyTo(networkStream);
                                    isFirstSend = false;
                                    if (TryGetAmountData(ref networkStream, ref response))
                                    {
                                        transfers.SetStatus(ref directory, ref filename, Properties.Resources.status_complete);
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                            }
                            else
                            {
                                try
                                {
                                    Encoding.UTF8.GetBytes(string.Format("{0}//{1}", fi.Name, fi.Length)).CopyTo(context,0);
                                    networkStream.Write(context, 0,512);
                                    fileStream.CopyTo(networkStream);
                                    if (TryGetAmountData(ref networkStream, ref response))
                                    {
                                        transfers.SetStatus(ref directory, ref filename, Properties.Resources.status_complete);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    break;
                                }
                            }

                        }
                    }
                    catch (FileNotFoundException)
                    {
                        transfers.SetStatus(ref directory, ref filename, Properties.Resources.status_not_found);
                    }
                    catch (Exception ex)
                    {
                        transfers.SetStatus(ref directory, ref filename, Properties.Resources.status_access);
                    }
                }
                networkStream.Dispose();
                client.Close();
            });
        }

        public void ProcessFilesReceive(ref string entry_message, ref string usermachine, ref NetworkStream stream)
        {
            string msg = entry_message;
            FileSystemC fsc = FileSystemC.GetInstance();
            int buffer_len = Settings.Default.buffer_size;
            byte[] buffer = new byte[buffer_len];
            byte[] msg_buf = new byte[Settings.Default.msg_size];
            bool isRec = true;
            while (isRec && TryGetFileContext(ref msg, out string filename, out long size))
            {
                FileStream output_file_stream = fsc.GetStreamReceive(ref filename, out string directory);
                if (output_file_stream != null)
                {
                    int bytes_readed = 0;
                    bool isStop = false;
                    try
                    {
                        while (!isStop)
                        {
                            if (size > buffer_len)
                            {
                                bytes_readed = stream.Read(buffer, 0, buffer_len);
                                output_file_stream.Write(buffer, 0, bytes_readed);
                                size -= bytes_readed;
                            }
                            else
                            {
                                bytes_readed = stream.Read(buffer, 0, (int)size);
                                if (size > bytes_readed)
                                {
                                    size -= bytes_readed;
                                }
                                else
                                {
                                    isStop = true;
                                    stream.Write(Encoding.UTF8.GetBytes("OK"), 0, 2);
                                }
                                output_file_stream.Write(buffer, 0, bytes_readed);
                            }
                        }
                        output_file_stream.Dispose();
                        fsc.ReceiveLog.AddNodeToLog(directory, filename);
                    }
                    catch (IOException)
                    {
                        output_file_stream.Dispose();
                        File.Delete(Path.Combine(directory, filename));
                        isRec = false;
                    }
                }
                else
                {
                    throw new FileNotFoundException();
                }
                if (TryGetAmountData(ref stream, ref msg_buf))
                {
                    msg = Encoding.UTF8.GetString(msg_buf);
                }
                else
                {
                    isRec = false;
                }
            }           
        }

        private bool TryGetFileContext(ref string msg_context, out string filename, out long size)
        {
            filename = "";
            size = 0;
            string[] segments = msg_context.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
            {
                filename = segments[0];
                if (long.TryParse(segments[1], out size))
                    return true;
            }
            return false;
        }

        private bool IsIpThisMachine(uint local_ip)
        {
            foreach (NetInterface iface in if_list)
            {
                if (iface.IpLocal == local_ip)
                {
                    return true;
                }
            }
            return false;
        }

        private void ListenTcpConnection()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, recv_port);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(ProvideAcceptCallback), listener);

        }

        private void ProvideAcceptCallback(IAsyncResult asyncResult)
        {
            TcpListener listener = (TcpListener)asyncResult.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(asyncResult);
            IPAddress ip_client = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            NetworkStream stream = client.GetStream();
            listener.BeginAcceptTcpClient(new AsyncCallback(ProvideAcceptCallback), listener);
            byte[] buffer = new byte[Settings.Default.msg_size];
            if (TryGetAmountData(ref stream, ref buffer) && IsMessageValid(ref buffer, out string usermachine, out string message))
            {
                stream.Write(Encoding.UTF8.GetBytes("OK"), 0, 2);
                MemberInteractionC iser = MemberInteractionC.GetInstance();
                switch (message)
                {
                    case "MemOnline":
                        {
                            client.Close();
                            if (iser.IsFindAbandonedMessage(ref usermachine, out message) && SendMessageToUser(message, ip_client))
                            {
                               iser.Abandoned.TryRemove(usermachine, out string val);
                            }
                            iser.ProcessNewUser(usermachine, ip_client);
                            if (iser.IsFilesOnSend(ref usermachine, out TransferTaskContainer transfers))
                            {
                                ProcessSending(usermachine, ip_client, transfers);
                            }
                            break;
                        }
                    case "RequestLinkSend":
                        {
                            iser.AddMember(ref usermachine, Group.RequestReceived);
                            break;
                        }
                    case "RequestLinkAccept":
                        {
                            iser.ChangeMemberGroup(ref usermachine, Group.Linked, ip_client);
                            break;
                        }
                    case "LinkStop":
                        {
                            iser.RemoveMember(ref usermachine);
                            break;
                        }
                    default:
                        {
                            if (message.StartsWith(Properties.Resources.msg_file) && iser.IsMemberAllowedTransfer(ref usermachine))
                            {
                                string filecontext = message.Remove(0, Properties.Resources.msg_file.Length);
                                ProcessFilesReceive(ref filecontext, ref usermachine, ref stream);
                            }
                            break;
                        }
                }

            }
            stream.Close();
            client.Close();
        }

        public bool SendMessageToUser(string message, IPAddress local_addr)
        {
            bool isOk = false;
            mutex.WaitOne();
            try
            {
                byte[] data = new byte[2];
                tcp_msg_sender = new TcpClient();
                if (tcp_msg_sender.ConnectAsync(local_addr, recv_port).Wait(200))
                {
                    NetworkStream str = tcp_msg_sender.GetStream();
                    str.Write(ConvertRequestString(message, true), 0, 512);
                    if (TryGetAmountData(ref str, ref data))
                    {
                        isOk = true;
                    }
                }
                tcp_msg_sender.Close();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                tcp_msg_sender.Close();
                mutex.ReleaseMutex();
            }
            return isOk;
        }

        private bool TryGetAmountData(ref NetworkStream nw_stream, ref byte[] data)
            {
                int len = data.Length;
                int offset = 0;
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                while (len > 0 && sw.ElapsedMilliseconds < 500)
                {
                    offset += nw_stream.Read(data, offset, len);
                    len -= offset;
                }
                sw.Stop();
                return len == 0;
            }

        private bool IsMessageValid(ref byte[] income, out string usermachine, out string message)
        {
            usermachine = "";
            message = "";
            int ind = Array.IndexOf(income, (byte)'\0');
            if (ind != -1)
            {
                income = income.Take(ind).ToArray();
            }
            string[] segments = Encoding.UTF8.GetString(income).Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 3 && segments[0].Equals("DStar"))
            {
                usermachine = segments[1];
                message = segments[2];
                return true;
            }
            return false;
        }

        private void SendAttention(HashSet<NetworkInterface> updated_ifs)
        {
            if (udp_sender == null)
            {
                udp_sender = new UdpClient(send_port);
            }
            udp_sender.EnableBroadcast = true;
            byte[] request = ConvertRequestString("MemCon", false);
            foreach (var ni in updated_ifs)
            {
                foreach (var address in ni.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        udp_sender.Send(request,
                            request.Length,
                            GetBroadcastAddress(address, recv_port)
                            );
                    }
                }
            }
        }

        private byte[] ConvertRequestString(string context, bool isTcp)
        {
            string request_str = string.Format("DStar*{0}*{1}", pc_name, context);
            if (!isTcp)
                return Encoding.UTF8.GetBytes(request_str);
            byte[] buffer = new byte[512];
            byte[] message = Encoding.UTF8.GetBytes(request_str);
            message.CopyTo(buffer, 0);
            return buffer;
        }

        private IPEndPoint GetBroadcastAddress(UnicastIPAddressInformation uaddr, int port)
        {
            uint ipaddr = BitConverter.ToUInt32(uaddr.Address.GetAddressBytes(), 0);
            uint subnetmask = BitConverter.ToUInt32(uaddr.IPv4Mask.GetAddressBytes(), 0);
            uint bcast = ipaddr | ~subnetmask;
            return new IPEndPoint(new IPAddress(BitConverter.GetBytes(bcast)), port);
        }

    }
}
