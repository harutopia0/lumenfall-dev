using UnityEngine;
using UnityEngine.VFX;

public class SoulParticles : MonoBehaviour
{
    public RectTransform uiElement;
    public VisualEffect vfxGraphObject;

    private void Awake()
    {
        EnsureAttached();
    }

    private void EnsureAttached()
    {
        if (uiElement != null && vfxGraphObject != null)
        {
            if (vfxGraphObject.transform.parent != uiElement)
            {
                vfxGraphObject.transform.SetParent(uiElement, false);
            }
            vfxGraphObject.transform.localPosition = Vector3.zero;
            vfxGraphObject.transform.localRotation = Quaternion.identity;
            vfxGraphObject.transform.localScale = Vector3.one;
        }
    }

    public void Spawn()
    {
        if (uiElement == null || vfxGraphObject == null) return;
        EnsureAttached();

        Vector3 localScale = new Vector3(uiElement.rect.width, uiElement.rect.height, 0);

        vfxGraphObject.SetVector3("spawnPosition", Vector3.zero);
        vfxGraphObject.SetVector3("worldScale", localScale);
        vfxGraphObject.Play();
    }
}
