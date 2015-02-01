using PorygonOS.Core.Debug;
using PorygonOS.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.ShowDate = true;//show the date of when logs happen

            TaskThread.StartupDefault();//startup the task threads

            Console.In.ReadLine();

            TaskThread.ShutdownAll();
        }
    }
}
