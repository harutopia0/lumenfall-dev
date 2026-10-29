using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lumenfall.Core
{
    public class PoolManager : PersistentSingleton<PoolManager>
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 10;

        public void Prewarm(GameObject prefab, int count = 0)
        {
            if (prefab == null) return;
            int targetCount = count > 0 ? count : defaultPoolSize;

            if (!poolDictionary.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                poolDictionary[prefab] = queue;
            }

            for (int i = 0; i < targetCount; i++)
            {
                GameObject obj = CreateNewInstance(prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!poolDictionary.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                poolDictionary[prefab] = queue;
            }

            GameObject obj = null;
            while (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj != null) break;
            }

            if (obj == null)
            {
                obj = CreateNewInstance(prefab);
            }

            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawnFromPool();
            }

            return obj;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (!instanceToPrefabMap.TryGetValue(instance, out var prefab))
            {
                Destroy(instance);
                return;
            }

            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnReturnToPool();
            }

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);

            if (!poolDictionary.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                poolDictionary[prefab] = queue;
            }

            queue.Enqueue(instance);
        }

        public void Despawn(GameObject instance, float delay)
        {
            if (instance == null) return;
            StartCoroutine(DespawnRoutine(instance, delay));
        }

        private IEnumerator DespawnRoutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(instance);
        }

        private GameObject CreateNewInstance(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            PooledObject pooled = obj.GetComponent<PooledObject>();
            if (pooled == null)
            {
                pooled = obj.AddComponent<PooledObject>();
            }
            pooled.SourcePrefab = prefab;
            instanceToPrefabMap[obj] = prefab;
            return obj;
        }

        public void ClearPools()
        {
            foreach (var kvp in poolDictionary)
            {
                while (kvp.Value.Count > 0)
                {
                    GameObject obj = kvp.Value.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            poolDictionary.Clear();
            instanceToPrefabMap.Clear();
        }
    }
}
