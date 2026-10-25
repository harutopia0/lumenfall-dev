#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lumenfall.Editor
{
    /// <summary>
    /// Bo dieu khien che do khoi chay Play Mode:
    /// - Tester Mode (Mac dinh): Khoi chay tu man hinh Menu chinh (MainMenu.unity) va mo Game View toan man hinh khong vien
    ///   (Borderless Fullscreen) tach biet hoan toan khoi Unity Editor giong het chay file .exe doc lap.
    /// - Editor Mode: Vao thang Scene dang mo trong Editor (o Game View thong thuong, tu nap Core_Persistent).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeLauncher
    {
        private const string PREF_TESTER_MODE = "Lumenfall_PlayLauncher_IsTesterMode";
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
        private const string POPUP_MARKER = "Lumenfall_Tester_Fullscreen";

        private static bool isDialogActive = false;
        private static EditorWindow fullscreenWindow = null;
        private static IntPtr editorWindowBefore = IntPtr.Zero;

        static PlayModeLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.quitting -= ForceRestore;
            EditorApplication.quitting += ForceRestore;

            AssemblyReloadEvents.beforeAssemblyReload -= ForceRestore;
            AssemblyReloadEvents.beforeAssemblyReload += ForceRestore;

            HookGlobalKeyHandler();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (isDialogActive) return;
                isDialogActive = true;

                int choice = EditorUtility.DisplayDialogComplex(
                    "Lumenfall - Chon Che Do Choi (Launch Mode)",
                    "Chon cach ban muon khoi chay game:\n\n" +
                    "- [Tester Mode]: Khoi chay tu man hinh Menu (MainMenu.unity) va toan man hinh borderless giong het file .exe (an taskbar va giao dien Unity).\n\n" +
                    "- [Editor Mode]: Vao thang Scene dang chon/mo trong Editor (Game View thong thuong).",
                    "Tester Mode (.EXE Fullscreen)",     // Button 0 (Default khi nhan Enter)
                    "Huy (Cancel)",                     // Button 1
                    "Editor Mode"                       // Button 2
                );

                if (choice == 1) // Huy
                {
                    EditorApplication.isPlaying = false;
                    isDialogActive = false;
                    return;
                }

                if (choice == 0) // Tester Mode
                {
                    EditorPrefs.SetBool(PREF_TESTER_MODE, true);
                    SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MAIN_MENU_SCENE_PATH);
                    if (menuScene != null)
                    {
                        EditorSceneManager.playModeStartScene = menuScene;
                    }
                }
                else // Editor Mode (choice == 2)
                {
                    EditorPrefs.SetBool(PREF_TESTER_MODE, false);
                    EditorSceneManager.playModeStartScene = null;
                }

                isDialogActive = false;
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (EditorPrefs.GetBool(PREF_TESTER_MODE, false))
                {
                    EditorApplication.delayCall += OpenFullscreenGameView;
                }
                else
                {
                    // O che do Editor, neu GameView dang bi Maximize thi tra ve binh thuong
                    Type gvType = FindGameViewType();
                    if (gvType != null)
                    {
                        EditorWindow gv = EditorWindow.GetWindow(gvType, false, null, false);
                        if (gv != null && gv.maximized)
                        {
                            gv.maximized = false;
                        }
                    }
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (EditorPrefs.GetBool(PREF_TESTER_MODE, false))
                {
                    CloseFullscreenGameView();
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (EditorPrefs.GetBool(PREF_TESTER_MODE, false))
                {
                    EditorPrefs.SetBool(PREF_TESTER_MODE, false);
                    EditorSceneManager.playModeStartScene = null;
                }
                CloseFullscreenGameView();
            }
        }

        private static void ForceRestore()
        {
            CloseFullscreenGameView();
        }

        private static void OpenFullscreenGameView()
        {
            if (!EditorApplication.isPlaying) return;

            Type gameViewType = FindGameViewType();
            if (gameViewType == null)
            {
                Debug.LogError("[PlayModeLauncher] Khong tim thay UnityEditor.GameView!");
                return;
            }

            editorWindowBefore = Platform.GetForegroundWindowHandle();

            // 1. An Taskbar cua Windows
            Platform.SetTaskbarVisible(false);

            // 2. Tao mot cua so GameView doc lap
            fullscreenWindow = (EditorWindow)ScriptableObject.CreateInstance(gameViewType);
            fullscreenWindow.titleContent = new GUIContent(POPUP_MARKER);

            // 3. An thanh Toolbar cua GameView (khong hien Display, Aspect, Stats...)
            SetShowToolbar(fullscreenWindow, false);

            // 4. Tinh toan kich thuoc Monitor
            Rect monitorBounds = Platform.GetFullscreenBounds();
            Rect editorPoints = ToEditorPoints(monitorBounds);

            // 5. Hien thi dang Popup (khong vien, khong header, khong thanh docking)
            fullscreenWindow.position = editorPoints;
            fullscreenWindow.ShowPopup();
            fullscreenWindow.Focus();

            // 6. Win32: Set TOPMOST va phu toan bo pixel man hinh
            Platform.BeginTopmostRetry(monitorBounds, editorWindowBefore);
        }

        private static void CloseFullscreenGameView()
        {
            Platform.ClearTopmost();
            Platform.SetTaskbarVisible(true);

            if (fullscreenWindow != null)
            {
                try { fullscreenWindow.Close(); } catch { }
                fullscreenWindow = null;
            }

            Type gameViewType = FindGameViewType();
            if (gameViewType != null)
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (candidate != null && candidate.GetType() == gameViewType &&
                        candidate.titleContent != null && candidate.titleContent.text == POPUP_MARKER)
                    {
                        try { candidate.Close(); } catch { }
                    }
                }
            }

            Platform.RestoreEditorFocus(editorWindowBefore);
            editorWindowBefore = IntPtr.Zero;
        }

        private static Type FindGameViewType()
        {
            // 1. Tim truc tiep tu cac EditorWindow dang ton tai trong Editor
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var w in windows)
            {
                if (w != null && (w.GetType().Name == "GameView" || w.GetType().Name == "PlayModeView"))
                {
                    return w.GetType();
                }
            }

            // 2. Fallback tim qua Reflection
            Type type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if (type != null) return type;

            type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PlayModeView");
            if (type != null) return type;

            type = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (type != null) return type;

            type = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
            if (type != null) return type;

            return null;
        }

        private static void SetShowToolbar(EditorWindow window, bool value)
        {
            if (window == null) return;
            Type type = window.GetType();
            for (Type t = type; t != null; t = t.BaseType)
            {
                PropertyInfo prop = t.GetProperty("showToolbar",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(window, value);
                    return;
                }

                FieldInfo field = t.GetField("showToolbar",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field == null)
                    field = t.GetField("m_ShowToolbar", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(window, value);
                    return;
                }
            }
        }

        private static Rect ToEditorPoints(Rect physical)
        {
            float scale = EditorGUIUtility.pixelsPerPoint;
            if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
                scale = 1f;

            return new Rect(physical.x / scale, physical.y / scale, physical.width / scale, physical.height / scale);
        }

        private static void HookGlobalKeyHandler()
        {
            FieldInfo field = typeof(EditorApplication).GetField("globalEventHandler",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (field != null)
            {
                EditorApplication.CallbackFunction handler = (EditorApplication.CallbackFunction)field.GetValue(null);
                handler -= OnGlobalKeyEvent;
                handler += OnGlobalKeyEvent;
                field.SetValue(null, handler);
            }
        }

        private static void OnGlobalKeyEvent()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            // F11 hoac Escape de thoat che do Fullscreen / dung Play Mode
            if (e.keyCode == KeyCode.F11 || e.keyCode == KeyCode.Escape)
            {
                e.Use();
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    CloseFullscreenGameView();
                }
            }
        }

        private static class Platform
        {
#if UNITY_EDITOR_WIN
            [StructLayout(LayoutKind.Sequential)]
            private struct POINT { public int x; public int y; }

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT { public int left; public int top; public int right; public int bottom; }

            [StructLayout(LayoutKind.Sequential)]
            private struct MONITORINFO
            {
                public int cbSize;
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;
            }

            [DllImport("user32.dll")]
            private static extern IntPtr FindWindow(string className, string windowName);

            [DllImport("user32.dll")]
            private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowName);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool IsWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool GetCursorPos(out POINT point);

            [DllImport("user32.dll")]
            private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

            private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            private const uint SWP_SHOWWINDOW = 0x0040;
            private const uint MONITOR_DEFAULTTONEAREST = 2;
            private const int SW_HIDE = 0;
            private const int SW_SHOW = 5;

            private static IntPtr _fullscreenHwnd = IntPtr.Zero;

            public static IntPtr GetForegroundWindowHandle() => GetForegroundWindow();

            public static Rect GetFullscreenBounds()
            {
                if (GetCursorPos(out POINT cursor))
                {
                    IntPtr monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
                    MONITORINFO info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                    {
                        RECT r = info.rcMonitor;
                        return new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
                    }
                }

                Resolution res = Screen.currentResolution;
                return new Rect(0, 0, res.width, res.height);
            }

            public static void BeginTopmostRetry(Rect bounds, IntPtr foregroundBefore)
            {
                int attempts = 0;
                EditorApplication.CallbackFunction step = null;
                step = () =>
                {
                    attempts++;
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero && hwnd != foregroundBefore)
                    {
                        _fullscreenHwnd = hwnd;
                        SetWindowPos(hwnd, HWND_TOPMOST, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, SWP_SHOWWINDOW);
                        EditorApplication.update -= step;
                        return;
                    }

                    if (attempts >= 60)
                    {
                        EditorApplication.update -= step;
                    }
                };

                EditorApplication.update += step;
            }

            public static void ClearTopmost()
            {
                if (_fullscreenHwnd != IntPtr.Zero)
                {
                    if (IsWindow(_fullscreenHwnd))
                    {
                        SetWindowPos(_fullscreenHwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_SHOWWINDOW);
                    }
                    _fullscreenHwnd = IntPtr.Zero;
                }
            }

            public static void SetTaskbarVisible(bool visible)
            {
                int command = visible ? SW_SHOW : SW_HIDE;
                IntPtr primary = FindWindow("Shell_TrayWnd", null);
                if (primary != IntPtr.Zero)
                {
                    ShowWindow(primary, command);
                }

                IntPtr secondary = IntPtr.Zero;
                while ((secondary = FindWindowEx(IntPtr.Zero, secondary, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
                {
                    ShowWindow(secondary, command);
                }
            }

            public static void RestoreEditorFocus(IntPtr preferred)
            {
                IntPtr hwnd = preferred;
                if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                {
                    try
                    {
                        using (var process = System.Diagnostics.Process.GetCurrentProcess())
                        {
                            process.Refresh();
                            hwnd = process.MainWindowHandle;
                        }
                    }
                    catch { }
                }

                if (hwnd != IntPtr.Zero)
                {
                    SetForegroundWindow(hwnd);
                }
            }
#else
            public static IntPtr GetForegroundWindowHandle() => IntPtr.Zero;
            public static Rect GetFullscreenBounds()
            {
                Resolution res = Screen.currentResolution;
                return new Rect(0, 0, res.width, res.height);
            }
            public static void BeginTopmostRetry(Rect bounds, IntPtr foregroundBefore) { }
            public static void ClearTopmost() { }
            public static void SetTaskbarVisible(bool visible) { }
            public static void RestoreEditorFocus(IntPtr preferred) { }
#endif
        }
    }
}
#endif
