using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// โครงสร้างข้อมูลสำหรับแต่ละรอบที่เล่นจบเกม (Clear Record)
/// </summary>
[System.Serializable]
public class EndGameClearRecord
{
    public int clearIndex = 1;
    public string timestamp = "";
    public int currentHearts = 3;
    public int maxHearts = 3;
    public int totalStagesCleared = 0;
    public List<string> clearedStages = new List<string>();
    public List<string> defeatedBosses = new List<string>();
}

/// <summary>
/// โครงสร้างข้อมูลภาพรวมการจบเกม บันทึกลงใน db/EndGame_Log.json
/// </summary>
[System.Serializable]
public class EndGameLogData
{
    public bool gameCompleted = true;
    public string lastCompletionTimestamp = "";
    public int totalClearCount = 1;
    public int finalCurrentHearts = 3;
    public int finalMaxHearts = 3;
    public List<string> clearedStages = new List<string>();
    public List<string> defeatedBosses = new List<string>();
    public List<EndGameClearRecord> clearHistory = new List<EndGameClearRecord>();
}

/// <summary>
/// ควบคุมการเล่นวิดีโอฉากจบ (End Credit)
/// - เล่นวิดีโอ End Credit และสลับไปยังฉากถัดไป (เช่น Lobby) เมื่อวิดีโอจบหรือหมดเวลา
/// - บันทึกข้อมูลสถิติการเล่นจบเกมลงใน db/EndGame_Log.json อัตโนมัติ
/// </summary>
public class EndCreditCS : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อฉากที่จะโหลดไปหลังจากเล่น End Credit จบ (ค่าเริ่มต้น: Lobby)")]
    [SerializeField] private string nextSceneName = "Lobby";

    [Header("Timeout Settings")]
    [Tooltip("ระยะเวลาการเล่นวิดีโอสูงสุดก่อนตัดเข้าฉากถัดไป (วินาที)")]
    [SerializeField] private float timeLimit = 20.00f;

    [Header("Skip Settings")]
    [Tooltip("อนุญาตให้กดปุ่มใดๆ (เช่น Spacebar / Escape / คลิกเมาส์) เพื่อข้าม End Credit ได้หรือไม่")]
    [SerializeField] private bool allowSkipWithInput = false;

    [Header("End Game Log Settings")]
    [Tooltip("บันทึกข้อมูลการจบเกมลง db/EndGame_Log.json อัตโนมัติเมื่อเข้าฉากนี้")]
    [SerializeField] private bool autoRecordEndGameLog = true;

    private VideoPlayer videoPlayer;
    private float timer = 0f;
    private bool isChangingScene = false;
    private bool isLogRecorded = false;

    void Start()
    {
        // 1. บันทึกข้อมูลการจบเกมลงใน db/EndGame_Log.json ทันทีเมื่อเข้าสู่ฉาก End Credit
        if (autoRecordEndGameLog)
        {
            RecordEndGameLog();
        }

        // 2. จัดการ VideoPlayer
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Debug.LogError("[EndCreditCS] ไม่พบ VideoPlayer Component ใน GameObject นี้!");
        }
    }

    void Update()
    {
        if (!isChangingScene)
        {
            timer += Time.deltaTime;

            // ตรวจสอบการกดปุ่มเพื่อข้าม (ถ้าเปิดใช้งาน)
            if (allowSkipWithInput && timer >= 1.0f) // รอ 1 วินาทีแรกเพื่อป้องกันการกดข้ามโดยไม่ตั้งใจ
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    Debug.Log("[EndCreditCS] ผู้เล่นกดข้าม End Credit");
                    ChangeScene();
                    return;
                }
            }

            // ตรวจสอบเวลาหมด
            if (timer >= timeLimit)
            {
                ChangeScene();
            }
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        ChangeScene();
    }

    /// <summary>
    /// สลับเข้าสู่ฉากถัดไป (เช่น Lobby)
    /// </summary>
    void ChangeScene()
    {
        if (isChangingScene) return;
        isChangingScene = true;

        // รับประกันว่าได้บันทึก Log เรียบร้อยก่อนเปลี่ยนฉาก
        if (!isLogRecorded && autoRecordEndGameLog)
        {
            RecordEndGameLog();
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[EndCreditCS] กรุณาใส่ชื่อ Next Scene Name ใน Inspector ด้วยครับ");
        }
    }

    /// <summary>
    /// บันทึกสถิติและข้อมูลการจบเกมลงใน db/EndGame_Log.json
    /// </summary>
    public void RecordEndGameLog()
    {
        if (isLogRecorded) return;
        isLogRecorded = true;

        SaveEndGameLogToFile();
    }

    /// <summary>
    /// ฟังก์ชัน Static สำหรับบันทึกข้อมูลจบเกมลงไฟล์ db/EndGame_Log.json
    /// สามารถเรียกใช้งานได้จากทุกที่
    /// </summary>
    public static void SaveEndGameLogToFile()
    {
        try
        {
            string dbDir = Path.Combine(Application.dataPath, "..", "db");
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            string logPath = Path.Combine(dbDir, "EndGame_Log.json");

            EndGameLogData logData = new EndGameLogData();

            // 1. ตรวจสอบว่ามีไฟล์เดิมอยู่หรือไม่ หากมีให้อ่านค่าเดิมเพื่อสะสมจำนวนรอบที่เล่นจบ (Total Clears)
            if (File.Exists(logPath))
            {
                try
                {
                    string existingJson = File.ReadAllText(logPath).Trim();
                    if (!string.IsNullOrEmpty(existingJson))
                    {
                        EndGameLogData existingData = JsonUtility.FromJson<EndGameLogData>(existingJson);
                        if (existingData != null)
                        {
                            logData = existingData;
                            logData.totalClearCount++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EndCreditCS] ไม่สามารถอ่านประวัติ Log เดิมได้ กำลังสร้างข้อมูลใหม่: {e.Message}");
                }
            }

            // 2. ดึงข้อมูลด่านที่ผ่านและบอสที่ปราบได้จาก GameProgressManagerCS
            GameProgressData progressData = GameProgressManagerCS.LoadProgress();
            logData.clearedStages = progressData != null && progressData.clearedStages != null
                ? new List<string>(progressData.clearedStages)
                : new List<string>();
            logData.defeatedBosses = progressData != null && progressData.defeatedBosses != null
                ? new List<string>(progressData.defeatedBosses)
                : new List<string>();

            // หากยังไม่มี End_Credit ใน clearedStages ให้เพิ่มเข้าไป
            if (!logData.clearedStages.Contains("End_Credit"))
            {
                logData.clearedStages.Add("End_Credit");
            }

            // 3. ดึงข้อมูลเลือดล่าสุดของผู้เล่นจาก player_health.json
            string healthPath = Path.Combine(dbDir, "player_health.json");
            int currentHearts = 3;
            int maxHearts = 3;

            if (File.Exists(healthPath))
            {
                try
                {
                    string healthJson = File.ReadAllText(healthPath);
                    PlayerHealthCS.SaveData healthData = JsonUtility.FromJson<PlayerHealthCS.SaveData>(healthJson);
                    if (healthData != null)
                    {
                        currentHearts = healthData.currentHearts;
                        maxHearts = healthData.maxHearts;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EndCreditCS] ไม่สามารถอ่านไฟล์ player_health.json ได้: {e.Message}");
                }
            }

            logData.finalCurrentHearts = currentHearts;
            logData.finalMaxHearts = maxHearts;
            logData.gameCompleted = true;
            logData.lastCompletionTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 4. สร้างบันทึกประวัติรอบการเล่นจบ (Clear Record)
            if (logData.clearHistory == null)
            {
                logData.clearHistory = new List<EndGameClearRecord>();
            }

            EndGameClearRecord newRecord = new EndGameClearRecord
            {
                clearIndex = logData.totalClearCount,
                timestamp = logData.lastCompletionTimestamp,
                currentHearts = currentHearts,
                maxHearts = maxHearts,
                totalStagesCleared = logData.clearedStages.Count,
                clearedStages = new List<string>(logData.clearedStages),
                defeatedBosses = new List<string>(logData.defeatedBosses)
            };

            logData.clearHistory.Add(newRecord);

            // 5. บันทึกลงใน db/EndGame_Log.json
            string jsonOutput = JsonUtility.ToJson(logData, true);
            File.WriteAllText(logPath, jsonOutput);

            Debug.Log($"[EndCreditCS] บันทึกข้อมูลการเล่นจบเกมลง '{logPath}' สำเร็จเรียบร้อย! (จบเกมรอบที่ {logData.totalClearCount})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EndCreditCS] เกิดข้อผิดพลาดในการบันทึก EndGame_Log.json: {ex.Message}");
        }
    }
}
