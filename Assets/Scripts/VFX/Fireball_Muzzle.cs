using UnityEngine;

public class Fireball_Muzzle : MonoBehaviour
{
    private Player player;

    public void Setup(Player playerRef)
    {
        player = playerRef;
    }

    public void SpawnFireballTrigger()
    {
        if (player != null)
        {
            player.SpawnFireballProjectile();
        }
    }
}
