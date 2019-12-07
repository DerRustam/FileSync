using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DirectStar.Model
{
    class NetworkDeviceContainer
    {
        private string _deviceUserName;
        private IPAddress _localAddress;

        public string DeviceUserName
        {
            get
            {
                return _deviceUserName;
            }
        }
        public IPAddress LocalAddress
        {
            get
            {
                return _localAddress;
            }
            set
            {
                _localAddress = value;
            }
        }

        public NetworkDeviceContainer(string deviceUserName, IPAddress localAddress)
        {
            _deviceUserName = deviceUserName;
            _localAddress = localAddress;
        }
    }
}
