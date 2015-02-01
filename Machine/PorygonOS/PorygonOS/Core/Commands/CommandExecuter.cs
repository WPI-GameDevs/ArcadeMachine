using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Commands
{
    public class CommandExecuter
    {
        public CommandExecuter()
        {
            foreach(Type commandType in Reflection.ReflectionHelper.GetTypesWithAttribute(typeof(CommandAttribute)))
            {
                CommandAttribute commandAttribute = commandType.GetCustomAttributes(typeof(CommandAttribute), true)[0] as CommandAttribute;

                ConstructorInfo creator = commandType.GetConstructor(new Type[0]);
                if (creator == null)
                    continue;

                object instance = creator.Invoke(null);

                commandTable.Add(commandAttribute.Name, instance);
            }
        }

        public void Execute(string[] commands)
        {
            while(commands.Length > 0)
            {
                string command = commands[0];//assume that the start is a command

                int i;
                for(i = 1; i < commands.Length; i++)
                {
                    string s = commands[i];
                    if (s.StartsWith("-"))//if it starts with - then it is a command and should be ignored
                        break;
                }

                int length = i - 1;//get the number of parameters that we have
                string[] parameters;
                if (length == 0)//if we have no parameters do not have an array
                    parameters = null;
                else
                {
                    parameters = new string[length];
                    Array.Copy(commands, 1, parameters, 0, length);
                }

                //get the instance that will interpret the command
                object commandInstance = null;
                commandTable.TryGetValue(command, out commandInstance);

                if (commandInstance != null)
                {
                    object[] methodParams = length == 0 ? new object[0] : new object[] { parameters };
                    MethodInfo executeMethod = commandInstance.GetType().GetMethod("Execute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (executeMethod != null)
                        executeMethod.Invoke(commandInstance, methodParams);
                }

                string[] oldCommands = commands;
                commands = new string[oldCommands.Length - length - 1];
                Array.Copy(oldCommands, length + 1, commands, 0, commands.Length);
            }
        }

        public string[] BreakCommandString(string commandString)
        {
            bool scanTillDoubleParen = false;
            bool scanTillParen = false;

            List<string> commandList = new List<string>();

            string currentScan = "";
            int stringSize = commandString.Length;
            for(int i = 0; i < stringSize; i++)
            {
                char c = commandString[i];

                if(scanTillDoubleParen)
                {
                    if (c == '\"')
                    {
                        scanTillDoubleParen = false;
                        continue;
                    }
                }
                else if(scanTillParen)
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

        private Dictionary<string, object> commandTable = new Dictionary<string, object>();
    }
}
