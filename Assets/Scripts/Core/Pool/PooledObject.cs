using UnityEngine;

namespace Lumenfall.Core
{
    public class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; internal set; }

        public void ReturnToPool()
        {
            if (PoolManager.HasInstance)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
