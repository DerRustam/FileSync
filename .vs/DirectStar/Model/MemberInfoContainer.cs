using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;

namespace DirectStar.Model
{
    [DataContract]
    public class MemberInfoContainer
    {
        [DataMember]
        private string _properName;
        [DataMember]
        private string _adjustedName;
        [DataMember]
        private Group _group;

        private IPAddress _localAddress;

        public string ProperName
        {
            get { return _properName; }
            set { _properName = value; }
        }

        public string AdjustedName
        {
            get { return _adjustedName; }
            set { _adjustedName = value; }
        }

        public Group Group
        {
            get { return _group; }
            set { _group = value; }
        }

        public IPAddress LocalAddress
        {
            get { return _localAddress; }
            set { _localAddress = value; }
        }

        public MemberInfoContainer(string properName, string adjustedName, Group group)
        {
            _properName = properName;
            _adjustedName = adjustedName;
            _group = group;
            _localAddress = null;
        }

        public MemberInfoContainer(string properName, string adjustedName, Group group, IPAddress ip_addr)
        {
            _properName = properName;
            _adjustedName = adjustedName;
            _group = group;
            _localAddress = ip_addr;
        }
    }

    public enum Group
    {
        None = 0,
        RequestSended = 1,
        RequestReceived = 2,
        Linked = 3,
        Favorite = 4,
    }
}
