using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ระบบเมนูหยุดเกมชั่วคราวระหว่างเล่น (In-Game Pause Menu)
/// - ดักจับการกดปุ่ม Escape เพื่อเปิด/ปิดหน้าต่างเมนู
/// - หยุดเวลาในเกมด้วย Time.timeScale = 0
/// - มีฟังก์ชันเบื้องหลังสำหรับผูกกับปุ่มใน Inspector:
///   1. ResumeGame() -> เล่นเกมต่อ
///   2. RestartCurrentStage() -> เริ่มด่านปัจจุบันใหม่
///   3. GoToLobby() -> กลับสู่หน้าล็อบบี้
///   4. QuitGame() -> ออกจากเกม
/// </summary>
public class PauseMenuCS : MonoBehaviour
{
    private static PauseMenuCS instance;
    public static PauseMenuCS Instance => instance;

    [Header("UI Reference")]
    [Tooltip("ลาก GameObject ของหน้าต่าง Pause Menu (Panel) มาใส่ที่นี่")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Settings")]
    [Tooltip("ชื่อฉากห้องล็อบบี้ (ค่าเริ่มต้น: Lobby)")]
    [SerializeField] private string lobbySceneName = "Lobby";

    [Tooltip("ค้นหาพาเนลชื่อ 'PauseMenuPanel' หรือ 'PauseMenu' อัตโนมัติหากไม่ได้ลากใส่")]
    [SerializeField] private bool autoFindPanel = true;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // ค้นหาพาเนลอัตโนมัติหากยังไม่ได้ลากใส่ใน Inspector
        if (pauseMenuPanel == null && autoFindPanel)
        {
            GameObject found = GameObject.Find("PauseMenuPanel");
            if (found == null) found = GameObject.Find("PauseMenu");
            if (found != null) pauseMenuPanel = found;
        }
    }

    private void Start()
    {
        // รับประกันว่าเริ่มเกมมา เวลาต้องเดินตามปกติ และหน้าต่างเมนูต้องซ่อนอยู่
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // หากผู้เล่นตายอยู่ (มีหน้าต่าง GameOver แสดง) จะไม่รับการกด ESC ของ Pause Menu
        if (GameOverManagerCS.Instance != null && GameOverManagerCS.Instance.IsGameOver)
        {
            return;
        }

        // ตรวจจับการกดปุ่ม ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// สลับสถานะเปิด/ปิดเมนูหยุดเกม
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// หยุดเกมชั่วคราวและเปิดหน้าต่างเมนู
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // หยุดเวลาในเกม

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        Debug.Log("[PauseMenu] หยุดเกม (Paused)");
    }

    /// <summary>
    /// เล่นเกมต่อและปิดหน้าต่างเมนู
    /// (ผูกเข้ากับ OnClick ของปุ่ม Resume)
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // ให้เวลาเดินตามปกติ

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Debug.Log("[PauseMenu] เล่นต่อ (Resumed)");
    }

    /// <summary>
    /// เริ่มด่านปัจจุบันใหม่
    /// (ผูกเข้ากับ OnClick ของปุ่ม Restart)
    /// </summary>
    public void RestartCurrentStage()
    {
        Time.timeScale = 1f;
        isPaused = false;

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// กลับสู่หน้าล็อบบี้
    /// (ผูกเข้ากับ OnClick ของปุ่ม Lobby / Main Menu)
    /// </summary>
    public void GoToLobby()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(lobbySceneName);
    }

    /// <summary>
    /// ออกจากเกม
    /// (ผูกเข้ากับ OnClick ของปุ่ม Quit)
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // ป้องกันเวลาค้างเมื่อเปลี่ยนฉาก
        Time.timeScale = 1f;
    }
}
