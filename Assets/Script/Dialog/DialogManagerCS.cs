using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// โครงสร้างข้อมูลสำหรับแต่ละบรรทัดของบทสนทนา (Dialogue Line)
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("รูปภาพประจำตัว/กล่องบทสนทนา (Avatar / Dialog Box)")]
    public Sprite DialogBox;

    [Tooltip("รูปภาพกรอบชื่อตัวละคร (Name Box / Banner)")]
    public Sprite nameBox;

    [Tooltip("ชื่อตัวละครผู้พูด")]
    public string characterName;

    [TextArea(2, 5)]
    [Tooltip("เนื้อหาบทสนทนา")]
    public string dialogueText;
}

/// <summary>
/// ระบบจัดการบทสนทนาแบบรวมศูนย์ (Centralized Dialog Manager)
/// - รองรับการพิมพ์ข้อความแบบค่อยๆ โผล่ (Typewriter Effect)
/// - จัดการอักษรภาษาไทย (สระบน/ล่าง และวรรณยุกต์) ให้แสดงผลได้อย่างราบรื่นไม่กระตุก
/// - รองรับการกดข้าม (Skip) และส่งบรรทัดถัดไปด้วยปุ่ม R, Spacebar หรือคลิกซ้าย
/// - มีระบบ Auto-Find UI และ Null Safety ป้องกันข้อผิดพลาด NullReferenceException
/// </summary>
public class DialogManagerCS : MonoBehaviour
{
    private static DialogManagerCS instance;
    public static DialogManagerCS Instance => instance;

    [Header("UI Containers")]
    [Tooltip("GameObject ของ Canvas หรือ Panel ที่ครอบ UI บทสนทนาทั้งหมดไว้")]
    public GameObject dialogueCanvas;

    [Header("UI Elements")]
    [Tooltip("Text สำหรับแสดงชื่อตัวละคร")]
    public TextMeshProUGUI nameDisplay;

    [Tooltip("Text สำหรับแสดงเนื้อหาบทสนทนา")]
    public TextMeshProUGUI contentDisplay;

    [Tooltip("Image สำหรับแสดงรูปกล่องข้อความหรือภาพประจำตัว")]
    public Image BoxDisplay;

    [Tooltip("Image สำหรับแสดงรูปกรอบชื่อตัวละคร")]
    public Image nameBoxDisplay;

    [Header("Dialogue Sequence")]
    [Tooltip("รายการข้อความบทสนทนาที่ต้องการแสดง")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Settings")]
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษร (วินาทีต่อตัว) ค่าเริ่มต้น: 0.05")]
    public float typingSpeed = 0.05f;

    [Tooltip("ค้นหา UI ในฉากอัตโนมัติหากยังไม่ได้ลากใส่ใน Inspector")]
    [SerializeField] private bool autoFindUI = true;

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private float dialogueStartTime = 0f;

    private bool isTalking = false;

    /// <summary>
    /// สถานะว่ากำลังเปิดบทสนทนาอยู่หรือไม่ (รองรับการเรียกแบบ static)
    /// </summary>
    public static bool IsTalking
    {
        get => instance != null ? instance.isTalking : false;
        set
        {
            if (instance != null) instance.isTalking = value;
        }
    }

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

        if (autoFindUI)
        {
            AutoFindUIElements();
        }
    }

    private void Start()
    {
        // หากมีรายการข้อความที่ตั้งไว้ ให้เริ่มแสดงบทสนทนาทันที
        if (dialogueLines != null && dialogueLines.Count > 0)
        {
            StartDialogue();
        }
        else
        {
            Debug.LogWarning("[DialogManager] ยังไม่มีข้อความใน dialogueLines (หากต้องการทดสอบ ให้กรอกข้อความใน Inspector หรือคลิกขวาที่ Component แล้วเลือก 'Test Start Dialogue')");
        }
    }

