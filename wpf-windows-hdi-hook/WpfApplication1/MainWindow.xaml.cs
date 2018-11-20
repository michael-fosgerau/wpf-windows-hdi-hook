namespace WpfApplication1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Shapes;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    public class MousePosArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public uint MouseData { get; set; }
        public uint Flags { get; set; }
        public uint Time { get; set; }
        public MouseMessages Message { get; set; }

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_WBUTTONDOWN = 0x0207,
            WM_WBUTTONUP = 0x0208
        }
    }

    public class KeyboardArgs
    {
        public uint VirtualKeyCode { get; set; }
        public uint ScanCode { get; set; }
        public uint Flags { get; set; }
        public uint Time { get; set; }
        public KeyboardMessages Message { get; set; }
        public Keys Keys { get; set; }

        public enum KeyboardMessages
        {
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 0x101,
            WM_SYSKEYDOWN = 0x104,
            WM_SYSKEYUP = 0x105
        }
    }

    public static class MouseHook
    {
        public static void Start()
        {
            _mouseHookID = SetLowLevelHook(_llMouseproc, WH_MOUSE_LL);
            _keyboardHookID = SetLowLevelHook(_llKeyboardproc, WH_KEYBOARD_LL);
        }

        public static void stop()
        {
            UnhookWindowsHookEx(_mouseHookID);
            UnhookWindowsHookEx(_keyboardHookID);
        }
        private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static event EventHandler<KeyboardArgs> KeyboardAction = delegate { };
        private static LowLevelHookProc _llKeyboardproc = KeyboardHookCallback;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;

        public static event EventHandler<MousePosArgs> MouseAction = delegate { };
        private static LowLevelHookProc _llMouseproc = MouseHookCallback;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        private const int WH_MOUSE_LL = 14;

        private static IntPtr SetLowLevelHook(LowLevelHookProc proc, int hookId)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hook = SetWindowsHookEx(hookId, proc, GetModuleHandle("user32"), 0);
                if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
                return hook;
            }
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 /*&& MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam*/)
            {
                var hookStruct = (MSLLMOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLMOUSEHOOKSTRUCT));
                MouseAction(null, new MousePosArgs() { X = hookStruct.pt.x, Y = hookStruct.pt.y, MouseData = hookStruct.mouseData, Flags = hookStruct.flags, Time = hookStruct.time, Message = (MousePosArgs.MouseMessages)wParam });
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (MSLLKEYBOARDHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLKEYBOARDHOOKSTRUCT));
                var keys = (Keys)Marshal.ReadInt32(lParam);
                KeyboardAction(null, new KeyboardArgs() { Keys = keys, VirtualKeyCode = hookStruct.vkCode, ScanCode = hookStruct.scanCode, Flags = hookStruct.flags, Time = hookStruct.time, Message = (KeyboardArgs.KeyboardMessages)wParam });
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms644967(v=vs.85).aspx 
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLMOUSEHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms644970(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLKEYBOARDHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
    
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetWindowText(IntPtr hwnd, StringBuilder lpString, uint cch);

        [DllImport("User32.dll")]
        static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("User32.dll")]
        static extern IntPtr ChildWindowFromPoint(IntPtr hWndParent, POINT p);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetClassName(IntPtr hwnd, StringBuilder lpClassName, uint nMaxCount);

        [DllImport("User32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, uint nSize);

        private Stopwatch stopWatch;

        private string GetProcessInfo(IntPtr hWnd)
        {
            uint processID = 0;
            uint threadID = GetWindowThreadProcessId(hWnd, out processID);

            Process process = Process.GetProcessById((int)processID);

            var found = false;
            long sum = 0;
            for (int i = 0; i < listViewAppTime.Items.Count; i++)
            {
                var it = listViewAppTime.Items.GetItemAt(i) as ExeTimeItem;
                if (it != null) { sum += it.Time; }
            }
            for (int i = 0; i < listViewAppTime.Items.Count; i++)
            {
                var it = listViewAppTime.Items.GetItemAt(i) as ExeTimeItem;
                if (it != null && it.Exe == process.ProcessName)
                {
                    it.Time += (long)stopWatch.Elapsed.TotalMilliseconds;
                    found = true;
                    break;
                }
            }
            for (int i = 0; i < listViewAppTime.Items.Count; i++)
            {
                var it = listViewAppTime.Items.GetItemAt(i) as ExeTimeItem;
                if (it != null) {
                    it.Pct = ((it.Time / (float)sum) * 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            stopWatch.Restart();

            if (!found)
            {
                listViewAppTime.Items.Add(new ExeTimeItem() { Time = 0, Exe = process.ProcessName, Pct = "" });
            }

            return
                "ProcessId: " + processID.ToString() + " " +
                "ProcessName: " + process.ProcessName + " (" + process.MainWindowTitle + ") " +
                "Started: " + process.StartTime.ToString() + " " +
                "Threads: " + process.Threads.Count + " " +
                "Memory usage: " + process.WorkingSet64.ToString() ;
        }

        private List<Rectangle> mouseTrail;
        private class KeyLogItem
        {
            public string Time { get; set; }

            public string State { get; set; }

            public string VirtualKeyCode { get; set; }

            public string ScanCode { get; set; }

            public string CapsLock { get; set; }

            public string NumLock { get; set; }

            public string ScrollLock { get; set; }

            public string Shift { get; set; }

            public string VisibleChar { get; set; }
        }

        private class ExeTimeItem : INotifyPropertyChanged
        {
            private long time;
            public long Time
            {
                get
                {
                    return time;
                }

                set
                {
                    time = value;
                    NotifyPropertyChanged();
                }
            }

            private string pct;
            public string Pct
            {
                get
                {
                    return pct;
                }

                set
                {
                    pct = value;
                    NotifyPropertyChanged();
                }
            }

            public string Exe { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            stopWatch = new Stopwatch();
            stopWatch.Start();
            lastDown = false;
            mouseTrail = new List<Rectangle>();
            for (var i = 0; i < 1000; i++)
            {
                var t = new Rectangle() { Width = 3, Height = 3, Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 2 };
                mouseTrail.Add(t);
                canvas1.Children.Add(t);
            }

            MouseHook.MouseAction += new EventHandler<MousePosArgs>(OnMouseEvent);
            MouseHook.KeyboardAction += new EventHandler<KeyboardArgs>(OnKeyboardEvent);
            MouseHook.Start();
            
            /*
            label1.Content = "Screens: " + Screen.AllScreens.Length + Environment.NewLine +
                " BOL: " + Screen.AllScreens[0].Bounds.Left +
                " BOT: " + Screen.AllScreens[0].Bounds.Top +
                " BOLX: " + Screen.AllScreens[0].Bounds.Location.X +
                " BOLY: " + Screen.AllScreens[0].Bounds.Location.Y +
                " BOW: " + Screen.AllScreens[0].Bounds.Width +
                " BOH: " + Screen.AllScreens[0].Bounds.Height +
                " WAL: " + Screen.AllScreens[0].WorkingArea.Left +
                " WAT: " + Screen.AllScreens[0].WorkingArea.Top +
                " WALX: " + Screen.AllScreens[0].WorkingArea.Location.X +
                " WALY: " + Screen.AllScreens[0].WorkingArea.Location.Y +
                " WAW: " + Screen.AllScreens[0].WorkingArea.Width +
                " WAH: " + Screen.AllScreens[0].WorkingArea.Height +
                " " + Screen.AllScreens[0].DeviceName +
                " Primary: " + Screen.AllScreens[0].Primary +
                " BPP: " + Screen.AllScreens[1].BitsPerPixel + Environment.NewLine +
                " BOL: " + Screen.AllScreens[1].Bounds.Left +
                " BOT: " + Screen.AllScreens[1].Bounds.Top +
                " BOLX: " + Screen.AllScreens[1].Bounds.Location.X +
                " BOLY: " + Screen.AllScreens[1].Bounds.Location.Y +
                " BOW: " + Screen.AllScreens[1].Bounds.Width +
                " BOH: " + Screen.AllScreens[1].Bounds.Height +
                " WAL: " + Screen.AllScreens[1].WorkingArea.Left +
                " WAT: " + Screen.AllScreens[1].WorkingArea.Top +
                " WALX: " + Screen.AllScreens[1].WorkingArea.Location.X +
                " WALY: " + Screen.AllScreens[1].WorkingArea.Location.Y +
                " WAW: " + Screen.AllScreens[1].WorkingArea.Width +
                " WAH: " + Screen.AllScreens[1].WorkingArea.Height +
                " " + Screen.AllScreens[1].DeviceName +
                " Primary: " + Screen.AllScreens[1].Primary +
                " BPP: " + Screen.AllScreens[1].BitsPerPixel + Environment.NewLine +
                " BOL: " + Screen.AllScreens[2].Bounds.Left +
                " BOT: " + Screen.AllScreens[2].Bounds.Top +
                " BOLX: " + Screen.AllScreens[2].Bounds.Location.X +
                " BOLY: " + Screen.AllScreens[2].Bounds.Location.Y +
                " BOW: " + Screen.AllScreens[2].Bounds.Width +
                " BOH: " + Screen.AllScreens[2].Bounds.Height +
                " WAL: " + Screen.AllScreens[2].WorkingArea.Left +
                " WAT: " + Screen.AllScreens[2].WorkingArea.Top +
                " WALX: " + Screen.AllScreens[2].WorkingArea.Location.X +
                " WALY: " + Screen.AllScreens[2].WorkingArea.Location.Y +
                " WAW: " + Screen.AllScreens[2].WorkingArea.Width +
                " WAH: " + Screen.AllScreens[2].WorkingArea.Height +
                " " + Screen.AllScreens[2].DeviceName +
                " Primary: " + Screen.AllScreens[2].Primary +
                " BPP: " + Screen.AllScreens[2].BitsPerPixel
                ;
            */
        }

        private int trailUpdateCounter = 0;
        private void MapMouseToScreen(int x, int y, bool leftclick, bool rightclick)
        {
            var maxWidth = canvas1.Width;
            var maxHeight = canvas1.Height;

            var wt = (double)0;
            var ht = (double)0;

            for (var i = 0; i < Screen.AllScreens.Length; i++)
            {
                wt += Screen.AllScreens[0].Bounds.Width;
                ht += Screen.AllScreens[0].Bounds.Height;
            }

            var ox = ((double)x / wt) * maxWidth - 137;
            var oy = ((double)y / ht) * maxHeight - 3;

            trailUpdateCounter++;
            if (trailUpdateCounter >= 1)
            {
                for (var i = 1; i < mouseTrail.Count; i++)
                {
                    var mt = mouseTrail[i - 1];
                    var mt1 = mouseTrail[i];

                    mt.Stroke = mt1.Stroke;
                    Canvas.SetLeft(mt, Canvas.GetLeft(mt1));
                    Canvas.SetTop(mt, Canvas.GetTop(mt1));
                }
                var lastTrail = mouseTrail[mouseTrail.Count - 1];
                lastTrail.Stroke = mousePointer.Stroke == System.Windows.Media.Brushes.Black ? System.Windows.Media.Brushes.Gray : mousePointer.Stroke;
                Canvas.SetLeft(lastTrail, Canvas.GetLeft(mousePointer));
                Canvas.SetTop(lastTrail, Canvas.GetTop(mousePointer));
                trailUpdateCounter = 0;
            }
            var click = leftclick || rightclick;

            mousePointer.Stroke = click ? (leftclick ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.Red) : System.Windows.Media.Brushes.Black;
            Canvas.SetLeft(mousePointer, ox + maxWidth / 2);
            Canvas.SetTop(mousePointer, oy + 100);
        }

        private bool mouseLBtnDown = false;
        private bool mouseRBtnDown = false;
        private int getWindow = 0;
        private string w;
        private void OnMouseEvent(object sender, MousePosArgs e) {
            if (IsClosing) return;

            if (e.Message == MousePosArgs.MouseMessages.WM_LBUTTONDOWN)
            {
                mouseLBtnDown = true;
            }
            if (e.Message == MousePosArgs.MouseMessages.WM_LBUTTONUP)
            {
                mouseLBtnDown = false;
            }
            if (e.Message == MousePosArgs.MouseMessages.WM_RBUTTONDOWN)
            {
                mouseRBtnDown = true;
            }
            if (e.Message == MousePosArgs.MouseMessages.WM_RBUTTONUP)
            {
                mouseRBtnDown = false;
            }

            if (getWindow++ > 5)
            {
                IntPtr hwnd = WindowFromPoint(new POINT(e.X, e.Y));
                w = "Window Handle : " + hwnd.ToInt64();
                if (hwnd.ToInt64() > 0)
                {
                    w += " Caption: " + GetCaptionOfWindow(hwnd);
                    w += " ClassName: " + GetClassNameOfWindow(hwnd);

                    //For Parent 
                    IntPtr hWndParent = GetParent(hwnd);
                    if (hWndParent.ToInt64() > 0)
                    {
                        w += " ParentCaption: " + GetCaptionOfWindow(hWndParent);
                    }
                    w += Environment.NewLine + GetProcessInfo(hwnd);
                    
                }
                getWindow = 0;
            }

            label1.Content = "MOUSE: X=" + e.X + ", Y=" + e.Y + ", t=" + e.Time + ", flags=" + e.Flags + ", Data=" + e.MouseData + ", Message=" + e.Message + Environment.NewLine +
                "WINDOW: " + w;
            MapMouseToScreen(e.X, e.Y, mouseLBtnDown, mouseRBtnDown);
        }

        private string GetCaptionOfWindow(IntPtr hwnd)
        {
            string caption = "";
            StringBuilder windowText = null;
            try
            {
                uint max_length = GetWindowTextLength(hwnd);
                windowText = new StringBuilder("", (int)(max_length + 5));
                GetWindowText(hwnd, windowText, max_length + 2);

                if (!String.IsNullOrEmpty(windowText.ToString()) && !String.IsNullOrWhiteSpace(windowText.ToString()))
                    caption = windowText.ToString();
            }
            catch (Exception ex)
            {
                caption = ex.Message;
            }
            finally
            {
                windowText = null;
            }
            return caption;
        }

        private string GetClassNameOfWindow(IntPtr hwnd)
        {
            string className = "";
            StringBuilder classText = null;
            try
            {
                uint cls_max_length = 1000;
                classText = new StringBuilder("", (int)(cls_max_length + 5));
                GetClassName(hwnd, classText, cls_max_length + 2);

                if (!String.IsNullOrEmpty(classText.ToString()) && !String.IsNullOrWhiteSpace(classText.ToString()))
                    className = classText.ToString();
            }
            catch (Exception ex)
            {
                className = ex.Message;
            }
            finally
            {
                classText = null;
            }
            return className;
        }

        private Keys lastKeys;
        private bool lastDown;
        private void OnKeyboardEvent(object sender, KeyboardArgs e)
        {
            if (IsClosing) return;

            //label.Content = "KEY: VKCode=" + e.VirtualKeyCode + ", ScanCode=" + e.ScanCode + ", t=" + e.Time + ", flags=" + e.Flags + ", Message=" + e.Message;

            var up = (e.Message == KeyboardArgs.KeyboardMessages.WM_KEYUP);
            var down = (e.Message == KeyboardArgs.KeyboardMessages.WM_KEYDOWN);
            var state = (up ? "UP  " : (down ? "DOWN" : "    "));
            if (lastKeys == e.Keys)
            {
                if (lastDown)
                {
                    if (up) { state = "KEY "; }
                    listView.Items.RemoveAt(listView.Items.Count - 1);
                }
            }
            lastKeys = e.Keys;
            lastDown = down;

            var capsLock = System.Windows.Forms.Control.IsKeyLocked(Keys.CapsLock) ? "ON " : "OFF";
            var numLock = System.Windows.Forms.Control.IsKeyLocked(Keys.NumLock) ? "ON " : "OFF";
            var scrollLock = System.Windows.Forms.Control.IsKeyLocked(Keys.Scroll) ? "ON " : "OFF";
            var shift = System.Windows.Forms.Control.ModifierKeys == Keys.Shift ? "ON " : "OFF";
            var item = new KeyLogItem()
            {
                Time = e.Time.ToString("D12"),
                State = state,
                VirtualKeyCode = "0x" + e.VirtualKeyCode.ToString("X4"),
                ScanCode = "0x" + e.ScanCode.ToString("X4"),
                CapsLock = capsLock,
                NumLock = numLock,
                ScrollLock = scrollLock,
                Shift = shift,
                VisibleChar = e.Keys.ToString()
            };
            listView.Items.Add(item);
        }

        private bool IsClosing = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosing = true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var maxWidth = canvas1.Width;
            var maxHeight = canvas1.Height;

            var wt = (double)0;
            var ht = (double)0;

            canvas1.Children.Clear();

            for (var i = 0; i < mouseTrail.Count; i++)
            {
                canvas1.Children.Add(mouseTrail[i]);
            }

            for (var i = 0; i < Screen.AllScreens.Length; i++)
            {
                wt += Screen.AllScreens[0].Bounds.Width;
                ht += Screen.AllScreens[0].Bounds.Height;
            }
            for (var i = 0; i < Screen.AllScreens.Length; i++)
            {
                var w0 = ((double)Screen.AllScreens[i].Bounds.Width / wt) * maxWidth;
                var h0 = ((double)Screen.AllScreens[i].Bounds.Height / ht) * maxHeight;

                var ox = ((double)Screen.AllScreens[i].Bounds.Location.X / wt) * maxWidth - w0 / 2;
                var oy = ((double)Screen.AllScreens[i].Bounds.Location.Y / ht) * maxHeight;

                var r = new System.Windows.Shapes.Rectangle() { Stroke = System.Windows.Media.Brushes.Black, StrokeThickness = 2, Width = w0, Height = h0 };
                Canvas.SetLeft(r, ox + maxWidth / 2);
                Canvas.SetTop(r, oy + 100);
                canvas1.Children.Add(r);

                var l = new System.Windows.Controls.Label()
                {
                    FontSize = 10,
                    Content = Screen.AllScreens[i].DeviceName + " (" + Screen.AllScreens[i].Bounds.Width + "x" + Screen.AllScreens[i].Bounds.Height + ")" +
                    (Screen.AllScreens[i].Primary ? Environment.NewLine + "PRIMARY" : "")
                };
                Canvas.SetLeft(l, ox + maxWidth / 2 + 10);
                Canvas.SetTop(l, oy + 110);
                canvas1.Children.Add(l);
            }
        }
    }
}
