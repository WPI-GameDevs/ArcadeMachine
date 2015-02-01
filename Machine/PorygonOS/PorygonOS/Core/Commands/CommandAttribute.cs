using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Commands
{
    public class CommandAttribute : Attribute
    {
        public string Name
        {
            get { return commandName; }
        }

        public CommandAttribute(string name)
        {
            this.commandName = name;
        }

        private string commandName;
    }
}
