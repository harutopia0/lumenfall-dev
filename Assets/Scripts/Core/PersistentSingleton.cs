using UnityEngine;

namespace Lumenfall.Core
{
    public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();

            if (instance != this)
            {
                return;
            }

            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
