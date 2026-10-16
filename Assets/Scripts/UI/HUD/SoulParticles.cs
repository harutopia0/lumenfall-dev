using UnityEngine;
using UnityEngine.VFX;

public class SoulParticles : MonoBehaviour
{
    public RectTransform uiElement;
    public VisualEffect vfxGraphObject;

    public void Spawn()
    {
        if (uiElement == null || vfxGraphObject == null) return;

        Vector3 worldCenter = GetRectTransformWorldCenter(uiElement);
        float worldWidth = uiElement.rect.width * uiElement.lossyScale.x;
        float worldHeight = uiElement.rect.height * uiElement.lossyScale.y;
        Vector3 worldScale = new Vector3(worldWidth, worldHeight, 0);

        vfxGraphObject.SetVector3("spawnPosition", worldCenter);
        vfxGraphObject.SetVector3("worldScale", worldScale);
        vfxGraphObject.Play();
    }

    private Vector3 GetRectTransformWorldCenter(RectTransform rect)
    {
        Vector3 worldPosition = rect.TransformPoint(rect.rect.center);
        return worldPosition;
    }
}
