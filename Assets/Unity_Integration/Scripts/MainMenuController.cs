using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace StellaSora.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Transition Configuration")]
        [Tooltip("Ten Scene gameplay muc tieu se chuyen toi sau man hinh Menu")]
        [SerializeField] private string targetSceneName = "Moss_Grotto_01";

        [Tooltip("Ten Scene Core_Persistent chua Player, CameraController, UI...")]
        [SerializeField] private string persistentCoreSceneName = "Core_Persistent";

        [Header("Spawn Position Configuration")]
        [Tooltip("Ten GameObject cua SpawnPoint trong scene tiep theo (vi du: Spawn_Default)")]
        [SerializeField] private string targetSpawnPointName = "Spawn_Default";

        [Tooltip("Tuy chon: Nhap truc tiep toa do spawn thay vi tim GameObject theo ten")]
        [SerializeField] private bool useManualCoordinates = false;

        [Tooltip("Toa do spawn thu cong cua nhan vat khi bat useManualCoordinates")]
        [SerializeField] private Vector2 manualSpawnCoordinates = new Vector2(0f, 2.25f);

        [Header("UI References")]
        [Tooltip("Nut vo hinh phu toan man hinh de nhan cu nhap chuot o bat ky dau")]
        [SerializeField] private Button fullScreenButton;

        [Tooltip("Dong chu nhac nguoi choi cham vao man hinh (tuy chon)")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Tooltip("CanvasGroup man che den dung de Fade-out khi chuyen canh")]
        [SerializeField] private CanvasGroup screenFadeGroup;

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float promptBlinkSpeed = 2.0f;

        private bool isTransitioning = false;

        private void Awake()
        {
            if (fullScreenButton != null)
            {
                fullScreenButton.onClick.AddListener(OnScreenClicked);
            }

            if (screenFadeGroup != null)
            {
                screenFadeGroup.alpha = 0f;
                screenFadeGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (promptText != null && !isTransitioning)
            {
                float alpha = Mathf.PingPong(Time.time * promptBlinkSpeed, 0.7f) + 0.3f;
                Color col = promptText.color;
                col.a = alpha;
                promptText.color = col;
            }

            if (!isTransitioning)
            {
                // 1. Kiem tra phim Escape de thoat nhanh (ho tro ca New Input System va Legacy)
                bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                    escapePressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Escape))
                    escapePressed = true;
#endif
                if (escapePressed)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    return;
#else
                    Application.Quit();
                    return;
#endif
                }

                // 2. Kiem tra bat ky cu nhap chuot / cham man hinh / bam phim de vao game
                bool clickDetected = false;

#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    clickDetected = true;
                else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    clickDetected = true;
                else if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
                    clickDetected = true;
                else if (UnityEngine.InputSystem.Gamepad.current != null && (UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame || UnityEngine.InputSystem.Gamepad.current.startButton.wasPressedThisFrame))
                    clickDetected = true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetMouseButtonDown(0) || Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    clickDetected = true;
                }
#endif

                if (clickDetected)
                {
                    OnScreenClicked();
                }
            }
        }

        public void OnScreenClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            Debug.Log("[MainMenuController] Screen clicked! Transitioning to gameplay...");
            StartCoroutine(TransitionToGameRoutine());
        }

        private IEnumerator TransitionToGameRoutine()
        {
            if (fullScreenButton != null)
            {
                fullScreenButton.interactable = false;
            }

            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }

            if (screenFadeGroup != null)
            {
                screenFadeGroup.blocksRaycasts = true;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    screenFadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }
                screenFadeGroup.alpha = 1f;
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            // 1. Dam bao Core_Persistent duoc nap (Additive)
            bool isCoreLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == persistentCoreSceneName)
                {
                    isCoreLoaded = true;
                    break;
                }
            }

            if (!isCoreLoaded && !string.IsNullOrEmpty(persistentCoreSceneName))
            {
                AsyncOperation coreOp = SceneManager.LoadSceneAsync(persistentCoreSceneName, LoadSceneMode.Additive);
                while (coreOp != null && !coreOp.isDone) yield return null;
            }

            // 2. Nap Scene gameplay muc tieu (Additive)
            AsyncOperation roomOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            while (roomOp != null && !roomOp.isDone) yield return null;

            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.SetActiveScene(targetScene);
            }

            // 3. Dinh vi nhan vat (Player) tai vi tri SpawnPoint
            Vector2 spawnPos = manualSpawnCoordinates;
            bool foundSpawn = false;

            if (!useManualCoordinates && !string.IsNullOrEmpty(targetSpawnPointName) && targetScene.IsValid())
            {
                foreach (GameObject root in targetScene.GetRootGameObjects())
                {
                    Transform[] children = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        if (child.name == targetSpawnPointName || child.name.EndsWith("/" + targetSpawnPointName))
                        {
                            spawnPos = child.position;
                            foundSpawn = true;
                            break;
                        }
                    }
                    if (foundSpawn) break;
                }
            }

            if (!foundSpawn && !useManualCoordinates && targetScene.IsValid())
            {
                foreach (GameObject root in targetScene.GetRootGameObjects())
                {
                    SpawnPoint sp = root.GetComponentInChildren<SpawnPoint>(true);
                    if (sp != null)
                    {
                        spawnPos = sp.transform.position;
                        foundSpawn = true;
                        break;
                    }
                }
            }

            Player player = Object.FindAnyObjectByType<Player>();
            if (player != null)
            {
                player.transform.position = spawnPos;
                if (player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.position = spawnPos;
                    rb.linearVelocity = Vector2.zero;
                }
                Physics2D.SyncTransforms();
            }

            if (CameraController.Instance != null && targetScene.IsValid())
            {
                CameraLockArea fallbackArea = null;
                foreach (GameObject root in targetScene.GetRootGameObjects())
                {
                    CameraLockArea[] areas = root.GetComponentsInChildren<CameraLockArea>(true);
                    foreach (CameraLockArea area in areas)
                    {
                        if (fallbackArea == null) fallbackArea = area;
                        if (spawnPos.x >= area.MinX && spawnPos.x <= area.MaxX &&
                            spawnPos.y >= area.MinY && spawnPos.y <= area.MaxY)
                        {
                            fallbackArea = area;
                            break;
                        }
                    }
                }
                if (fallbackArea != null)
                {
                    CameraController.Instance.ForceSetArea(fallbackArea);
                }
            }

            // 4. Do bo scene MainMenu
            Scene menuScene = gameObject.scene;
            if (menuScene.IsValid() && menuScene.isLoaded && menuScene.name != targetSceneName)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(menuScene);
                while (unloadOp != null && !unloadOp.isDone) yield return null;
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
