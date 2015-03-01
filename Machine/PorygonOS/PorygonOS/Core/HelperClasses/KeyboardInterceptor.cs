using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace PorygonOS.Core.HelperClasses
{
    class KeyboardInterceptor
    {
        //Variables for setting up the hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        //Variables for keeping track of the hardmapped keys and the keys from the config
        private static string keyMapConfigSection = "KeyMapping";
        private static string[] keyMapConfigKeys = new string[]{ "green1", "red1", "blue1", "yellow1", "white1", "black1",
                                                  "green2", "red2", "blue2", "yellow2", "white2", "black2" };
        private static string[] TheAlphabet = { "Z", "Y", "X", "W", "V", "U", "T", "S", "R", "Q" };

        static bool keyPressed = false;

        /// <summary>
        /// Start intercepting the keys.
        /// </summary>
        public void Start()
        {
            _hookID = SetHook(_proc);
            RemapKeys(null);
            Application.Run();

            UnhookWindowsHookEx(_hookID);
        }

        /// <summary>
        /// Populate a dictionary of hard-mapped keys to game-sepcific keys
        /// </summary>
        public static Dictionary<string, string> RemapKeys(Config.ConfigFile gameConfigFile)
        {
            Dictionary<string, string> remapDictionary = new Dictionary<string, string>();

            //Loop over every key
            for (int i = 0; i < keyMapConfigKeys.Length; i ++ )
            {
                //Map the global config key to the game config key
                remapDictionary.Add(Program.GlobalConfig.GetString(keyMapConfigSection, keyMapConfigKeys[i]), TheAlphabet[i%10].ToLower());//Until we are able to properly load the gameconfig, just be random
                                    //gameConfigFile.GetString(keyMapConfigSection, keyMapConfigKeys[i]));
            }

            return remapDictionary;
        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// Callback function that is called whenever a keyboard event is fired.
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {

            //Load the game config file.
            Config.ConfigFile gameConfigFile = null;

            //Get the dictionary of keys from the configs
            Dictionary<string, string> keyDictionary = RemapKeys(gameConfigFile);

            //If a keyboard key was pressed.
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                //Parse the key mapping out of the config files.
                string pressedKeyString = ((Keys)Marshal.ReadInt32(lParam)).ToString();

                //This is the key that the user pressed and we want to replace it with a new key.
                if (!keyPressed)
                {
                    //If the database has the key, send off the replacement.
                    if (keyDictionary.ContainsKey(pressedKeyString))
                    {
                        keyPressed = true;
                        SendKeys.Send(keyDictionary[pressedKeyString]);//Use SendKeys to simulate the key press in another application
                    }
                    //If the database doesn't have the key, reset the flag and ignore it.
                    else
                    {
                        keyPressed = false;
                    }
                    //Always ignore the first key that a person presses.
                    return (System.IntPtr)1;
                }
                //After we send the key we want to send, it comes to the callback a second time. 
                //Just let it through and reset the flag.
                else
                {
                    keyPressed = false;
                }

                
                
                //Replace the pressed key with a new key and pass it along
                /*KBDLLHOOKSTRUCT replacementKey = new KBDLLHOOKSTRUCT();
                IntPtr newlParam = lParam;
                Marshal.PtrToStructure(newlParam, replacementKey);
                replacementKey.vkCode = (uint)remappedKey;//Set the key to a new key.
                Marshal.StructureToPtr(replacementKey, newlParam, true);*/

                //Return nothing to block the key presses.
                //return (System.IntPtr)1;
                //return CallNextHookEx(_hookID, nCode, wParam, newlParam);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }
    }
}
