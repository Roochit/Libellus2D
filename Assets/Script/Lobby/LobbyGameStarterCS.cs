using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// จัดการระบบการเริ่มเกมและการโหลดเซฟ (ข้อ 1 และ ข้อ 2)
/// - ผูกฟังก์ชัน StartNewGame() เข้ากับ OnClick ของปุ่ม "เริ่มเกมใหม่"
/// - ผูกฟังก์ชัน LoadSaveGame() เข้ากับ OnClick ของปุ่ม "โหลดเซฟ / เล่นต่อ"
/// - ส่งผ่านหน้าโหลดดิ้งหลอกๆ (LoadingSceneCS) พร้อมบันทึกลง DB
/// </summary>
public class LobbyGameStarterCS : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อฉากที่ต้องการโหลดเข้าเล่นเกม (ค่าเริ่มต้นคือ LV00 หรือ LV01)")]
    [SerializeField] private string targetSceneName = "LV00";

    [Header("Loading Scene Settings")]
    [Tooltip("เปิดใช้งานการเปลี่ยนฉากผ่านหน้า Loading หลอกๆ หรือไม่")]
    [SerializeField] private bool useLoadingScene = true;

    [Tooltip("ชื่อฉากของหน้าโหลดดิ้งหลอกๆ (ค่าเริ่มต้น: LoadingScene)")]
    [SerializeField] private string loadingSceneName = "LoadingScene";

    [Header("UI Reference (Optional)")]
    [Tooltip("ปุ่มโหลดเซฟ (หากลากใส่ไว้ สคริปต์จะช่วยปิดการกดปุ่มให้อัตโนมัติหากไม่มีไฟล์เซฟ)")]
    [SerializeField] private Button loadSaveButton = default;

    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.dataPath, "..", "db", "player_health.json");
    }

    private void Start()
    {
        // คืนค่าเวลาและการแสดงผลเมาส์ในหน้าล็อบบี้
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // ตรวจสอบไฟล์เซฟเพื่อเปิด/ปิดการทำงานของปุ่มโหลดเซฟ
        UpdateLoadButtonState();
    }

    /// <summary>
    /// 1. ปุ่มเริ่มเกมใหม่ (New Game): ลบไฟล์เซฟเดิม บันทึก DB แล้วส่งผ่านหน้าโหลดดิ้ง
    /// </summary>
    public void StartNewGame()
    {
        Time.timeScale = 1f;
        DeleteSaveData();
        GameProgressManagerCS.DeleteProgressData();

        // บันทึกการเปลี่ยนฉากลงใน DB
        GameProgressManagerCS.SaveSceneTransition("Lobby", targetSceneName);

        LoadSceneThroughLoading(targetSceneName);
    }

    /// <summary>
    /// 2. ปุ่มโหลดเซฟเกม (Load / Continue Game): โหลดเข้าฉากเดิมและส่งผ่านหน้าโหลดดิ้ง
    /// </summary>
    public void LoadSaveGame()
    {
        Time.timeScale = 1f;
        if (HasSaveData())
        {
            string sceneToLoad = GameProgressManagerCS.GetSavedSceneName(fallback: targetSceneName);
            Debug.Log($"[LobbyGameStarter] พบไฟล์เซฟเดิม -> กำลังโหลดเข้าด่าน {sceneToLoad}...");

            GameProgressManagerCS.SaveSceneTransition("Lobby", sceneToLoad);
            LoadSceneThroughLoading(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("[LobbyGameStarter] ไม่พบไฟล์เซฟเดิม! ไม่สามารถโหลดได้");
        }
    }

    /// <summary>
    /// โหลดฉากตามชื่อที่ระบุ (เผื่อใช้กับปุ่มเลือกด่านอื่นๆ เช่น LV02)
    /// </summary>
    public void LoadCustomScene(string sceneName)
    {
        Time.timeScale = 1f;
        GameProgressManagerCS.SaveSceneTransition("Lobby", sceneName);
        LoadSceneThroughLoading(sceneName);
    }

    private void LoadSceneThroughLoading(string destinationScene)
    {
        if (useLoadingScene && !string.IsNullOrEmpty(loadingSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(loadingSceneName))
            {
                Debug.Log($"[LobbyGameStarter] ส่งผู้เล่นผ่านหน้าโหลดดิ้ง '{loadingSceneName}' -> ด่าน: {destinationScene}");
                SceneManager.LoadScene(loadingSceneName);
                return;
            }
            else
            {
                Debug.LogWarning($"[LobbyGameStarter] ไม่พบฉาก '{loadingSceneName}' ใน Build Settings! กำลังโหลดเข้า '{destinationScene}' โดยตรง");
            }
        }

        SceneManager.LoadScene(destinationScene);
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
    /// อัปเดตสถานะการกดของปุ่มโหลดเซฟ (Interactable)
    /// </summary>
    public void UpdateLoadButtonState()
    {
        if (loadSaveButton != null)
        {
            loadSaveButton.interactable = HasSaveData();
        }
    }

    private void DeleteSaveData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[LobbyGameStarter] ลบไฟล์เซฟเดิมเรียบร้อยแล้ว");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LobbyGameStarter] ไม่สามารถลบไฟล์เซฟได้: " + e.Message);
        }
    }
}
