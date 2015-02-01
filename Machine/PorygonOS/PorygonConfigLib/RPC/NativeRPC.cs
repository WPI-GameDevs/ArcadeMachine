using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.RPC
{
    public enum RPCSecurityLevel
    {
        Low,
        Medium,
        High
    }

    public class NativeRPC : Attribute
    {
        public RPCSecurityLevel SecurityLevel
        {
            get { return security; }
        }

        public NativeRPC(RPCSecurityLevel security)
        {
            this.security = security;
        }

        private RPCSecurityLevel security;
    }
}
