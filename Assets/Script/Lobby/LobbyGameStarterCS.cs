using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// จัดการระบบการเริ่มเกมและการโหลดเซฟ (ข้อ 1 และ ข้อ 2)
/// - ผูกฟังก์ชัน StartNewGame() เข้ากับ OnClick ของปุ่ม "เริ่มเกมใหม่"
/// - ผูกฟังก์ชัน LoadSaveGame() เข้ากับ OnClick ของปุ่ม "โหลดเซฟ / เล่นต่อ"
/// </summary>
public class LobbyGameStarterCS : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อฉากที่ต้องการโหลดเข้าเล่นเกม (ค่าเริ่มต้นคือ LV01)")]
    [SerializeField] private string targetSceneName = "LV01";

    [Header("UI Reference (Optional)")]
    [Tooltip("ปุ่มโหลดเซฟ (หากลากใส่ไว้ สคริปต์จะช่วยปิดการกดปุ่มให้อัตโนมัติหากไม่มีไฟล์เซฟ)")]
    [SerializeField] private Button loadSaveButton;

    private string saveFilePath;

    private void Awake()
    {
        // ที่อยู่ไฟล์เซฟเดียวกับ PlayerHealthCS
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
    /// 1. ปุ่มเริ่มเกมใหม่ (New Game): ลบไฟล์เซฟเดิมเพื่อให้เริ่มใหม่ที่เลือด 3 ดวง แล้วโหลดฉาก
    /// </summary>
    public void StartNewGame()
    {
        Time.timeScale = 1f;
        DeleteSaveData();
        GameProgressManagerCS.DeleteProgressData();
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 2. ปุ่มโหลดเซฟเกม (Load / Continue Game): โหลดเข้าฉากโดยใช้ข้อมูลเลือดและด่านล่าสุดจากเซฟเดิม
    /// </summary>
    public void LoadSaveGame()
    {
        Time.timeScale = 1f;
        if (HasSaveData())
        {
            string sceneToLoad = GameProgressManagerCS.GetSavedSceneName(fallback: targetSceneName);
            Debug.Log($"[LobbyGameStarter] พบไฟล์เซฟเดิม -> กำลังโหลดเข้าด่าน {sceneToLoad}...");
            SceneManager.LoadScene(sceneToLoad);
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
        SceneManager.LoadScene(sceneName);
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
