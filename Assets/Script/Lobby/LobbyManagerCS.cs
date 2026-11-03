using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ตัวจัดการระบบหลังบ้านหน้า Lobby แบบรวมศูนย์ (Centralized Lobby Manager)
/// - จัดการการเริ่มเกมใหม่ (StartNewGame) และโหลดเซฟ (LoadSaveGame) โดยส่งผ่านหน้า LoadingSceneCS
/// - บันทึกสถานะการส่งฉาก ("Lobby" -> "TargetScene") ลง DB (game_progress.json)
/// - จัดการเอฟเฟกต์สีปุ่มเมื่อเมาส์ชี้ (Hover) เป็นสีขาวอัตโนมัติทั่วทั้งหน้าล็อบบี้
/// - เชื่อมต่อกับ SettingManagerCS ในการปรับเสียงและจอแสดงผล
/// </summary>
public class LobbyManagerCS : MonoBehaviour
{
    [Header("1. Game Start Settings")]
    [Tooltip("ชื่อฉากที่ต้องการโหลดเข้าเล่นเกม (ค่าเริ่มต้น: LV00 หรือ LV01)")]
    [SerializeField] private string targetSceneName = "LV00";

    [Header("2. Loading Screen Settings")]
    [Tooltip("เปิดใช้งานการเปลี่ยนฉากผ่านหน้า Loading หลอกๆ หรือไม่")]
    [SerializeField] private bool useLoadingScene = true;

    [Tooltip("ชื่อฉากของหน้าโหลดดิ้งหลอกๆ (ค่าเริ่มต้น: LoadingScene)")]
    [SerializeField] private string loadingSceneName = "LoadingScene";

    [Header("3. Save System (Optional)")]
    [Tooltip("ปุ่มโหลดเซฟ (หากลากใส่ไว้ สคริปต์จะช่วยเปิด/ปิดปุ่มตามการมีอยู่ของไฟล์เซฟให้อัตโนมัติ)")]
    [SerializeField] private Button loadSaveButton = default;

    [Header("4. Button Hover Settings (เปลี่ยนสีปุ่มเป็นสีขาว)")]
    [Tooltip("เปิดใช้งานระบบเปลี่ยนสีปุ่มที่เมาส์ชี้เป็นสีขาวอัตโนมัติบนทุกปุ่มในหน้าล็อบบี้")]
    [SerializeField] private bool autoApplyButtonHoverWhite = true;

    [Tooltip("สีเมื่อเมาส์ชี้ที่ปุ่ม (ค่าเริ่มต้น: สีขาวบริสุทธิ์)")]
    [SerializeField] private Color buttonHoverColor = Color.white;

    [Header("5. Settings References (Optional)")]
    [Tooltip("Slider ปรับเสียง (สคริปต์จะดึงค่าที่เคยเซฟไว้มาใส่ให้ตอนเริ่ม)")]
    [SerializeField] private Slider volumeSlider = default;

    [Tooltip("Toggle ปรับเต็มจอ (สคริปต์จะดึงค่าที่เคยเซฟไว้มาใส่ให้ตอนเริ่ม)")]
    [SerializeField] private Toggle fullscreenToggle = default;

