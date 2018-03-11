namespace EventHook.Hooks
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Track global window focus.
    /// </summary>
    /// <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/ms644977%28v=vs.85%29.aspx?f=255&amp;MSPPError=-2147217396</remarks>
    public sealed class WindowHookEx: IDisposable
    {
        IntPtr hookID;

        /// <summary>
        /// Must be called from UI thread
        /// </summary>
        public WindowHookEx() {
            this.hookID = SetWinEventHook(
                hookMin: WindowEvent.ForegroundChanged, hookMax: WindowEvent.ForegroundChanged,
                moduleHandle: IntPtr.Zero, callback: this.Hook,
                processID: 0, threadID: 0,
                flags: HookFlags.OutOfContext);

            if (this.hookID == IntPtr.Zero)
                throw new Win32Exception();
        }

        /// <summary>
        /// Occurs when a window is about to be activated
        /// </summary>
        public event EventHandler<WindowEventArgs> Activated;

        void Hook(IntPtr hookHandle, WindowEvent @event,
            IntPtr hwnd,
            int @object, int child,
            int threadID, int timestampMs) {
            EventHandler<WindowEventArgs> handler = null;
            switch (@event) {
            case WindowEvent.ForegroundChanged: handler = this.Activated; break;
            }
            handler?.Invoke(this, new WindowEventArgs(hwnd));
        }

        void ReleaseUnmanagedResources() {
            if (this.hookID == IntPtr.Zero)
                return;
            if (!UnhookWinEvent(this.hookID))
                throw new Win32Exception();

            this.hookID = IntPtr.Zero;
        }

        /// <inheritdoc />
        public void Dispose() {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~WindowHookEx() {
            this.ReleaseUnmanagedResources();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetWinEventHook(WindowEvent hookMin, WindowEvent hookMax,
            IntPtr moduleHandle,
            WinEventProc callback, int processID, int threadID, HookFlags flags);

        [Flags]
        enum HookFlags: int
        {
            None = 0,
            OutOfContext = 0,
        }

        enum WindowEvent
        {
            ForegroundChanged = 0x03,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWinEvent(IntPtr hhk);

        delegate void WinEventProc(IntPtr hookHandle, WindowEvent @event,
            IntPtr hwnd,
            int @object, int child,
            int threadID, int timestampMs);
    }

    public class WindowEventArgs
    {
        public WindowEventArgs(IntPtr handle) {
            this.Handle = handle;
        }
        public IntPtr Handle { get; }
    }
}
