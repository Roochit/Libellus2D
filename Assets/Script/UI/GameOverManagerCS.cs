using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// จัดการระบบเมื่อผู้เล่นตาย (Game Over System)
/// - หยุดเวลาในเกม (Time.timeScale = 0)
/// - แสดงหน้าต่าง Game Over UI
/// - มีปุ่มเริ่มด่านใหม่ (Retry) และปุ่มกลับล็อบบี้ (Lobby)
/// - รองรับการกดคีย์ลัด: R เพื่อเริ่มใหม่ทันที
/// </summary>
public class GameOverManagerCS : MonoBehaviour
{
    private static GameOverManagerCS instance;
    public static GameOverManagerCS Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameOverManagerCS>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("GameOverManager");
                    instance = managerObj.AddComponent<GameOverManagerCS>();
                }
            }
            return instance;
        }
    }

    [Header("UI References (Optional - Auto-creates if null)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Settings")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private bool allowRestartShortcut = true;

    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;

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

        // ซ่อนหน้าต่าง Game Over ไว้ก่อนเมื่อเริ่มเกม
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // ผูกปุ่มเข้ากับฟังก์ชัน
        if (retryButton != null) retryButton.onClick.AddListener(RestartStage);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(GoToLobby);
    }

    private void Start()
    {
        // รับประกันว่าเริ่มฉากใหม่ เวลาต้องเดินตามปกติเสมอ
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!isGameOver) return;

        // คีย์ลัดเมื่อหน้า Game Over แสดงผลอยู่
        if (allowRestartShortcut)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartStage();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.L))
            {
                GoToLobby();
            }
        }
    }

    /// <summary>
    /// สั่งเปิดหน้าจอ Game Over และหยุดเกม
    /// </summary>
    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameOverManager] ผู้เล่นตาย! หยุดเกมและเปิดหน้าจอ Game Over");

        // 1. หยุดเวลาในเกม
        Time.timeScale = 0f;

        // 2. ปลดล็อกและแสดงเมาส์
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 3. เปิดการแสดงผล UI
        if (gameOverPanel == null)
        {
            // หากยังไม่มี UI ให้ค้นหาจาก Canvas หรือสร้างอัตโนมัติ
            TryFindOrBuildUI();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// ปุ่มเริ่มด่านใหม่ (Retry)
    /// </summary>
    public void RestartStage()
    {
        Debug.Log("[GameOverManager] เริ่มด่านใหม่...");

        // คืนค่าความเร็วเวลาก่อนโหลดฉากใหม่เสมอเพื่อป้องกันเกมค้าง
        Time.timeScale = 1f;
        isGameOver = false;

        // ลบไฟล์เซฟเพื่อให้ผู้เล่นเกิดใหม่ด้วยหัวใจเต็ม 3 ดวง
        string savePath = System.IO.Path.Combine(Application.dataPath, "..", "db", "player_health.json");
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                System.IO.File.Delete(savePath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameOverManager] ไม่สามารถลบไฟล์เซฟได้: " + e.Message);
            }
        }

        // โหลดฉากปัจจุบันใหม่
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// ปุ่มกลับห้องล็อบบี้ (Lobby)
    /// </summary>
    public void GoToLobby()
    {
        Debug.Log("[GameOverManager] กลับสู่หน้าล็อบบี้...");

        // คืนค่าความเร็วเวลาก่อนโหลดฉากใหม่
        Time.timeScale = 1f;
        isGameOver = false;

        // ลบไฟล์เซฟเพื่อให้เริ่มใหม่ที่ 3 หัวใจเมื่อเข้าเล่นใหม่
        string savePath = System.IO.Path.Combine(Application.dataPath, "..", "db", "player_health.json");
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                System.IO.File.Delete(savePath);
            }
            catch { }
        }

        if (Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogWarning($"[GameOverManager] ไม่พบฉาก '{lobbySceneName}' จึงโหลดฉากปัจจุบันแทน");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// ค้นหาหน้าต่าง Game Over จาก Canvas หรือสร้างขึ้นมาแบบไดนามิกพร้อมดีไซน์สวยงาม
    /// </summary>
    private void TryFindOrBuildUI()
    {
        // 1. ลองค้นหา GameObject ที่ชื่อ GameOver_UI ในฉาก
        Transform existingUI = null;
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas != null)
        {
            existingUI = canvas.transform.Find("GameOver_UI");
        }

        if (existingUI != null)
        {
            gameOverPanel = existingUI.gameObject;
            return;
        }

        // ตรวจสอบ EventSystem สำหรับการคลิกปุ่ม UI
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 2. หากไม่มี ให้สร้าง Dynamic Game Over UI ขึ้นมาบน Canvas
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameOverCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        BuildDynamicGameOverUI(canvas);
    }

    /// <summary>
    /// สร้าง UI Game Over อัตโนมัติที่มีดีไซน์สวยงาม ทันสมัย พร้อมปุ่ม Retry และ Lobby
    /// </summary>
    private void BuildDynamicGameOverUI(Canvas canvas)
    {
        // --- 1. Root Panel (Overlay Backdrop) ---
        GameObject panelObj = new GameObject("GameOver_UI", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.04f, 0.07f, 0.88f); // มืดโปร่งแสงแบบฟิล์มภาพยนตร์

        // --- 2. Card Container (ตรงกลาง) ---
        GameObject cardObj = new GameObject("CardContainer", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardObj.transform.SetParent(panelObj.transform, false);

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(540, 420);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.11f, 0.12f, 0.16f, 0.95f); // พื้นหลังการ์ดสีเทาเข้มหรู

        VerticalLayoutGroup layout = cardObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // --- 3. Title Text (GAME OVER) ---
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(460, 70);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "GAME OVER";
        titleTMP.fontSize = 54;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.96f, 0.26f, 0.21f, 1f); // แดงนีออนเด่นชัด
        titleText = titleTMP;

        // --- 4. Subtitle Text (ภาษาไทย) ---
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(cardObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.sizeDelta = new Vector2(460, 40);

        TextMeshProUGUI subTMP = subObj.GetComponent<TextMeshProUGUI>();
        subTMP.text = "คุณตายแล้ว... อย่าเพิ่งยอมแพ้!";
        subTMP.fontSize = 24;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.85f, 0.88f, 0.92f, 1f);
        subtitleText = subTMP;

        // --- 5. ปุ่มเริ่มด่านใหม่ (Retry Button) ---
        GameObject retryBtnObj = CreateButton(cardObj.transform, "RetryButton", "เริ่มด่านใหม่ (Retry) [R]", new Color(0.89f, 0.22f, 0.21f, 1f));
        retryButton = retryBtnObj.GetComponent<Button>();
        retryButton.onClick.AddListener(RestartStage);

        // --- 6. ปุ่มกลับล็อบบี้ (Lobby Button) ---
        GameObject lobbyBtnObj = CreateButton(cardObj.transform, "LobbyButton", "กลับหน้าล็อบบี้ (Lobby) [ESC]", new Color(0.24f, 0.28f, 0.36f, 1f));
        lobbyButton = lobbyBtnObj.GetComponent<Button>();
        lobbyButton.onClick.AddListener(GoToLobby);

        gameOverPanel = panelObj;
    }

    /// <summary>
    /// Helper สร้างปุ่มกด UI สไตล์โมเดิร์น
    /// </summary>
    private GameObject CreateButton(Transform parent, string name, string label, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(420, 56);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.15f;
        colors.pressedColor = bgColor * 0.85f;
        colors.selectedColor = bgColor;
        btn.colors = colors;

        // ข้อความบนปุ่ม
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTMP = textObj.GetComponent<TextMeshProUGUI>();
        btnTMP.text = label;
        btnTMP.fontSize = 22;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;

        return btnObj;
    }

    private void OnDestroy()
    {
        // ป้องกันเวลาค้างเมื่อเปลี่ยนฉาก
        Time.timeScale = 1f;
    }
}
