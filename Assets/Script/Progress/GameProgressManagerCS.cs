using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// โครงสร้างข้อมูลความคืบหน้าของเกม (Game Progress Data)
/// บันทึกลงใน db/game_progress.json
/// </summary>
[System.Serializable]
public class GameProgressData
{
    public string currentScene = "LV01";               // ฉากล่าสุดที่ผู้เล่นอยู่
    public string highestUnlockedScene = "LV01";        // ด่านสูงสุดที่ปลดล็อกแล้ว
    public List<string> clearedStages = new List<string>(); // รายชื่อด่านที่เคลียร์แล้ว
    public List<string> defeatedBosses = new List<string>();// รายชื่อบอสที่ปราบได้แล้ว
    public string lastSaveTimestamp = "";               // เวลาที่เซฟล่าสุด
}

/// <summary>
/// จัดการฐานข้อมูลความคืบหน้าของเกม (Game Progress DB System)
/// - บันทึกและโหลดข้อมูลจาก db/game_progress.json
/// - สามารถเรียกใช้ผ่าน static functions ได้จากทุกที่ในโปรเจกต์
/// - มีตัวเลือก auto-save เมื่อเข้าสู่ด่านเล่นจริง
/// </summary>
public class GameProgressManagerCS : MonoBehaviour
{
    private static GameProgressManagerCS instance;
    public static GameProgressManagerCS Instance => instance;

    [Header("Settings")]
    [Tooltip("บันทึกด่านปัจจุบันลง DB อัตโนมัติเมื่อเข้าฉาก (ถ้าไม่ใช่หน้า Lobby)")]
    [SerializeField] private bool autoSaveActiveScene = true;

    private static string saveFilePath;

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

        InitPath();

        // บันทึกความคืบหน้าของด่านปัจจุบันอัตโนมัติหากเปิดใช้งาน
        if (autoSaveActiveScene)
        {
            AutoSaveCurrentScene();
        }
    }

    private static void InitPath()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            string dbDir = Path.Combine(Application.dataPath, "..", "db");
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            saveFilePath = Path.Combine(dbDir, "game_progress.json");
        }
    }

    private void AutoSaveCurrentScene()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        // ไม่บันทึกถ้าเป็นหน้า Lobby หรือฉากทดสอบทั่วไป
        if (!activeScene.Equals("Lobby", StringComparison.OrdinalIgnoreCase) &&
            !activeScene.Equals("SampleScene", StringComparison.OrdinalIgnoreCase))
        {
            SaveCurrentScene(activeScene);
        }
    }

    /// <summary>
    /// บันทึกฉากปัจจุบันลงใน DB
    /// </summary>
    public static void SaveCurrentScene(string sceneName)
    {
        InitPath();
        GameProgressData data = LoadProgress();
        data.currentScene = sceneName;
        data.lastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        SaveProgress(data);
        Debug.Log($"[GameProgressManager] บันทึกฉากปัจจุบันลง DB: {sceneName}");
    }

    /// <summary>
    /// บันทึกว่าผู้เล่นเคลียร์ด่านแล้ว
    /// </summary>
    public static void MarkStageCleared(string stageName, string nextUnlockedStage = "")
    {
        InitPath();
        GameProgressData data = LoadProgress();

        if (!data.clearedStages.Contains(stageName))
        {
            data.clearedStages.Add(stageName);
        }

        if (!string.IsNullOrEmpty(nextUnlockedStage))
        {
            data.highestUnlockedScene = nextUnlockedStage;
        }

        data.lastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SaveProgress(data);
        Debug.Log($"[GameProgressManager] บันทึกผ่านด่าน: {stageName} (ด่านสูงสุด: {data.highestUnlockedScene})");
    }

    /// <summary>
    /// บันทึกว่าผู้เล่นปราบหัวหน้าตัวใดไปแล้ว
    /// </summary>
    public static void MarkBossDefeated(string bossId)
    {
        InitPath();
        GameProgressData data = LoadProgress();

        if (!data.defeatedBosses.Contains(bossId))
        {
            data.defeatedBosses.Add(bossId);
        }

        data.lastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SaveProgress(data);
        Debug.Log($"[GameProgressManager] บันทึกปราบหัวหน้า: {bossId}");
    }

    /// <summary>
    /// โหลดข้อมูลความคืบหน้าทั้งหมด หากไม่มีไฟล์เซฟจะคืนค่าเริ่มต้น (LV01)
    /// </summary>
    public static GameProgressData LoadProgress()
    {
        InitPath();

        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                GameProgressData data = JsonUtility.FromJson<GameProgressData>(json);
                if (data != null) return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressManager] เกิดข้อผิดพลาดในการโหลดไฟล์เซฟ: {e.Message}");
            }
        }

        // คืนค่าเริ่มต้นหากยังไม่มีไฟล์เซฟ
        return new GameProgressData();
    }

    /// <summary>
    /// เขียนบันทึกข้อมูล GameProgressData ลงใน JSON
    /// </summary>
    public static void SaveProgress(GameProgressData data)
    {
        InitPath();

        try
        {
            data.lastSaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressManager] ไม่สามารถเขียนไฟล์เซฟได้: {e.Message}");
        }
    }

    /// <summary>
    /// ดึงชื่อฉากล่าสุดที่บันทึกไว้ใน DB
    /// </summary>
    public static string GetSavedSceneName(string fallback = "LV01")
    {
        InitPath();
        if (!HasProgressData()) return fallback;

        GameProgressData data = LoadProgress();
        return string.IsNullOrEmpty(data.currentScene) ? fallback : data.currentScene;
    }

    /// <summary>
    /// ตรวจสอบว่ามีไฟล์บันทึกความคืบหน้าอยู่ในระบบหรือไม่
    /// </summary>
    public static bool HasProgressData()
    {
        InitPath();
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
    /// ลบไฟล์เซฟความคืบหน้า (ใช้เมื่อเริ่มเกมใหม่)
    /// </summary>
    public static void DeleteProgressData()
    {
        InitPath();
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[GameProgressManager] ลบไฟล์ความคืบหน้าเกมเรียบร้อย");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameProgressManager] ลบไฟล์เซฟไม่สำเร็จ: {e.Message}");
        }
    }
}