    private void Update()
    {
        // ทำงานเฉพาะเมื่อบทสนทนากำลังเปิดอยู่
        if (!isTalking) return;

        // Debounce ป้องกันการกดคลิกผ่านเร็วเกินไปในเฟรมแรกที่เพิ่งเปิดบทสนทนา
        if (Time.unscaledTime - dialogueStartTime < 0.15f) return;

        // รับคำสั่งไปบรรทัดถัดไป หรือกดข้ามเอฟเฟกต์พิมพ์
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // หากกำลังพิมพ์อยู่ ให้ข้ามไปแสดงข้อความทั้งหมดของบรรทัดนี้ทันที
                CompleteCurrentLine();
            }
            else
            {
                // หากพิมพ์เสร็จแล้ว ให้ไปยังบรรทัดถัดไป
                NextLine();
            }
        }
    }

    /// <summary>
    /// ไปยังบทสนทนาบรรทัดถัดไป
    /// </summary>
    public void NextLine()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            EndDialogue();
            return;
        }

        if (currentIndex < dialogueLines.Count - 1)
        {
            currentIndex++;
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(ShowCurrentLine());
            Debug.Log($"[DialogManager] แสดงข้อความบรรทัดที่ {currentIndex}");
        }
        else
        {
            // จบบทสนทนา
            EndDialogue();
            Debug.Log("[DialogManager] จบบทสนทนาและปิด UI แล้ว");
        }
    }

    /// <summary>
    /// Coroutine แสดงข้อความและรูปภาพในบรรทัดปัจจุบัน พร้อมเอฟเฟกต์พิมพ์ทีละตัว
    /// </summary>
    private IEnumerator ShowCurrentLine()
    {
        if (dialogueLines == null || currentIndex < 0 || currentIndex >= dialogueLines.Count)
        {
            EndDialogue();
            yield break;
        }

        DialogueLine line = dialogueLines[currentIndex];
        if (line == null)
        {
            NextLine();
            yield break;
        }

        isTyping = true;

        // 1. จัดการการแสดงชื่อตัวละคร
        if (nameDisplay != null)
        {
            bool hasName = !string.IsNullOrEmpty(line.characterName);
            nameDisplay.gameObject.SetActive(hasName);
            nameDisplay.text = line.characterName ?? "";
        }

        // 2. จัดการรูปกล่องข้อความ / ภาพประจำตัว
        if (BoxDisplay != null)
        {
            if (line.DialogBox != null)
            {
                BoxDisplay.gameObject.SetActive(true);
                BoxDisplay.sprite = line.DialogBox;
            }
            else
            {
                BoxDisplay.gameObject.SetActive(false);
            }
        }

        // 3. จัดการรูปกรอบชื่อตัวละคร
        if (nameBoxDisplay != null)
        {
            if (line.nameBox != null)
            {
                nameBoxDisplay.gameObject.SetActive(true);
                nameBoxDisplay.sprite = line.nameBox;
            }
            else
            {
                nameBoxDisplay.gameObject.SetActive(false);
            }
        }

        // 4. จัดการข้อความบทสนทนา (พิมพ์ทีละตัวอักษร)
        if (contentDisplay != null)
        {
            string fullText = line.dialogueText ?? "";
            contentDisplay.text = fullText;
            contentDisplay.maxVisibleCharacters = 0;

            // บังคับให้ TextMeshPro คำนวณ Layout ล่วงหน้า
            contentDisplay.ForceMeshUpdate();

            // ตรวจสอบการใช้ฟอนต์ที่ไม่รองรับภาษาไทย
            if (contentDisplay.font != null && contentDisplay.font.name.Contains("LiberationSans") && ContainsThai(fullText))
            {
                Debug.LogWarning("[DialogManager] ข้อความเป็นภาษาไทย แต่ FontAsset ของ contentDisplay เป็น 'LiberationSans' ซึ่งไม่รองรับอักษรไทย กรุณาเปลี่ยนเป็น 'Sarabun-Regular SDF' ใน Inspector เพื่อให้แสดงผลได้ถูกต้อง");
            }

            int totalCharacters = contentDisplay.textInfo.characterCount;
            if (totalCharacters <= 0 && fullText.Length > 0)
            {
                totalCharacters = fullText.Length;
            }

            int visibleCount = 0;

            while (visibleCount <= totalCharacters)
            {
                contentDisplay.maxVisibleCharacters = visibleCount;

                // ตรวจสอบตัวอักษรภาษาไทย: หากตัวถัดไปเป็นสระบน/ล่าง หรือวรรณยุกต์ ให้แสดงพร้อมกันทันที ไม่ต้องรอ Wait
                while (visibleCount < fullText.Length && IsThaiCombiningChar(fullText[visibleCount]))
                {
                    visibleCount++;
                    contentDisplay.maxVisibleCharacters = visibleCount;
                }

                visibleCount++;

                if (typingSpeed > 0.001f)
                {
                    // ใช้ WaitForSecondsRealtime เพื่อให้เอฟเฟกต์พิมพ์ทำงานได้แม้ Time.timeScale = 0
                    yield return new WaitForSecondsRealtime(typingSpeed);
                }
                else
                {
                    yield return null;
                }
            }

            contentDisplay.maxVisibleCharacters = int.MaxValue;
        }
        else
        {
            Debug.LogError("[DialogManager] ไม่พบ TextMeshProUGUI (contentDisplay) สำหรับแสดงเนื้อหาบทสนทนา!");
        }

        isTyping = false;
        typingCoroutine = null;
    }

    /// <summary>
    /// ข้ามเอฟเฟกต์พิมพ์และแสดงข้อความทั้งหมดของบรรทัดปัจจุบันทันที
    /// </summary>
    public void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (contentDisplay != null)
        {
            contentDisplay.maxVisibleCharacters = int.MaxValue;
        }

        isTyping = false;
    }

    /// <summary>
    /// เริ่มแสดงบทสนทนาตั้งแต่บรรทัดแรก
    /// </summary>
    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("[DialogManager] ไม่สามารถเริ่มบทสนทนาได้เนื่องจากไม่มีข้อมูลใน dialogueLines");
            return;
        }

        isTalking = true;
        dialogueStartTime = Time.unscaledTime;
        currentIndex = 0;

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(ShowCurrentLine());
        Debug.Log($"[DialogManager] เริ่มแสดงบทสนทนา (จำนวน {dialogueLines.Count} บรรทัด)");
    }

    /// <summary>
    /// จบบทสนทนาและซ่อนหน้าต่าง UI
    /// </summary>
    public void EndDialogue()
    {
        isTalking = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueCanvas != null)
        {
            // หาก script นี้ติดอยู่บน dialogueCanvas โดยตรง ให้ปิดแค่ Canvas component หรือลูกพาเนล เพื่อไม่ให้ deactive ตัวสคริปต์เอง
            if (dialogueCanvas == gameObject)
            {
                Canvas cv = GetComponent<Canvas>();
                if (cv != null) cv.enabled = false;
            }
            else
            {
                dialogueCanvas.SetActive(false);
            }
        }

        Debug.Log("[DialogManager] จบบทสนทนาแล้ว");
    }

    /// <summary>
    /// สั่งให้เล่นบทสนทนาชุดใหม่ (สำหรับ Cutscene หรืออีเวนต์เฉพาะ)
    /// </summary>
    public void StartCutsceneDialogue(List<DialogueLine> newLines)
    {
        if (newLines == null || newLines.Count == 0)
        {
            Debug.LogWarning("[DialogManager] StartCutsceneDialogue: รายการข้อความที่ส่งมาว่างเปล่า");
            return;
        }

        dialogueLines = newLines;
        StartDialogue();
    }

    /// <summary>
    /// ค้นหา UI อัตโนมัติจากในฉากหากผู้ใช้ไม่ได้ลากใส่ใน Inspector
    /// </summary>
    private void AutoFindUIElements()
    {
        // 1. ค้นหา dialogueCanvas หากยังไม่ได้กำหนด
        if (dialogueCanvas == null)
        {
            Canvas localCanvas = GetComponentInChildren<Canvas>(true);
            if (localCanvas == null) localCanvas = GetComponentInParent<Canvas>(true);

            if (localCanvas != null)
            {
                dialogueCanvas = localCanvas.gameObject;
            }
            else
            {
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Canvas c in allCanvases)
                {
                    if (c == null) continue;
                    string cName = c.name.ToLower();
                    if (cName.Contains("dialog") || cName.Contains("talk"))
                    {
                        dialogueCanvas = c.gameObject;
                        break;
                    }
                }
            }
        }

        GameObject searchRoot = dialogueCanvas != null ? dialogueCanvas : gameObject;

        // 2. ค้นหา TextMeshProUGUI (nameDisplay, contentDisplay)
        TextMeshProUGUI[] texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            if (t == null) continue;
            string lowerName = t.name.ToLower();

            if (nameDisplay == null && (lowerName.Contains("name") || lowerName.Contains("character") || lowerName.Contains("ชื่อ")))
            {
                nameDisplay = t;
            }
            else if (contentDisplay == null && (lowerName.Contains("content") || lowerName.Contains("dialog") || lowerName.Contains("text") || lowerName.Contains("message") || lowerName.Contains("ข้อความ")))
            {
                contentDisplay = t;
            }
        }

        // หากยังหา contentDisplay ไม่เจอ ให้เลือกตัวแรกที่ไม่ได้เป็น nameDisplay
        if (contentDisplay == null && texts.Length > 0)
        {
            foreach (var t in texts)
            {
                if (t != nameDisplay)
                {
                    contentDisplay = t;
                    break;
                }
            }
        }

        // 3. ค้นหา Image (nameBoxDisplay, BoxDisplay)
        Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img == null) continue;
            string lowerName = img.name.ToLower();

            if (nameBoxDisplay == null && lowerName.Contains("name"))
            {
                nameBoxDisplay = img;
            }
            else if (BoxDisplay == null && (lowerName.Contains("box") || lowerName.Contains("dialog") || lowerName.Contains("avatar") || lowerName.Contains("face")))
            {
                BoxDisplay = img;
            }
        }
    }

    /// <summary>
    /// ตรวจสอบว่าอักขระเป็นสระบน/ล่าง หรือวรรณยุกต์ในภาษาไทยหรือไม่
    /// </summary>
    private bool IsThaiCombiningChar(char c)
    {
        return (c == '\u0E31') || 
               (c >= '\u0E34' && c <= '\u0E3A') || 
               (c >= '\u0E47' && c <= '\u0E4E');
    }

    /// <summary>
    /// ตรวจสอบว่าข้อความมีอักษรภาษาไทยหรือไม่
    /// </summary>
    private bool ContainsThai(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
        {
            if (c >= '\u0E01' && c <= '\u0E5B') return true;
        }
        return false;
    }

    /// <summary>
    /// เมนูคลิกขวาใน Inspector เพื่อทดสอบเริ่มบทสนทนาทันที
    /// </summary>
    [ContextMenu("Test Start Dialogue")]
    public void TestStartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            dialogueLines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    characterName = "ระบบ",
                    dialogueText = "สวัสดี! นี่คือข้อความทดสอบของระบบบทสนทนา (DialogManagerCS)"
                }
            };
        }
        StartDialogue();
    }
}