    private string saveFilePath;
    private const string VOLUME_KEY = "MasterVolume";
    private const string FULLSCREEN_KEY = "IsFullscreen";

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.dataPath, "..", "db", "player_health.json");

        // ตรวจสอบและติดตั้ง SettingManagerCS บน Lobby_Manager อัตโนมัติหากยังไม่มีในฉาก
        if (SettingManagerCS.Instance == null && FindFirstObjectByType<SettingManagerCS>() == null)
        {
            if (!TryGetComponent<SettingManagerCS>(out _))
            {
                gameObject.AddComponent<SettingManagerCS>();
                Debug.Log("[LobbyManager] ติดตั้ง SettingManagerCS บน Lobby_Manager อัตโนมัติเรียบร้อย");
            }
        }
    }

    private void Start()
    {
        // คืนค่าเวลาและการแสดงผลเมาส์
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // อัปเดตสถานะปุ่มโหลดเซฟ
        if (loadSaveButton != null)
        {
            loadSaveButton.interactable = HasSaveData();
        }

        // โหลดและปรับการตั้งค่าเดิม
        float savedVol = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        AudioListener.volume = savedVol;
        if (volumeSlider != null) volumeSlider.value = savedVol;

        // ปิดการสั่ง Screen.fullScreen ซ้ำๆ ใน Start เพื่อป้องกัน macOS เด้งหลุดจากโหมดเต็มจอ
        // และซ่อน UI Fullscreen Toggle ไว้ก่อนชั่วคราว
        if (fullscreenToggle != null)
        {
            fullscreenToggle.gameObject.SetActive(false);
            if (fullscreenToggle.transform.parent != null && 
                fullscreenToggle.transform.parent.name.IndexOf("fullscreen", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fullscreenToggle.transform.parent.gameObject.SetActive(false);
            }
        }

        // ติดตั้งเอฟเฟกต์สีปุ่มเป็นสีขาวเมื่อเมาส์ชี้ (Hover) บนทุกปุ่มในล็อบบี้
        if (autoApplyButtonHoverWhite)
        {
            ApplyHoverEffectToAllButtons();
        }
    }

    /// <summary>
    /// ติดตั้งคอมโพเนนต์ ButtonHoverEffectCS ให้กับทุกปุ่มในฉาก รวมถึงปุ่มในหน้าต่างที่ซ่อนอยู่ (เช่น SettingCanvas)
    /// </summary>
    public void ApplyHoverEffectToAllButtons()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;

                // 1. ปรับ ColorBlock ของ Button ให้ Highlighted และ Selected เป็นสีขาว
                ColorBlock cb = btn.colors;
                cb.highlightedColor = buttonHoverColor;
                cb.selectedColor = buttonHoverColor;
                btn.colors = cb;

                // 2. ติดตั้งคอมโพเนนต์ ButtonHoverEffectCS หากยังไม่มี
                if (!btn.TryGetComponent<ButtonHoverEffectCS>(out var hoverEffect))
                {
                    hoverEffect = btn.gameObject.AddComponent<ButtonHoverEffectCS>();
                }

                if (hoverEffect != null)
                {
                    hoverEffect.SetHoverColor(buttonHoverColor);
                }
            }
        }
    }

    // ==========================================
    // 1. ระบบเริ่มเกมใหม่ (NEW GAME)
    // ==========================================
    /// <summary>
    /// ลบไฟล์เซฟเดิมเพื่อให้เริ่มใหม่ที่เลือด 3 ดวง บันทึกลง DB แล้วส่งผ่านหน้าโหลดดิ้งเข้าสู่ด่านเป้าหมาย
    /// </summary>
    public void StartNewGame()
    {
        Time.timeScale = 1f;
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[LobbyManager] ลบไฟล์เซฟเดิมเรียบร้อยแล้ว");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LobbyManager] ลบไฟล์เซฟไม่สำเร็จ: " + e.Message);
        }

        GameProgressManagerCS.DeleteProgressData();

        // บันทึกการส่งฉาก: จาก Lobby ไปยัง targetSceneName ลงใน DB
        GameProgressManagerCS.SaveSceneTransition("Lobby", targetSceneName);

        // โหลดฉากผ่าน Loading Scene
        LoadSceneThroughLoading(targetSceneName);
    }

    // ==========================================
    // 2. ระบบโหลดเซฟเกม (LOAD SAVE)
    // ==========================================
    /// <summary>
    /// โหลดเข้าเกมโดยใช้ข้อมูลเซฟเดิมที่มีอยู่ และส่งผ่านหน้าโหลดดิ้ง
    /// </summary>
    public void LoadSaveGame()
    {
        Time.timeScale = 1f;
        if (HasSaveData())
        {
            string sceneToLoad = GameProgressManagerCS.GetSavedSceneName(fallback: targetSceneName);
            Debug.Log($"[LobbyManager] โหลดเซฟเดิมเข้าเกม -> ด่าน {sceneToLoad}...");

            // บันทึกการส่งฉาก: จาก Lobby ไปยังด่านที่โหลดมาลงใน DB
            GameProgressManagerCS.SaveSceneTransition("Lobby", sceneToLoad);

            // โหลดฉากผ่าน Loading Scene
            LoadSceneThroughLoading(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] ไม่พบไฟล์เซฟเดิม!");
        }
    }

    /// <summary>
    /// ตรวจสอบว่ามีไฟล์เซฟอยู่ในระบบหรือไม่
    /// </summary>
    public bool HasSaveData()
    {
        try
        {
            return File.Exists(saveFilePath) || GameProgressManagerCS.HasProgressData();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// โหลดฉากตามชื่อที่ต้องการ โดยส่งผ่านหน้าโหลดดิ้ง
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;

        // บันทึกการส่งฉากลงใน DB
        GameProgressManagerCS.SaveSceneTransition("Lobby", sceneName);

        // โหลดฉากผ่าน Loading Scene
        LoadSceneThroughLoading(sceneName);
    }

    /// <summary>
    /// กลไกการโหลดฉาก: หากเปิดใช้งาน Loading Scene และมีฉากอยู่ในระบบ จะโหลดผ่าน LoadingSceneCS ก่อน
    /// </summary>
    private void LoadSceneThroughLoading(string destinationScene)
    {
        if (useLoadingScene && !string.IsNullOrEmpty(loadingSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(loadingSceneName))
            {
                Debug.Log($"[LobbyManager] ส่งผู้เล่นผ่านหน้าโหลดดิ้ง '{loadingSceneName}' -> ด่านเป้าหมาย: {destinationScene}");
                SceneManager.LoadScene(loadingSceneName);
                return;
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] ไม่พบฉาก '{loadingSceneName}' ใน Build Settings! กำลังโหลดเข้า '{destinationScene}' โดยตรง");
            }
        }

        SceneManager.LoadScene(destinationScene);
    }

    // ==========================================
    // 3. ระบบตั้งค่า (SETTINGS)
    // ==========================================
    /// <summary>
    /// ปรับระดับเสียง Master Volume (ผูกกับ OnValueChanged ของ Slider)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (SettingManagerCS.Instance != null)
        {
            SettingManagerCS.Instance.SetMasterVolume(volume);
        }
        else
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VOLUME_KEY, volume);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ปรับโหมดเต็มจอ (ผูกกับ OnValueChanged ของ Toggle)
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        if (SettingManagerCS.Instance != null)
        {
            SettingManagerCS.Instance.SetFullscreen(isFullscreen);
        }
        else
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // ==========================================
    // 4. ระบบออกจากเกม (QUIT GAME)
    // ==========================================
    /// <summary>
    /// ปิดเกม (รองรับทั้ง Unity Editor และ Standalone Build)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[LobbyManager] สั่งออกจากเกม");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
