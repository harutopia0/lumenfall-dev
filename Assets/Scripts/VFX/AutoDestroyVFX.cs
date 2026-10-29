using UnityEngine;
using Lumenfall.Core;

public class AutoDestroyVFX : MonoBehaviour, IPoolable
{
    private PooledObject pooledObject;
    private Animator anim;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        anim = GetComponent<Animator>();
    }

    public void DestroySelf()
    {
        if (pooledObject != null && PoolManager.HasInstance)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawnFromPool()
    {
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    public void OnReturnToPool()
    {
    }
}

