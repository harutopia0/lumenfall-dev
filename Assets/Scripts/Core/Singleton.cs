using UnityEngine;

namespace Lumenfall.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T instance;
        private static readonly object lockObj = new object();

        public static T Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = Object.FindAnyObjectByType<T>();
                    }
                    return instance;
                }
            }
            protected set => instance = value;
        }

        public static bool HasInstance => instance != null;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
