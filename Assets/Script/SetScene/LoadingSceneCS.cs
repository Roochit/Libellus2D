using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ควบคุมหน้าโหลดดิ้งหลอกๆ (Fake Loading Scene) สไตล์ Command Line / Terminal Interface
/// - อ่านด่านที่เราอยู่ (currentScene) และด่านที่เราจะส่งไป (targetScene) จาก DB (game_progress.json)
/// - แสดงผลคอนโซลแบบ Terminal / Command Line พิมพ์ข้อความ Log และแถบ ASCII Progress Bar
/// - รองรับ UI แบบมาตรฐาน (Slider, Image Fill, TMP Text) ควบคู่กับโหมด Command Line
/// - มีระบบสร้าง Terminal Canvas อัตโนมัติหากยังไม่ได้จัด UI ไว้ในฉาก
/// - จำลองการโหลดแบบหลอกๆ (Fake Loading) พร้อมโหลดฉากจริงแบบ Asynchronous เบื้องหลัง
/// </summary>
public class LoadingSceneCS : MonoBehaviour
{
    [Header("Terminal / Command Line Mode")]
    [Tooltip("เปิดใช้งานโหมดแสดงผลหน้าโหลดแบบ Command Line Terminal สไตล์ Retro/Hacker")]
    [SerializeField] private bool useCommandLineMode = true;

    [Tooltip("TextMeshPro สำหรับแสดงข้อความในหน้าต่าง Command Line")]
    [SerializeField] private TMP_Text terminalOutputText;

