using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PorygonOS.Core.RPC
{
    public struct RPCCommandInfo
    {
        public bool IsValid
        {
            get { valid; }
        }

        public string Destination
        {
            get { return destination; }
        }

        public string Name
        {
            get { return cmdName; }
        }

        public string[] Args
        {
            get { return args; }
        }

        public RPCCommandInfo(bool valid, string destination, string cmdName, string[] args)
        {
            this.valid = valid;
            this.destination = destination;
            this.cmdName = cmdName;
            this.args = args;
        }

        private bool valid;
        private string destination;
        private string cmdName;
        private string[] args;
    }

    public class RPCManager
    {
        public delegate void RPCNativeCall(params string[] args);

        protected struct RPCTableEntry
        {
            public RPCNativeCall NativeCode
            {
                get { return nativeCode; }
            }

            public RPCSecurityLevel Security
            {
                get { return securityLeve; }
            }

            public RPCTableEntry(RPCNativeCall nativeCall, RPCSecurityLevel securityLevel)
            {
                this.nativeCode = nativeCall;
                this.securityLevel = securityLevel;
            }

            private RPCNativeCall nativeCode;
            private RPCSecurityLevel securityLevel;
        }

        public static RPCManager Instance
        {
            get { return instance; }
        }

        public static RPCManager Create(string localName)
        {
            if (instance == null)
                instance = new RPCManager(localName);
            return instance;
        }

        protected RPCManager(string localName)
        {
            this.localName = localName;

            Assembly loadingAssembly = Assembly.GetCallingAssembly();//load types from assembly that invoked the rpc creation
            Type[] types = loadingAssembly.GetTypes();//get all types in the loaded assembly

            foreach(Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach(MethodInfo method in methods)
                {
                    if (method.IsDefined(typeof(NativeRPC)))
                    {
                        NativeRPC info = method.GetCustomAttribute<NativeRPC>();
                        RPCNativeCall nativeCode = Delegate.CreateDelegate(typeof(RPCNativeCall), method) as RPCNativeCall;
                        if (!AddRPCCall(method.Name, nativeCode, info.SecurityLevel))
                            HookRPCCall(method.Name, nativeCode);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a command line as an RPC command.
        /// </summary>
        /// <param name="command">The command line to execute.</param>
        /// <param name="securityLevel">The security level to process it with.</param>
        /// <returns>True if the process could be processed, false otherwise.</returns>
        public virtual bool ProcessCommand(string command, RPCSecurityLevel securityLevel)
        {
            RPCCommandInfo commandInfo = ExtractCommand(command);
            if (!commandInfo.IsValid)
                return false;

            if (commandInfo.Destination != localName)
                return false;

            if (!cmdTable.ContainsKey(commandInfo.Name))
                return false;

            RPCTableEntry entry = cmdTable[commandInfo.Name];

            if (securityLevel < entry.Security)
                return false;

            try
            {
                entry.NativeCode(commandInfo.Args);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add an RPC call to the manager
        /// </summary>
        /// <param name="callName">What the name of the call should be, this is the name that will be parsed out of the command line.</param>
        /// <param name="nativeCode">The native code to execute when this RPC command is executed.</param>
        /// <param name="securityLevel">The security level of this call.</param>
        /// <returns>True if the new call could be added.</returns>
        public bool AddRPCCall(string callName, RPCNativeCall nativeCode, RPCSecurityLevel securityLevel)
        {
            if (cmdTable.ContainsKey(callName))
                return false;

            cmdTable.Add(callName, new RPCTableEntry(nativeCode, securityLevel));

            return true;
        }

        public bool HookRPCCall(string hookName, RPCNativeCall nativeCode)
        {
            if (!cmdTable.ContainsKey(hookName))
                return false;

            cmdTable[hookName].NativeCode += nativeCode;

            return true;
        }

        protected virtual RPCCommandInfo ExtractCommand(string command)
        {
            Match match = rpcRegEx.Match(command);
            if (!match.Success)
                return new RPCCommandInfo(false, null, null, null);

            string destination = match.Groups["dest"];
            string cmdName = match.Groups["name"];
            string commandLine = match.Groups["line"];

            string[] args = BreakCommandString(commandLine);

            return new RPCCommandInfo(true, destination, cmdName, args);
        }

        private string[] BreakCommandString(string commandString)
        {
            bool scanTillDoubleParen = false;
            bool scanTillParen = false;

            List<string> commandList = new List<string>();

            string currentScan = "";
            int stringSize = commandString.Length;
            for (int i = 0; i < stringSize; i++)
            {
                char c = commandString[i];

                if (scanTillDoubleParen)
                {
                    if (c == '\"')
                    {
                        scanTillDoubleParen = false;
                        continue;
                    }
                }
                else if (scanTillParen)
                {
                    if (c == '\'')
                    {
                        scanTillParen = false;
                        continue;
                    }
                }
                else if (c == '\"')
                {
                    scanTillDoubleParen = true;
                    continue;
                }
                else if (c == '\'')
                {
                    scanTillParen = true;
                    continue;
                }
                else if (c == ' ' || c == '\t' || c == '\n')
                {
                    if (!string.IsNullOrWhiteSpace(currentScan))
                        commandList.Add(currentScan);
                    currentScan = "";
                    continue;
                }

                currentScan += c;
            }

            if (!string.IsNullOrWhiteSpace(currentScan))
                commandList.Add(currentScan);

            return commandList.ToArray();
        }


        private Dictionary<string, RPCTableEntry> cmdTable = new Dictionary<string, RPCTableEntry>();//table of rpc commands that can be called

        private string localName;

        private const Regex rpcRegEx = new Regex(rpcPattern, RegexOptions.Compiled);
        private const string rpcPattern = @"##&RPC\[(?<dest>[.a-zA-Z0-9]+)\] (?<name>[a-zA-Z0-9]+) (?<line>[ -~]+)";

        private static RPCManager instance;
    }
}
