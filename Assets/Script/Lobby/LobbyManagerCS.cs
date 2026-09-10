using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ตัวจัดการระบบหลังบ้านหน้า Lobby แบบรวมศูนย์ (Zero-UI Generation)
/// มีฟังก์ชันพร้อมผูกเข้ากับ Event ใน Inspector ได้ทันที:
/// 1. StartNewGame() -> ปุ่มเริ่มเกมใหม่
/// 2. LoadSaveGame() -> ปุ่มโหลดเซฟเกม
/// 3. SetMasterVolume(float) & SetFullscreen(bool) -> การตั้งค่า
/// 4. QuitGame() -> ออกจากเกม
/// 
/// *หากต้องการแยก Component ติดตั้งเฉพาะส่วน สามารถใช้สคริปต์แยกย่อยได้:
/// - LobbyGameStarterCS (เริ่มเกม / โหลดเซฟ)
/// - LobbySettingsCS (การตั้งค่าเสียง / หน้าจอ)
/// - LobbyQuitCS (ออกจากเกม)
/// </summary>
public class LobbyManagerCS : MonoBehaviour
{
    [Header("1. Game Start Settings")]
    [Tooltip("ชื่อฉากที่ต้องการโหลดเข้าเล่นเกม (ค่าเริ่มต้น: LV01)")]
    [SerializeField] private string targetSceneName = "LV01";

    [Header("2. Save System (Optional)")]
    [Tooltip("ปุ่มโหลดเซฟ (หากลากใส่ไว้ สคริปต์จะช่วยเปิด/ปิดปุ่มตามการมีอยู่ของไฟล์เซฟให้อัตโนมัติ)")]
    [SerializeField] private Button loadSaveButton;

    [Header("3. Settings References (Optional)")]
    [Tooltip("Slider ปรับเสียง (สคริปต์จะดึงค่าที่เคยเซฟไว้มาใส่ให้ตอนเริ่ม)")]
    [SerializeField] private Slider volumeSlider;

    [Tooltip("Toggle ปรับเต็มจอ (สคริปต์จะดึงค่าที่เคยเซฟไว้มาใส่ให้ตอนเริ่ม)")]
    [SerializeField] private Toggle fullscreenToggle;

    private string saveFilePath;
    private const string VOLUME_KEY = "MasterVolume";
    private const string FULLSCREEN_KEY = "IsFullscreen";

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.dataPath, "..", "db", "player_health.json");
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

        bool isFull = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFull;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFull;
    }

    // ==========================================
    // 1. ระบบเริ่มเกมใหม่ (NEW GAME)
    // ==========================================
    /// <summary>
    /// ลบไฟล์เซฟเดิมเพื่อให้เริ่มใหม่ที่เลือด 3 ดวง แล้วโหลดฉากเป้าหมาย
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

        SceneManager.LoadScene(targetSceneName);
    }

    // ==========================================
    // 2. ระบบโหลดเซฟเกม (LOAD SAVE)
    // ==========================================
    /// <summary>
    /// โหลดเข้าเกมโดยใช้ข้อมูลเซฟเดิมที่มีอยู่
    /// </summary>
    public void LoadSaveGame()
    {
        Time.timeScale = 1f;
        if (HasSaveData())
        {
            Debug.Log("[LobbyManager] โหลดเซฟเดิมเข้าเกม...");
            SceneManager.LoadScene(targetSceneName);
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
            return File.Exists(saveFilePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// โหลดฉากตามชื่อที่ต้องการ
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ==========================================
    // 3. ระบบตั้งค่า (SETTINGS)
    // ==========================================
    /// <summary>
    /// ปรับระดับเสียง Master Volume (ผูกกับ OnValueChanged ของ Slider)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับโหมดเต็มจอ (ผูกกับ OnValueChanged ของ Toggle)
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
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