    [Tooltip("ความเร็วในการพิมพ์ข้อความแต่ละบรรทัด (วินาที)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float terminalLineDelay = 0.16f;

    [Tooltip("เปิดใช้งาน Cursor กระพริบ (_) ที่ปลายบรรทัดคอนโซล")]
    [SerializeField] private bool enableBlinkingCursor = true;

    [Tooltip("ความยาวของช่อง ASCII Progress Bar ใน Command Line (จำนวนบล็อก)")]
    [SerializeField] private int asciiBarLength = 22;

    [Tooltip("สร้าง Terminal UI อัตโนมัติบนหน้าจอหากยังไม่ได้จัด UI ไว้ในฉาก LoadingScene")]
    [SerializeField] private bool autoBuildTerminalUI = true;

    [Tooltip("ข้อความหัวข้อระบบบนหน้าต่าง Command Line")]
    [SerializeField] private string systemHeader = "LIBELLUS-OS [Kernel v2.4.0-PROD] (x86_64-libellus-core)";

    [Header("UI Displays - Standard UI (Optional)")]
    [Tooltip("แสดงชื่อด่านที่เราอยู่ปัจจุบัน")]
    [SerializeField] private TMP_Text currentSceneText;

    [Tooltip("รูปแบบข้อความด่านปัจจุบัน เช่น 'ด่านปัจจุบัน: {0}' หรือ '{0}'")]
    [SerializeField] private string currentSceneFormat = "ด่านปัจจุบัน: {0}";

    [Tooltip("แสดงชื่อด่านปลายทางที่เรากำลังจะเดินทางไป")]
    [SerializeField] private TMP_Text targetSceneText;

    [Tooltip("รูปแบบข้อความด่านเป้าหมาย เช่น 'กำลังเดินทางสู่: {0}' หรือ 'Loading {0}...'")]
    [SerializeField] private string targetSceneFormat = "กำลังเดินทางสู่: {0}...";

    [Tooltip("แถบ Slider แสดงความคืบหน้าการโหลด (0 ถึง 1)")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("รูปภาพ Image ที่เปิด Image Type เป็น Filled (0 ถึง 1) กรณีไม่ใช้ Slider")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("ข้อความแสดงเปอร์เซ็นต์ เช่น 100%")]
    [SerializeField] private TMP_Text progressPercentageText;

    [Tooltip("ข้อความแสดงทิปแนะนำการเล่น")]
    [SerializeField] private TMP_Text tipText;

    [Header("Fake Loading Settings")]
    [Tooltip("ระยะเวลาการโหลดหลอกๆ ขั้นต่ำ (วินาที)")]
    [Range(0.5f, 10f)]
    [SerializeField] private float fakeLoadingDuration = 2.8f;

    [Tooltip("ต้องกดปุ่มใดๆ ก่อนเข้าสู่ด่านจริงหรือไม่เมื่อโหลดเสร็จ 100%")]
    [SerializeField] private bool waitForUserInput = false;

    [Tooltip("GameObject ข้อความแจ้งเตือนให้กดปุ่ม (เช่น 'Press Any Key to Continue')")]
    [SerializeField] private GameObject pressAnyKeyPrompt;

    [Tooltip("ด่านสำรองกรณีใน DB ไม่มีข้อมูลด่านเป้าหมาย")]
    [SerializeField] private string fallbackSceneName = "LV01";

    private string currentSceneName;
    private string targetSceneName;
    private bool isLoadingStarted = false;

    // สำหรับ Terminal Engine
    private StringBuilder terminalBuffer = new StringBuilder();
    private string activeProgressBarLine = "";
    private string promptStatusLine = "";
    private bool isBlinkingCursorOn = true;
    private Coroutine cursorCoroutine;

    private void Awake()
    {
        // ปรับเวลาให้เดินตามปกติเสมอเพื่อป้องกันเกมค้างจาก Time.timeScale = 0
        Time.timeScale = 1f;

        if (pressAnyKeyPrompt != null)
        {
            pressAnyKeyPrompt.SetActive(false);
        }

        // หากเปิดโหมด Command Line แต่ยังไม่มี Text ลากใส่ไว้ ให้สร้าง Terminal Canvas อัตโนมัติ
        if (useCommandLineMode && terminalOutputText == null && autoBuildTerminalUI)
        {
            TryBuildTerminalUI();
        }
    }

    private void Start()
    {
        if (!isLoadingStarted)
        {
            isLoadingStarted = true;
            InitializeLoadingScreen();

            if (enableBlinkingCursor && useCommandLineMode)
            {
                cursorCoroutine = StartCoroutine(CursorBlinkRoutine());
            }

            StartCoroutine(FakeLoadingRoutine());
        }
    }

    /// <summary>
    /// อ่านข้อมูลจาก DB และแสดงผลบน UI
    /// </summary>
    private void InitializeLoadingScreen()
    {
        // อ่านข้อมูลจาก DB ความคืบหน้าของเกม
        GameProgressData progressData = GameProgressManagerCS.LoadProgress();

        currentSceneName = !string.IsNullOrEmpty(progressData.currentScene) ? progressData.currentScene : fallbackSceneName;
        targetSceneName = !string.IsNullOrEmpty(progressData.targetScene) ? progressData.targetScene : fallbackSceneName;

        // อัปเดต UI ข้อความทั่วไป
        if (currentSceneText != null)
        {
            currentSceneText.text = string.Format(currentSceneFormat, currentSceneName);
        }

        if (targetSceneText != null)
        {
            targetSceneText.text = string.Format(targetSceneFormat, targetSceneName);
        }

        UpdateProgressVisuals(0f);

        Debug.Log($"[LoadingScene] โหลดหน้า Loading: จากด่าน '{currentSceneName}' -> กำลังไปด่าน '{targetSceneName}'");
    }

    /// <summary>
    /// Coroutine สำหรับจำลองการโหลดแบบหลอกๆ และโหลดฉากจริงเบื้องหลัง
    /// </summary>
    private IEnumerator FakeLoadingRoutine()
    {
        // ตรวจสอบว่ามีฉากเป้าหมายอยู่ใน Build Settings หรือไม่
        bool canLoadAsync = Application.CanStreamedLevelBeLoaded(targetSceneName);
        AsyncOperation asyncOp = null;

        if (canLoadAsync)
        {
            asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }
        }
        else
        {
            Debug.LogWarning($"[LoadingScene] ไม่พบฉาก '{targetSceneName}' ใน Build Settings! กำลังเตรียมใช้ด่านสำรอง: {fallbackSceneName}");
            if (Application.CanStreamedLevelBeLoaded(fallbackSceneName))
            {
                targetSceneName = fallbackSceneName;
                asyncOp = SceneManager.LoadSceneAsync(fallbackSceneName);
                if (asyncOp != null)
                {
                    asyncOp.allowSceneActivation = false;
                }
            }
        }

        // รายการข้อความขั้นตอนแบบ Command Line Console
        List<string> bootSequenceLogs = new List<string>
        {
            $"<color=#66FF66>=== {systemHeader} ===</color>",
            "> [SYS_INIT] Mounting filesystem /dev/sda1 ... <color=#00FFCC>[OK]</color>",
            "> [DB_SYSCALL] Reading 'db/game_progress.json' ... <color=#00FFCC>[LOADED]</color>",
            $"> [SECTOR_ORIGIN] Current Level: <color=#FFFF00>{currentSceneName}</color>",
            $"> [SECTOR_TARGET] Destination: <color=#00E5FF>{targetSceneName}</color>",
            "> [DB_SYSCALL] Validating profile in 'db/player_health.json' ... <color=#00FFCC>[OK]</color>",
            "> [MEMORY] Allocating scene buffer blocks & physics matrix...",
            "> [STREAM_LOAD] Transferring asset packages & shaders to GPU...",
        };

        // เริ่มต้นแสดงผล Command Line ขั้นต้น
        if (useCommandLineMode)
        {
            foreach (string log in bootSequenceLogs)
            {
                AppendTerminalLine(log);
                yield return new WaitForSecondsRealtime(terminalLineDelay);
            }
        }

        float elapsedTime = 0f;

        // จำลองแถบโหลดดิ้งให้ค่อยๆ เพิ่มขึ้นจนครบตามเวลา fakeLoadingDuration
        while (elapsedTime < fakeLoadingDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float fakeProgress = Mathf.Clamp01(elapsedTime / fakeLoadingDuration);
            float realProgress = (asyncOp != null) ? Mathf.Clamp01(asyncOp.progress / 0.9f) : 1f;
            float displayProgress = Mathf.Min(fakeProgress, realProgress);

            UpdateProgressVisuals(displayProgress);

            if (useCommandLineMode)
            {
                UpdateTerminalProgressBar(displayProgress);
            }

            yield return null;
        }

        // รอจนกระทั่ง AsyncOperation โหลดเสร็จจริงในเบื้องหลัง
        if (asyncOp != null)
        {
            while (asyncOp.progress < 0.9f)
            {
                yield return null;
            }
        }

        // แสดงผล 100% เต็ม
        UpdateProgressVisuals(1f);

        if (useCommandLineMode)
        {
            UpdateTerminalProgressBar(1f);
            AppendTerminalLine("> [STREAM_LOAD] Scene assets decompressed & compiled. <color=#00FFCC>[COMPLETE]</color>");
            AppendTerminalLine($"> [STATUS] Sector jump matrix to <color=#00E5FF>{targetSceneName}</color> verified. <color=#00FF66>[READY]</color>");
        }

        // หากเปิดใช้งานให้รอผู้เล่นกดปุ่ม
        if (waitForUserInput)
        {
            if (pressAnyKeyPrompt != null)
            {
                pressAnyKeyPrompt.SetActive(true);
            }

            if (useCommandLineMode)
            {
                promptStatusLine = $"\n<color=#FFFF00>> [EXECUTE] PRESS ANY KEY OR [SPACE] TO ENTER {targetSceneName}...</color>";
                RefreshTerminalDisplay();
            }

            yield return null; // หน่วง 1 เฟรมเพื่อป้องกันการตรวจจับปุ่มก่อนหน้าค้าง

            while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0))
            {
                yield return null;
            }
        }
        else
        {
            if (useCommandLineMode)
            {
                promptStatusLine = $"\n<color=#66FF66>> [BOOT] Access granted. Transporting to {targetSceneName} now...</color>";
                RefreshTerminalDisplay();
            }
            yield return new WaitForSecondsRealtime(0.35f);
        }

