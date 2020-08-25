using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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
        private static string header = "Title Shark version 1.2 - stoic";
        private static string saveFile = "Title Shark.txt";
        private static int updateTimer = 5;
        private static int timeOut = 30;
        private static List<String> filters;
        private static bool dowork = false;

        static void Main(string[] args)
        {
            Console.Title = "Title Shark";
            try
            {
                if (args.Length > 0)
                {
                    RunConsole(args[0]);
                }
                else
                {
                    RunConsole("");
                }
            }
            catch (Exception e)
            {
                string[] err = { e.Message };
                Main(err);
            }
        }
        private static void RunConsole(string error)
        {
            Console.Clear();
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
                        updateTimer = int.Parse(line.Replace("updatetimer=", ""));
                    }
                    if (line.Contains("timeout="))
                    {
                        timeOut = int.Parse(line.Replace("timeout=", ""));
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
            Console.WriteLine("     (3) Window Title\n\n");
            if (error != "") Console.Write("Error: " + error + "\n\n");
            Console.Write("Enter Number: ");
            int mode = Convert.ToInt32(Console.ReadLine());

            switch (mode)
            {
                case 1:
                    RunTrack(1, 0, false);
                    break;
                case 2:
                    RunTrack(2, 0, false);
                    break;
                case 3:
                    List<BrowserItem> browserItem = new List<BrowserItem>();
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
                            browserItem.Add(new BrowserItem(0, i));
                            Console.WriteLine("     (" + browserItem.Count + ") " + sb.ToString());
                        }
                    }
                    BrowserWindows = WindowsFinder("MozillaWindowClass", "firefox");
                    for (var i = 0; i < BrowserWindows.Count; i++)
                    {
                        int length = GetWindowTextLength(BrowserWindows[i]);
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                        if (sb.ToString() != "")
                        {
                            browserItem.Add(new BrowserItem(1, i));
                            Console.WriteLine("     (" + browserItem.Count + ") " + sb.ToString());
                        }
                    }
                    BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "msedge");
                    for (var i = 0; i < BrowserWindows.Count; i++)
                    {
                        int length = GetWindowTextLength(BrowserWindows[i]);
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                        if (sb.ToString() != "")
                        {
                            browserItem.Add(new BrowserItem(2, i));
                            Console.WriteLine("     (" + browserItem.Count + ") " + sb.ToString());
                        }
                    }
                    BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "brave");
                    for (var i = 0; i < BrowserWindows.Count; i++)
                    {
                        int length = GetWindowTextLength(BrowserWindows[i]);
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                        if (sb.ToString() != "")
                        {
                            browserItem.Add(new BrowserItem(3, i));
                            Console.WriteLine("     (" + browserItem.Count + ") " + sb.ToString());
                        }
                    }
                    Console.Write("\n\nEnter Number: ");
                    int track = Convert.ToInt32(Console.ReadLine());
                    RunTrack(browserItem[track - 1].trackid, browserItem[track - 1].browser, true);
                    break;
                default:
                    throw new System.ArgumentException("Index was out of range. Must be non-negative and less than the size of the collection.", "index");
            }
        }

        private static void RunTrack(int mode, int browser, bool custom)
        {
            string currentTitle = "";
            int update = updateTimer;
            int timeout = 0;
            dowork = true;

            var worker = new Thread(() =>
            {
                while (dowork)
                {
                    if (update < updateTimer - 1)
                    {
                        update++;
                    }
                    else
                    {
                        update = 0;
                        StringBuilder sb = null;
                        List<IntPtr> BrowserWindows = null;
                        bool searching = false;
                        if (custom)
                        {
                            if (browser == 0) BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "chrome");
                            if (browser == 1) BrowserWindows = WindowsFinder("MozillaWindowClass", "firefox");
                            if (browser == 2) BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "msedge");
                            if (browser == 3) BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "brave");
                            var length = GetWindowTextLength(BrowserWindows[mode]);
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
                            if (sb == null || !sb.ToString().Contains(search))
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
                            if (sb == null || !sb.ToString().Contains(search))
                            {
                                BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "msedge");
                                for (var i = 0; i < BrowserWindows.Count; i++)
                                {
                                    int length = GetWindowTextLength(BrowserWindows[i]);
                                    sb = new StringBuilder(length + 1);
                                    GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                                    if (sb.ToString().Contains(search)) break;
                                }
                            }
                            if (sb == null || !sb.ToString().Contains(search))
                            {
                                BrowserWindows = WindowsFinder("Chrome_WidgetWin_1", "brave");
                                for (var i = 0; i < BrowserWindows.Count; i++)
                                {
                                    int length = GetWindowTextLength(BrowserWindows[i]);
                                    sb = new StringBuilder(length + 1);
                                    GetWindowText(BrowserWindows[i], sb, sb.Capacity);
                                    if (sb.ToString().Contains(search)) break;
                                }
                            }
                            if (sb == null || !sb.ToString().Contains(search))
                            {
                                searching = true;
                                sb.Clear();
                                sb.Append("Searching...");
                                timeout += updateTimer;
                                if (timeout >= timeOut && timeOut != 0)
                                {
                                    dowork = false;
                                    string[] err = { "Unable to find the window title, timed out. (" + timeout + "s)" };
                                    Main(err);
                                    break;
                                }
                            }
                            else
                            {
                                timeout = 0;
                            }
                        }

                        foreach (string filter in filters)
                        {
                            sb.Replace(filter, "");
                        }

                        if (currentTitle != sb.ToString())
                        {
                            currentTitle = sb.ToString();
                            if (!searching)
                            {
                                using (StreamWriter writetext = new StreamWriter(saveFile))
                                {
                                    writetext.WriteLine(currentTitle);
                                }
                            }
                            Console.Clear();
                            Console.WriteLine(header);
                            if (custom)
                            {
                                if (browser == 0) Console.Write("\nGoogle Chrome");
                                if (browser == 1) Console.Write("\nMozilla Firefox");
                                if (browser == 2) Console.Write("\nMicrosoft Edge");
                                if (browser == 3) Console.Write("\nBrave");
                            }
                            else
                            {
                                if (mode == 1) Console.Write("\nYouTube");
                                if (mode == 2) Console.Write("\naersia.skie.me (VIP)");
                            }
                            Console.Write(" window title is being tracked! Title saved to: " + saveFile + "\n\n\n");
                            Console.WriteLine("     " + currentTitle);
                            Console.WriteLine("\n\nTracking may break if there's more than one window open or if tabs are rapidly accessed/moved/closed.");
                            Console.WriteLine("\n\nPress ESC to exit the program.\nPress Enter to return to the starting menu.");
                        }
                    }

                    Thread.Sleep(1000);
                }
            });

            worker.Start();
            while (dowork)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) Environment.Exit(0);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        dowork = false;
                        string[] err = { "" };
                        Main(err);
                        break;
                    }
                }
            }
        }

        private static List<IntPtr> WindowsFinder(string className, string process)
        {
            _className = className;
            Program.windowList = new List<IntPtr>();

            Process[] windowList = Process.GetProcessesByName(process);

            if (windowList.Length > 0)
            {
                foreach (Process window in windowList)
                {
                    if (window.MainWindowHandle != IntPtr.Zero)
                    {
                        foreach (ProcessThread thread in window.Threads)
                        {
                            EnumThreadWindows((uint)thread.Id, new EnumThreadDelegate(EnumThreadCallback), IntPtr.Zero);
                        }
                    }
                }
            }

            return Program.windowList;
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

    public class BrowserItem
    {
        public BrowserItem(int b, int t)
        {
            this.browser = b;
            this.trackid = t;
        }
        public int browser { get; set; }
        public int trackid { get; set; }
    } 
}