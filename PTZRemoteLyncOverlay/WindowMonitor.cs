using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PTZRemoteLyncOverlay
{
    /// <summary>
    /// Represents event information about a <see cref="Rectangle"/>.
    /// </summary>
    class RectEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        /// <value>
        /// The rectangle.
        /// </value>
        public Rect Rectangle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectEventArgs" /> class.
        /// </summary>
        /// <param name="rect">The rect.</param>
        public RectEventArgs(Rect rect)
        {
            Rectangle = rect;
        }
    }

    class WindowSelectionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the owner process of the window.
        /// </summary>
        /// <value>
        /// The owner process of the window.
        /// </value>
        public Process Process { get; private set; }

        /// <summary>
        /// Gets the active window.
        /// </summary>
        /// <value>
        /// The active window.
        /// </value>
        public WindowMonitor.WindowInformation ActiveWindow { get; private set; }

        /// <summary>
        /// Gets the selected window.
        /// </summary>
        /// <value>
        /// The selected window.
        /// </value>
        public WindowMonitor.WindowInformation SelectedWindow { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a selection has been made.
        /// </summary>
        /// <value>
        /// <c>true</c> if a selection has been made; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelectionMade { get { return SelectedWindow != null; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSelectionEventArgs" /> class.
        /// </summary>
        /// <param name="activeWindow">The active window.</param>
        public WindowSelectionEventArgs(Process process, WindowMonitor.WindowInformation activeWindow)
        {
            Process = process;
            ActiveWindow = activeWindow;
        }

        /// <summary>
        /// Selects the active window.
        /// </summary>
        public void Select()
        {
            if (SelectedWindow == null)
                SelectedWindow = ActiveWindow;
        }

        /// <summary>
        /// Selects the specified information.
        /// </summary>
        /// <param name="selectedWindow">The selected window.</param>
        public void Select(WindowMonitor.WindowInformation selectedWindow)
        {
            if (SelectedWindow == null)
                SelectedWindow = selectedWindow;
        }
    }

    class WindowMonitor
    {
        // Ideally this would be a DependencyObject, but I'm not bothered.

        #region NativeMethods
        private static class NativeMethods
        {
            public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

            struct Rect
            {
                public int Left;        // x position of upper-left corner
                public int Top;         // y position of upper-left corner
                public int Right;       // x position of lower-right corner
                public int Bottom;      // y position of lower-right corner
            }

            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", SetLastError = true)]
            private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32", SetLastError = true)]
            private static extern int GetWindowRect(IntPtr hWnd, ref Rect rect);

            [DllImport("user32.dll")]
            static extern bool EnumChildWindows(IntPtr hwndParent, Win32Callback lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            /// <summary>
            /// Gets the foreground window.
            /// </summary>
            /// <value>
            /// The foreground window.
            /// </value>
            public static IntPtr ForegroundWindow
            {
                get { return GetForegroundWindow(); }
            }

            /// <summary>
            /// Gets the process associated with a window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <returns>The associated process.</returns>
            public static Process GetWindowProcess(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero) return null;
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid == 0)
                {
                    ThrowWin32Error();
                    return null;
                }
                return Process.GetProcessById((int)pid);
            }

            /// <summary>
            /// Gets the thread associated with a window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <returns>The associated process.</returns>
            public static int GetWindowThread(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero) return 0;
                uint pid;
                var result = GetWindowThreadProcessId(hWnd, out pid);
                if (pid == 0)
                {
                    ThrowWin32Error();
                    return 0;
                }
                return result;
            }

            /// <summary>
            /// Gets information about a window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <returns>The information about a window.</returns>
            public static WindowInformation GetWindowInformation(IntPtr hWnd)
            {
                var sb = new StringBuilder(100);
                var len = GetClassName(hWnd, sb, sb.Capacity);

                return new WindowInformation(
                    handle: hWnd,
                    windowClass: sb.ToString().Substring(0, (int)len)
                    );
            }

            /// <summary>
            /// Gets the rectangle of a window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <returns>The rectangle of the window.</returns>
            public static System.Windows.Rect GetWindowRectangle(IntPtr hWnd)
            {
                var rect = new Rect();
                GetWindowRect(hWnd, ref rect);
                return new System.Windows.Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }

            /// <summary>
            /// Gets the child windows for the specified window.
            /// </summary>
            /// <param name="hWnd">The window handle.</param>
            /// <returns>The list of child windows.</returns>
            public static IEnumerable<IntPtr> GetChildWindows(IntPtr hWnd)
            {
                var result = new List<IntPtr>();
                Win32Callback cb = (h, l) =>
                {
                    result.Add(h);
                    return true;
                };
                EnumChildWindows(hWnd, cb, IntPtr.Zero);
                return result;
            }

            public static int GetWindowLong(IntPtr hwnd, WindowLong longType)
            {
                return GetWindowLong(hwnd, (int)longType);
            }

            private static void ThrowWin32Error()
            {
                var err = Marshal.GetLastWin32Error();
                ThrowWin32Error(err);
            }

            private static void ThrowWin32Error(int err)
            {
                if (err != 0) throw new Win32Exception(err);
            }
        }
        #endregion

        #region Enums
        public enum WindowLong
        {
            ID = (-12),
            Style = (-16),
            ExtendedStyle = (-20)
        }

        [Flags]
        public enum WindowStyles : long
        {
            Overlapped = 0,
            Popup = 0x80000000,
            Child = 0x40000000,
            Minimize = 0x20000000,
            Visible = 0x10000000,
            Disabled = 0x8000000,
            ClipSiblings = 0x4000000,
            ClipChildren = 0x2000000,
            Maximize = 0x1000000,
            Caption = 0xC00000,
            Border = 0x800000,
            DialogFrame = 0x400000,
            VerticalScroll = 0x200000,
            HorizontalScroll = 0x100000,
            SystemMenu = 0x80000,
            ThickFrame = 0x40000,
            Group = 0x20000,
            TabStop = 0x10000,
            MinimizeBox = 0x20000,
            MaximizeBox = 0x10000,
        }
        #endregion

        /// <summary>
        /// Represents information about a window.
        /// </summary>
        public class WindowInformation
        {
            /// <summary>
            /// Gets the handle of the window.
            /// </summary>
            /// <value>
            /// The handle of the window.
            /// </value>
            public IntPtr Handle { get; private set; }

            /// <summary>
            /// Gets the window class.
            /// </summary>
            /// <value>
            /// The window class.
            /// </value>
            public string WindowClass { get; private set; }

            /// <summary>
            /// Gets the child windows contained within this window.
            /// </summary>
            /// <value>
            /// The child windows contained within this window.
            /// </value>
            public IEnumerable<WindowInformation> ChildWindows
            {
                get
                {
                    foreach (var wnd in NativeMethods.GetChildWindows(Handle))
                    {
                        yield return NativeMethods.GetWindowInformation(wnd);
                    }
                }
            }

            /// <summary>
            /// Gets the style.
            /// </summary>
            /// <value>
            /// The style.
            /// </value>
            public WindowStyles Style
            {
                get
                {
                    return (WindowStyles)NativeMethods.GetWindowLong(Handle, WindowLong.Style);
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="WindowInformation" /> class.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="windowClass">The window class.</param>
            public WindowInformation(IntPtr handle = default(IntPtr), string windowClass = null)
            {
                Handle = handle;
                WindowClass = windowClass;
            }
        }

        /// <summary>
        /// Occurs when a valid window is activated.
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when the valid window's location changes.
        /// </summary>
        public event EventHandler<RectEventArgs> LocationChanged;

        /// <summary>
        /// Occurs when a valid window is deactivated.
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when a window needs to be selected.
        /// </summary>
        public event EventHandler<WindowSelectionEventArgs> SelectWindow;

        private Process targetProcess;

        private System.Threading.Timer checkProcessTimer;
        private System.Threading.Timer checkPositionTimer;

        private Rect lastRectangle;
        private bool running;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowMonitor" /> class.
        /// </summary>
        /// <param name="processPredicate">The process predicate.</param>
        /// <param name="windowPredicate">The window predicate.</param>
        public WindowMonitor()
        {
            checkProcessTimer = new System.Threading.Timer(CheckProcess, null, Timeout.Infinite, Timeout.Infinite);
            checkPositionTimer = new System.Threading.Timer(CheckPosition, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts searching for the window.
        /// </summary>
        public void Start()
        {
            running = true;
            checkProcessTimer.Change(0, Timeout.Infinite);
        }

        /// <summary>
        /// Stops searching for the window.
        /// </summary>
        public void Stop()
        {
            running = false;
            checkProcessTimer.Change(Timeout.Infinite, Timeout.Infinite);
            checkPositionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void CheckProcess(object state)
        {
            lastRectangle = new Rect(0, 0, 0, 0);

            Process process = null;
            try
            {
                var foreground = NativeMethods.ForegroundWindow;
                process = NativeMethods.GetWindowProcess(foreground);
                var info = NativeMethods.GetWindowInformation(foreground);

                var selectWindow = SelectWindow;
                if (process != null && selectWindow != null)
                {
                    var windowArgs = new WindowSelectionEventArgs(process, info);
                    selectWindow(this, windowArgs);

                    if (windowArgs.SelectedWindow != null)
                    {
                        process.Exited += process_Exited;
                        if (!process.HasExited)
                        {
                            targetProcess = process;

                            var act = Activated;
                            if (act != null) act(this, EventArgs.Empty);

                            process = null; // Prevent dispose (see finally).
                            checkPositionTimer.Change(0, Timeout.Infinite);
                            return;
                        }
                    }
                }

                var oldProcess = Interlocked.Exchange(ref targetProcess, null);
                if (oldProcess != null)
                {
                    oldProcess.Exited -= process_Exited;
                    var deact = Deactivated;
                    if (deact != null) deact(this, EventArgs.Empty);
                    oldProcess.Dispose();

                    checkPositionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    checkProcessTimer.Change(0, Timeout.Infinite);
                    return;
                }
            }
            catch
            {
                // There are a whole class of UAC and race conditions that we would
                // need to deal with, and would likely do the same thing I am doing
                // right now for them.
            }
            finally
            {
                if (process != null)
                {
                    process.Dispose();
                }
            }

            if (running)
                checkProcessTimer.Change(1000, Timeout.Infinite);
        }

        void process_Exited(object sender, EventArgs e)
        {
            var proc = Interlocked.Exchange(ref targetProcess, null);
            if (proc != null)
            {
                proc.Exited -= process_Exited;
                checkPositionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                checkProcessTimer.Change(1000, Timeout.Infinite);
                proc.Dispose();
            }
        }

        private void CheckPosition(object state)
        {
            var proc = targetProcess;
            if (proc != null)
            {
                var foreground = NativeMethods.ForegroundWindow;
                var windowArgs = new WindowSelectionEventArgs(proc, NativeMethods.GetWindowInformation(foreground));
                var selectWindow = SelectWindow;
                if (selectWindow != null) selectWindow(this, windowArgs);

                if (windowArgs.SelectedWindow == null)
                {
                    checkProcessTimer.Change(0, Timeout.Infinite);
                    return;
                }

                Rect? rect = null;
                try
                {
                    rect = NativeMethods.GetWindowRectangle(windowArgs.SelectedWindow.Handle);
                }
                catch { }

                if (rect != null)
                {
                    if (rect.Value != lastRectangle)
                    {
                        lastRectangle = rect.Value;
                        var tmp = LocationChanged;
                        if (tmp != null) tmp(this, new RectEventArgs(rect.Value));
                    }
                }

                if (running)
                    checkPositionTimer.Change(100, Timeout.Infinite);
            }
            else
            {
                checkProcessTimer.Change(0, Timeout.Infinite);
            }
        }
    }
}