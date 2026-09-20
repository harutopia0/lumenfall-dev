using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TimeController : MonoBehaviour
{
    [Header("Slow Motion Settings")]
    [Range(0.01f, 1f)] [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private KeyCode slowMotionKey = KeyCode.T;

    private void Update()
    {
        bool isSlowMotion = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            isSlowMotion = Keyboard.current.tKey.isPressed;
        }
        else
        {
            isSlowMotion = Input.GetKey(slowMotionKey);
        }
#else
        isSlowMotion = Input.GetKey(slowMotionKey);
#endif

        if (isSlowMotion)
        {
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
