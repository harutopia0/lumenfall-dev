#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Lumenfall.Editor
{
    [InitializeOnLoad]
    public static class EditorWindowUnmaximizer
    {
        static EditorWindowUnmaximizer()
        {
            EditorApplication.delayCall += RestoreAllMaximizedWindows;
            EditorApplication.update += OnceUpdate;
        }

        private static int counter = 0;
        private static void OnceUpdate()
        {
            counter++;
            RestoreAllMaximizedWindows();
            if (counter > 5)
            {
                EditorApplication.update -= OnceUpdate;
            }
        }

        public static void RestoreAllMaximizedWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.maximized)
                {
                    window.maximized = false;
                }
            }
        }
    }
}
#endif