        // บันทึกว่าผู้เล่นได้เข้าสู่ด่านเป้าหมายแล้ว
        GameProgressManagerCS.SaveCurrentScene(targetSceneName);

        // หยุด Cursor กระพริบก่อนเปลี่ยนฉาก
        if (cursorCoroutine != null)
        {
            StopCoroutine(cursorCoroutine);
        }

        // สลับเข้าสู่ฉากเป้าหมาย
        if (asyncOp != null)
        {
            asyncOp.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>
    /// อัปเดตการแสดงผล UI แถบความคืบหน้าแบบมาตรฐาน
    /// </summary>
    private void UpdateProgressVisuals(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = progress;
        }

        if (progressPercentageText != null)
        {
            progressPercentageText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }

    // ==========================================
    // COMMAND LINE / TERMINAL ENGINE
    // ==========================================

    private void AppendTerminalLine(string line)
    {
        if (terminalBuffer.Length > 0)
        {
            terminalBuffer.AppendLine();
        }
        terminalBuffer.Append(line);
        RefreshTerminalDisplay();
    }

    private void UpdateTerminalProgressBar(float progress)
    {
        progress = Mathf.Clamp01(progress);
        int filled = Mathf.RoundToInt(progress * asciiBarLength);
        int empty = asciiBarLength - filled;

        string bar = new string('=', filled) + (filled < asciiBarLength ? ">" : "") + new string(' ', Mathf.Max(0, empty - (filled < asciiBarLength ? 1 : 0)));
        int percentage = Mathf.RoundToInt(progress * 100f);

        activeProgressBarLine = $"\n> [TRANSFER] [{bar}] {percentage}%";
        RefreshTerminalDisplay();
    }

    private void RefreshTerminalDisplay()
    {
        if (terminalOutputText == null) return;

        string cursor = (enableBlinkingCursor && isBlinkingCursorOn) ? "<color=#00FF66>_</color>" : " ";
        terminalOutputText.text = terminalBuffer.ToString() + activeProgressBarLine + promptStatusLine + cursor;
    }

    private IEnumerator CursorBlinkRoutine()
    {
        while (true)
        {
            isBlinkingCursorOn = !isBlinkingCursorOn;
            RefreshTerminalDisplay();
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    /// <summary>
    /// สร้างหน้าต่าง Command Line UI อัตโนมัติหากในฉาก LoadingScene ยังไม่มี UI ใดๆ
    /// </summary>
    private void TryBuildTerminalUI()
    {
        // ตรวจสอบ EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // ค้นหาหรือสร้าง Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TerminalCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // สร้างพื้นหลังสีดำสนิทสไตล์ CRT Terminal
        GameObject bgObj = new GameObject("TerminalBackground");
        bgObj.transform.SetParent(canvas.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.05f, 0.06f, 1f); // ดำด้านสไตล์หน้าจอคอมพิวเตอร์
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // สร้างกรอบหน้าต่างคอนโซล (Console Window Box)
        GameObject consoleBox = new GameObject("ConsoleBox");
        consoleBox.transform.SetParent(bgObj.transform, false);
        Image boxImage = consoleBox.AddComponent<Image>();
        boxImage.color = new Color(0.08f, 0.10f, 0.12f, 0.95f);
        RectTransform boxRect = consoleBox.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.08f, 0.08f);
        boxRect.anchorMax = new Vector2(0.92f, 0.92f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        // เส้นขอบคอนโซลบน (Header Bar)
        GameObject headerBar = new GameObject("HeaderBar");
        headerBar.transform.SetParent(consoleBox.transform, false);
        Image headerImage = headerBar.AddComponent<Image>();
        headerImage.color = new Color(0.12f, 0.18f, 0.22f, 1f);
        RectTransform headerRect = headerBar.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.93f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        // ข้อความบนแถบ Header Bar
        GameObject headerTitle = new GameObject("HeaderTitle");
        headerTitle.transform.SetParent(headerBar.transform, false);
        TextMeshProUGUI headerText = headerTitle.AddComponent<TextMeshProUGUI>();
        headerText.text = "● ● ●  COMMAND TERMINAL -- ROOT SESSION";
        headerText.fontSize = 20;
        headerText.color = new Color(0.7f, 0.85f, 0.9f, 1f);
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.margin = new Vector4(20, 0, 0, 0);
        RectTransform headerTitleRect = headerTitle.GetComponent<RectTransform>();
        headerTitleRect.anchorMin = Vector2.zero;
        headerTitleRect.anchorMax = Vector2.one;
        headerTitleRect.offsetMin = Vector2.zero;
        headerTitleRect.offsetMax = Vector2.zero;

        // สร้าง Text สำหรับพิมพ์ผลลัพธ์ Command Line
        GameObject terminalTextObj = new GameObject("TerminalText");
        terminalTextObj.transform.SetParent(consoleBox.transform, false);
        TextMeshProUGUI tmp = terminalTextObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22;
        tmp.color = new Color(0.2f, 1.0f, 0.4f, 1f); // สีเขียว Matrix Terminal
        tmp.lineSpacing = 16f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.margin = new Vector4(30, 30, 30, 30);

        RectTransform textRect = terminalTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = new Vector2(1f, 0.93f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        terminalOutputText = tmp;
        Debug.Log("[LoadingScene] สร้าง Dynamic Command Line Terminal UI สำเร็จเรียบร้อย");
    }

    public string CurrentSceneName => currentSceneName;
    public string TargetSceneName => targetSceneName;
}
