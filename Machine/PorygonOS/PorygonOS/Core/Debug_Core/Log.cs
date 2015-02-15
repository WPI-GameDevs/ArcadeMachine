using PorygonOS.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PorygonOS.Core.Debug
{
    public static class Log
    {
        public static bool ShowDate
        {
            get { return bShowDate; }
            set { bShowDate = value; }
        }

        public static bool ShowThreadID
        {
            get { return bShowThreadID; }
            set { bShowThreadID = value; }
        }

        public static void Write(string value)
        {
            if (bShowDate)
                value = "[" + DateTime.Now.ToString("T") + "]: " + value;
            if(bShowThreadID)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                value = "[" + id.ToString() + "]" + value;
            }

            Console.Out.Write(value);
        }

        public static void Write(bool value)
        {
            Write(value.ToString());
        }

        public static void Write(byte value)
        {
            Write(value.ToString());
        }

        public static void Write(char value)
        {
            Write(value.ToString());
        }

        public static void Write(int value)
        {
            Write(value.ToString());
        }

        public static void Write(uint value)
        {
            Write(value.ToString());
        }

        public static void Write(long value)
        {
            Write(value.ToString());
        }

        public static void Write(ulong value)
        {
            Write(value.ToString());
        }

        public static void Write(float value)
        {
            Write(value.ToString());
        }

        public static void Write(double value)
        {
            Write(value.ToString());
        }

        public static void Write(decimal value)
        {
            Write(value.ToString());
        }

        public static void Write(object value)
        {
            Write(value.ToString());
        }

        public static void Write(string format, params object[] args)
        {
            string s = string.Format(format, args);
            Write(s);
        }

        public static void WriteLine(string value)
        {
            Write(value + "\n");
            Console.Out.Flush();
        }

        public static void WriteLine(string format, params object[] args)
        {
            string s = string.Format(format, args);
            WriteLine(s);
        }

        public static void WriteLine(bool value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(char value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(uint value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(long value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(ulong value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(float value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(double value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(decimal value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(object value)
        {
            WriteLine(value.ToString());
        }

        private static bool bShowDate;
        private static bool bShowThreadID;
    }
}
