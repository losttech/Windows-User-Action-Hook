using System;
using System.Windows.Forms;
using EventHook.Hooks.Library;

namespace EventHook.Hooks
{
    /// <summary>
    ///     //https://github.com/lemkepf/ClipHub/blob/master/ClipHub/ClipHub/Code/Helpers/ShellHook.cs
    /// </summary>
    internal delegate void GeneralShellHookEventHandler(ShellHook sender, IntPtr hWnd);

    internal sealed class ShellHook : NativeWindow
    {
        private readonly uint _wmShellHook;

        internal ShellHook(IntPtr hWnd)
        {
            var cp = new CreateParams();

            // Create the actual window
            CreateHandle(cp);

            User32.SetTaskmanWindow(hWnd);

            if (User32.RegisterShellHookWindow(Handle))
            {
                _wmShellHook = User32.RegisterWindowMessage("SHELLHOOK");
            }
        }

        internal void DeRegister()
        {
            User32.RegisterShellHook(Handle, 0);
        }

        #region Shell events

        /// <summary>
        ///     A top-level, unowned window has been created. The window exists when the system calls this hook.
        /// </summary>
        internal event GeneralShellHookEventHandler WindowCreated;

        /// <summary>
        ///     A top-level, unowned window is about to be destroyed. The window still exists when the system calls this hook.
        /// </summary>
        internal event GeneralShellHookEventHandler WindowDestroyed;

        /// <summary>
        ///     The activation has changed to a different top-level, unowned window.
        /// </summary>
        internal event GeneralShellHookEventHandler WindowActivated;


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == _wmShellHook)
            {
                switch ((ShellEvents)m.WParam)
                {
                    case ShellEvents.HSHELL_WINDOWCREATED:
                        if (IsAppWindow(m.LParam))
                        {
                            OnWindowCreated(m.LParam);
                        }

                        break;
                    case ShellEvents.HSHELL_WINDOWDESTROYED:
                        WindowDestroyed?.Invoke(this, m.LParam);
                        break;

                    case ShellEvents.HSHELL_WINDOWACTIVATED:
                        WindowActivated?.Invoke(this, m.LParam);
                        break;
                }
            }

            base.WndProc(ref m);
        }

        #endregion

        #region Windows enumeration

        internal void EnumWindows()
        {
            User32.EnumWindows(EnumWindowsProc, IntPtr.Zero);
        }

        private bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam)
        {
            if (IsAppWindow(hWnd))
            {
                OnWindowCreated(hWnd);
            }

            return true;
        }

        private void OnWindowCreated(IntPtr hWnd)
        {
            if (WindowCreated != null)
            {
                WindowCreated(this, hWnd);
            }
        }

        private static bool IsAppWindow(IntPtr hWnd)
        {
            if (User32.IsWindowVisible(hWnd))
            {
                var extendedStyle = GetExtendedStyle(hWnd);
                if (extendedStyle.HasFlag(WindowStyleEx.WS_EX_TOOLWINDOW))
                {
                    return false;
                }

                var hwndOwner = User32.GetWindow(hWnd, (int)GetWindowContstants.GW_OWNER);
                return (GetStyle(hwndOwner) &
                        (WindowStyle.WS_VISIBLE | WindowStyle.WS_CLIPCHILDREN)) !=
                       (WindowStyle.WS_VISIBLE | WindowStyle.WS_CLIPCHILDREN) ||
                       GetExtendedStyle(hwndOwner).HasFlag(WindowStyleEx.WS_EX_TOOLWINDOW);
            }

            return false;
        }

        private static WindowStyleEx GetExtendedStyle(IntPtr hWnd)
        {
            return (WindowStyleEx)GetWindowLong(hWnd, (int)GWLIndex.GWL_EXSTYLE);
        }

        private static WindowStyle GetStyle(IntPtr hWnd)
        {
            return (WindowStyle)GetWindowLong(hWnd, (int)GWLIndex.GWL_STYLE);
        }

        private static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return User32.GetWindowLong(hWnd, nIndex);
            }

            return User32.GetWindowLongPtr(hWnd, nIndex);
        }

        #endregion
    }
}
