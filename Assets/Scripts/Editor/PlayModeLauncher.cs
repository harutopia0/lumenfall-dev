#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeLauncher
{
    private const string PREF_TESTER_MODE = "Lumenfall_PlayLauncher_IsTesterMode";
    private const string CORE_SCENE_PATH = "Assets/Scenes/Core_Persistent.unity";
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

        HookGlobalKeyHandler();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (isDialogActive) return;
            isDialogActive = true;

            int choice = EditorUtility.DisplayDialogComplex(
                "Select Launch Mode (Lumenfall)",
                @"Choose play mode to launch:

- [Tester Mode]: Launches from Core_Persistent into borderless fullscreen, simulating a standalone build.

- [Editor Mode]: Enters directly into the currently active scene in Editor Game View.",
                "Tester Mode (Fullscreen)",
                "Cancel",
                "Editor Mode"
            );

            if (choice == 1)
            {
                EditorApplication.isPlaying = false;
                isDialogActive = false;
                return;
            }

            if (choice == 0)
            {
                EditorPrefs.SetBool(PREF_TESTER_MODE, true);
                SceneAsset coreScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CORE_SCENE_PATH);
                if (coreScene != null)
                {
                    EditorSceneManager.playModeStartScene = coreScene;
                }
            }
            else
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
        try
        {
            CloseFullscreenGameView();
            Platform.ClearTopmost();
        }
        catch { }
    }

    private static Type FindGameViewType()
    {
        return typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    }

    private static void OpenFullscreenGameView()
    {
        if (!EditorApplication.isPlaying) return;

        Type gameViewType = FindGameViewType();
        if (gameViewType == null)
        {
            Debug.LogError("[PlayModeLauncher] UnityEditor.GameView type not found!");
            return;
        }

        editorWindowBefore = Platform.GetForegroundWindowHandle();

        fullscreenWindow = (EditorWindow)ScriptableObject.CreateInstance(gameViewType);
        fullscreenWindow.titleContent = new GUIContent(POPUP_MARKER);

        SetShowToolbar(fullscreenWindow, false);

        Rect monitorBounds = Platform.GetFullscreenBounds();
        Rect editorPoints = ToEditorPoints(monitorBounds);

        fullscreenWindow.position = editorPoints;
        fullscreenWindow.ShowPopup();
        fullscreenWindow.Focus();

        Platform.PositionFullscreenWindow(monitorBounds, editorWindowBefore);
    }

    private static void CloseFullscreenGameView()
    {
        Platform.ClearTopmost();

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
                if (candidate != null && candidate.GetType() == gameViewType && candidate.titleContent != null && candidate.titleContent.text == POPUP_MARKER)
                {
                    try { candidate.Close(); } catch { }
                }
            }
        }

        Platform.RestoreEditorFocus(editorWindowBefore);
        editorWindowBefore = IntPtr.Zero;
    }

    private static void SetShowToolbar(EditorWindow window, bool value)
    {
        if (window == null) return;
        Type t = window.GetType();

        for (int depth = 0; depth < 3 && t != null; depth++, t = t.BaseType)
        {
            PropertyInfo prop = t.GetProperty("showToolbar",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (prop == null)
                prop = t.GetProperty("m_ShowToolbar", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

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

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

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

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

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

        public static void PositionFullscreenWindow(Rect bounds, IntPtr foregroundBefore)
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

                    try
                    {
                        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                        exStyle = (exStyle | WS_EX_APPWINDOW) & ~WS_EX_TOOLWINDOW;
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                    }
                    catch { }

                    SetWindowPos(hwnd, HWND_NOTOPMOST, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, SWP_SHOWWINDOW);
                    SetWindowPos(hwnd, HWND_TOP, (int)bounds.x, (int)bounds.y, (int)bounds.width, (int)bounds.height, SWP_SHOWWINDOW);

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
        public static void PositionFullscreenWindow(Rect bounds, IntPtr foregroundBefore) { }
        public static void ClearTopmost() { }
        public static void RestoreEditorFocus(IntPtr preferred) { }
#endif
    }
}
#endif
