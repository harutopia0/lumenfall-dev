using UnityEngine;

public interface IKnockbackable
{
    void ReceiveKnockback(Vector2 knockback, float duration);
}
