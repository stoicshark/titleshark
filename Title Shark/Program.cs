using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace Title_Shark
{
    class Program
    {
        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr windowHandle, StringBuilder stringBuilder, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);

        private static List<IntPtr> windowList;
        private static string _className;
        private static StringBuilder apiResult = new StringBuilder(256); //256 Is max class name length.
        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        private static string header = "Title Shark version 1.0 - stoic";
        private static string saveFile = "Title Shark.txt";
        private static int updateTimer = 5000;
        private static List<String> filters;

        static void Main(string[] args)
        {
            Console.Title = "Title Shark";

            try
            {
                RunConsole();
            } catch
            {
                Console.Clear();
                RunConsole();
            }
            
        }
        private static void RunConsole()
        {
            var lines = File.ReadLines("Title Shark.ini", UnicodeEncoding.UTF8);
            int iniread = 0;
            filters = new List<string>();
            foreach (var line in lines)
            {
                if (line == "[General]") iniread = 1;
                if (line == "[Filters]") iniread = 2;
                if (line == "") continue;
                if (iniread == 1)
                {
                    if (line.Contains("savefile="))
                    {
                        saveFile = line.Replace("savefile=", "");
                    }
                    if (line.Contains("updatetimer="))
                    {
                        updateTimer = Int32.Parse(line.Replace("updatetimer=", "")) * 1000;
                    }
                }
                if (iniread == 2 && !line.Contains("[Filters]"))
                {
                    
                    filters.Add(line);
                }
            }

            Console.WriteLine(header);
            Console.WriteLine("\nFor best results, keep whichever tab you want tracked seperate in it's own window and minimized.\n\n");
            Console.WriteLine("Select title to track:\n\n");
            Console.WriteLine("     (1) YouTube");
            Console.WriteLine("     (2) aersia.skie.me (VIP)");
            Console.WriteLine("     (3) Window Title");
            Console.Write("\n\nEnter Number: ");
            int mode = Convert.ToInt32(Console.ReadLine());

            if (mode == 1) RunTrack(1, 0, false);
            if (mode == 2) RunTrack(2, 0, false);
            if (mode == 3)
            {
                List<int> item = new List<int>();
                int browser = 0;
                Console.Clear();
                Console.WriteLine(header);
                Console.WriteLine("\nSelect which browser window title to track:\n\n");
                List<IntPtr> BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "chrome");
                for (var i = 0; i < BrowserWindows.Count; i++)
                {
                    int length = GetWindowTextLength(BrowserWindows[i]);
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                    if (sb.ToString() != "")
                    {
                        item.Add(i);
                        Console.WriteLine("     (" + item.Count + ") " + sb.ToString());
                    }
                }
                browser = item.Count;
                BrowserWindows = WindowsFinder("MozillaWindowClass", "firefox");
                for (var i = 0; i < BrowserWindows.Count; i++)
                {
                    int length = GetWindowTextLength(BrowserWindows[i]);
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                    if (sb.ToString() != "")
                    {
                        item.Add(i);
                        Console.WriteLine("     (" + item.Count + ") " + sb.ToString());
                    }
                }
                Console.Write("\n\nEnter Number: ");
                int track = Convert.ToInt32(Console.ReadLine());
                if (track > browser)
                {
                    browser = 1;
                }
                else
                {
                    browser = 0;
                }

                try
                {
                    RunTrack(item[track - 1], browser, true);
                }
                catch
                {
                    Console.Clear();
                    RunConsole();
                }
            }
        }

        private static void RunTrack(int mode, int browser, bool custom)
        {
            string currentTitle = "";
            while (true)
            {
                StringBuilder sb = null;
                List<IntPtr> BrowserWindows = null;
                if (custom)
                {
                    if (browser == 0) BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "chrome");
                    if (browser == 1) BrowserWindows = WindowsFinder("MozillaWindowClass", "firefox");
                    int length = GetWindowTextLength(BrowserWindows[mode]);
                    sb = new StringBuilder(length + 1);
                    GetWindowText(BrowserWindows[mode], sb, sb.Capacity);
                }
                else
                {
                    string search = "";
                    if (mode == 1) search = " - YouTube";
                    if (mode == 2) search = "▶️ ";
                    BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "chrome");
                    for (var i = 0; i < BrowserWindows.Count; i++)
                    {
                        int length = GetWindowTextLength(BrowserWindows[i]);
                        sb = new StringBuilder(length + 1);
                        GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                        if (sb.ToString().Contains(search)) break;
                    }
                    if (!sb.ToString().Contains(search))
                    {
                        BrowserWindows = WindowsFinder("MozillaWindowClass", "firefox");
                        for (var i = 0; i < BrowserWindows.Count; i++)
                        {
                            int length = GetWindowTextLength(BrowserWindows[i]);
                            sb = new StringBuilder(length + 1);
                            GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                            if (sb.ToString().Contains(search)) break;
                        }
                    }
                    if (!sb.ToString().Contains(search))
                    {
                        Console.Clear();
                        RunConsole();
                    }
                }

                foreach (string filter in filters)
                {
                    sb.Replace(filter, "");
                }

                if (currentTitle != sb.ToString())
                {
                    currentTitle = sb.ToString();
                    using (StreamWriter writetext = new StreamWriter(saveFile))
                    {
                        writetext.WriteLine(currentTitle);
                    }
                    Console.Clear();
                    Console.WriteLine(header);
                    if (custom)
                    {
                        if (browser == 0) Console.Write("\nGoogle Chrome");
                        if (browser == 1) Console.Write("\nMozilla Firefox");
                    }
                    else
                    {
                        if (mode == 1) Console.Write("\nYouTube");
                        if (mode == 2) Console.Write("\naersia.skie.me (VIP)");
                    }
                    Console.Write(" window title is being tracked! Title saved to: " + saveFile + "\n\n\n");
                    Console.WriteLine("     " + sb.ToString());
                    Console.WriteLine("\n\nTracking may break if there's more than one window open or if tabs are rapidly accessed/moved/closed.");
                }

                Thread.Sleep(updateTimer);
            }



        }

        private static List<IntPtr> WindowsFinder(string className, string process)
        {
            _className = className;
            windowList = new List<IntPtr>();

            Process[] chromeList = Process.GetProcessesByName(process);

            if (chromeList.Length > 0)
            {
                foreach (Process chrome in chromeList)
                {
                    if (chrome.MainWindowHandle != IntPtr.Zero)
                    {
                        foreach (ProcessThread thread in chrome.Threads)
                        {
                            EnumThreadWindows((uint)thread.Id, new EnumThreadDelegate(EnumThreadCallback), IntPtr.Zero);
                        }
                    }
                }
            }

            return windowList;
        }

        static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (GetClassName(hWnd, apiResult, apiResult.Capacity) != 0)
            {
                if (string.CompareOrdinal(apiResult.ToString(), _className) == 0)
                {
                    windowList.Add(hWnd);
                }
            }
            return true;
        }
    }
}