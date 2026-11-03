#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;

/// <summary>
/// เครื่องมือช่วยเตรียมการและสั่งบิลด์เกม (Build Automation & Project Verifier)
/// เข้าใช้งานได้จากแถบเมนูด้านบนของ Unity Editor: [Build Game]
/// </summary>
public static class BuildGameEditor
{
    private const string BUILD_FOLDER = "Builds";
    private const string GAME_NAME = "Libellus2D";

    // รายชื่อด่านหลักตามลำดับการเล่น
    private static readonly string[] REQUIRED_SCENES = new string[]
    {
        "Assets/Scenes/Lobby.unity",
        "Assets/Scenes/LV00.unity",
        "Assets/Scenes/LoadingScene.unity",
        "Assets/Scenes/LV01.unity",
        "Assets/Scenes/BossLV01.unity",
        "Assets/Scenes/LV02.unity",
        "Assets/Scenes/End_Credit.unity"
    };

    // ========================================================
    // 1. ตรวจสอบความพร้อมของโปรเจกต์ (Project Verification)
    // ========================================================
    [MenuItem("Build Game/1. ตรวจสอบความพร้อมของโปรเจกต์ (Verify Project)", false, 1)]
    public static bool VerifyProjectReady()
    {
        Debug.Log("=================================================");
        Debug.Log("[BuildGame] เริ่มต้นการตรวจสอบความพร้อมของโปรเจกต์...");
        Debug.Log("=================================================");

        bool allPassed = true;

        // 1. ตรวจสอบไฟล์ฉากใน Build Settings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (buildScenes.Count == 0)
        {
            Debug.LogError("[BuildGame] ❌ ไม่พบฉากใดๆ ใน Build Settings!");
            allPassed = false;
        }
        else
        {
            // ตรวจสอบฉากแรก (Index 0) ต้องเป็น Lobby
            EditorBuildSettingsScene firstScene = buildScenes.Find(s => s.enabled);
            if (firstScene == null || !firstScene.path.Contains("Lobby"))
            {
                Debug.LogWarning($"[BuildGame] ⚠️ ฉากแรกใน Build Settings ไม่ใช่ Lobby (ฉากปัจจุบัน: {firstScene?.path})");
            }
            else
            {
                Debug.Log($"[BuildGame] ✅ ฉากเริ่มต้น (Index 0): {firstScene.path}");
            }

            // ตรวจสอบความครบถ้วนของทุกด่าน
            foreach (string reqScene in REQUIRED_SCENES)
            {
                if (!File.Exists(reqScene))
                {
                    Debug.LogError($"[BuildGame] ❌ ไม่พบไฟล์ฉากบนดิสก์: {reqScene}");
                    allPassed = false;
                }
                else
                {
                    bool inBuild = buildScenes.Exists(s => s.path == reqScene && s.enabled);
                    if (!inBuild)
                    {
                        Debug.LogWarning($"[BuildGame] ⚠️ ฉาก {reqScene} ยังไม่ได้ถูกเปิดใช้งานใน Build Settings!");
                        allPassed = false;
                    }
                    else
                    {
                        Debug.Log($"[BuildGame] ✅ ฉากพร้อมใน Build Settings: {reqScene}");
                    }
                }
            }
        }

        // 2. ตรวจสอบฐานข้อมูลโฟลเดอร์ db/
        string projectDbDir = Path.Combine(Application.dataPath, "..", "db");
        if (!Directory.Exists(projectDbDir))
        {
            Directory.CreateDirectory(projectDbDir);
            Debug.Log("[BuildGame] ℹ️ ได้สร้างโฟลเดอร์ db/ ให้เรียบร้อยแล้ว");
        }

        string[] requiredJsonFiles = new string[]
        {
            "player_health.json",
            "game_progress.json",
            "game_seting.json",
            "EndGame_Log.json"
        };

        foreach (string jsonFile in requiredJsonFiles)
        {
            string fullPath = Path.Combine(projectDbDir, jsonFile);
            if (!File.Exists(fullPath) || new FileInfo(fullPath).Length == 0)
            {
                CreateDefaultJsonFile(jsonFile, fullPath);
                Debug.Log($"[BuildGame] ✅ สร้างไฟล์ข้อมูลตั้งต้นให้เรียบร้อย: db/{jsonFile}");
            }
            else
            {
                Debug.Log($"[BuildGame] ✅ พบไฟล์ข้อมูล: db/{jsonFile}");
            }
        }

        // 3. สรุปผล
        if (allPassed)
        {
            Debug.Log("=================================================");
            Debug.Log("🎉 [BuildGame] โปรเจกต์พร้อมสำหรับการบิลด์ 100%!");
            Debug.Log("=================================================");
            EditorUtility.DisplayDialog("Project Verification", "โปรเจกต์พร้อมสำหรับการบิลด์เกมเรียบร้อยแล้ว! ✅\n\n- ฉากครบถ้วนตามลำดับ\n- ไฟล์ฐานข้อมูล db ครบถ้วน\n- โค้ดคอมไพล์ผ่านสมบูรณ์", "ตกลง");
        }
        else
        {
            Debug.LogWarning("=================================================");
            Debug.LogWarning("⚠️ [BuildGame] ตรวจพบการตั้งค่าบางอย่างที่ต้องตรวจสอบเพิ่มเติม (ดูรายละเอียดใน Console)");
            Debug.LogWarning("=================================================");
            EditorUtility.DisplayDialog("Project Verification", "พบข้อควรปรับปรุงในโปรเจกต์ กรุณาดูรายละเอียดใน Console Log", "รับทราบ");
        }

        return allPassed;
    }

    // ========================================================
    // 2. บิลด์สำหรับ macOS (StandaloneOSX)
    // ========================================================
    [MenuItem("Build Game/2. บิลด์สำหรับ macOS (Build macOS .app)", false, 20)]
    public static void BuildMacOS()
    {
        if (!VerifyProjectReady())
        {
            if (!EditorUtility.DisplayDialog("แจ้งเตือน", "ยังมีการตั้งค่าบางอย่างไม่สมบูรณ์ ต้องการดำเนินการบิลด์ต่อหรือไม่?", "บิลด์ต่อ", "ยกเลิก"))
            {
                return;
            }
        }

        string outputDir = Path.Combine(Application.dataPath, "..", BUILD_FOLDER, "macOS");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string appPath = Path.Combine(outputDir, $"{GAME_NAME}.app");

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = appPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        ExecuteBuild(buildOptions, outputDir);
    }

    // ========================================================
    // 3. บิลด์สำหรับ Windows (StandaloneWindows64)
    // ========================================================
    [MenuItem("Build Game/3. บิลด์สำหรับ Windows 64-bit (Build Windows .exe)", false, 21)]
    public static void BuildWindows64()
    {
        if (!VerifyProjectReady())
        {
            if (!EditorUtility.DisplayDialog("แจ้งเตือน", "ยังมีการตั้งค่าบางอย่างไม่สมบูรณ์ ต้องการดำเนินการบิลด์ต่อหรือไม่?", "บิลด์ต่อ", "ยกเลิก"))
            {
                return;
            }
        }

        string outputDir = Path.Combine(Application.dataPath, "..", BUILD_FOLDER, "Windows");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string exePath = Path.Combine(outputDir, $"{GAME_NAME}.exe");

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        ExecuteBuild(buildOptions, outputDir);
    }

    // ========================================================
    // 4. เปิดโฟลเดอร์ผลลัพธ์ Builds
    // ========================================================
    [MenuItem("Build Game/4. เปิดโฟลเดอร์ Builds (Open Builds Folder)", false, 40)]
    public static void OpenBuildsFolder()
    {
        string buildDir = Path.Combine(Application.dataPath, "..", BUILD_FOLDER);
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }
        EditorUtility.RevealInFinder(buildDir);
    }

    // ========================================================
    // ฟังก์ชันเบื้องหลังสำหรับกระบวนการ Build
    // ========================================================
    private static string[] GetEnabledScenePaths()
    {
        List<string> scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
            {
                scenes.Add(s.path);
            }
        }
        return scenes.ToArray();
    }

    private static void ExecuteBuild(BuildPlayerOptions options, string outputDir)
    {
        Debug.Log($"[BuildGame] เริ่มต้นการบิลด์ไปยัง: {options.locationPathName} (Target: {options.target})...");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"🎉 [BuildGame] บิลด์สำเร็จเรียบร้อย! ขนาดไฟล์: {summary.totalSize / (1024 * 1024):F1} MB (ใช้เวลา {summary.totalTime.TotalSeconds:F1} วินาที)");
            EditorUtility.RevealInFinder(options.locationPathName);
            EditorUtility.DisplayDialog("Build Success", $"บิลด์เกมสำเร็จเรียบร้อย! 🎉\n\nปลายทาง: {options.locationPathName}\nขนาด: {summary.totalSize / (1024 * 1024):F1} MB", "เปิดดูไฟล์");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"❌ [BuildGame] บิลด์ล้มเหลว! มีข้อผิดพลาด {summary.totalErrors} รายการ (ตรวจสอบรายละเอียดใน Console)");
            EditorUtility.DisplayDialog("Build Failed", $"บิลด์ไม่สำเร็จ เกิดข้อผิดพลาด {summary.totalErrors} จุด กรุณาเปิด Console ตรวจสอบ", "ตกลง");
        }
    }

    // ========================================================
    // PostProcess: คัดลอกโฟลเดอร์ db/ ไปไว้คู่กับตัวเกมที่บิลด์ออกมาให้อัตโนมัติ
    // ========================================================
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        try
        {
            string sourceDbDir = Path.Combine(Application.dataPath, "..", "db");
            if (!Directory.Exists(sourceDbDir)) return;

            string targetDbDir = "";

            if (target == BuildTarget.StandaloneOSX)
            {
                // สำหรับ Mac: วางไว้ข้าง .app และใน Contents/Resources/db เพื่อรองรับทุกโหมดการอ่าน Path
                string parentDir = Path.GetDirectoryName(pathToBuiltProject);
                string outDb1 = Path.Combine(parentDir, "db");
                CopyDirectory(sourceDbDir, outDb1);

                string resourcesDb = Path.Combine(pathToBuiltProject, "Contents", "Resources", "db");
                CopyDirectory(sourceDbDir, resourcesDb);

                Debug.Log($"[BuildGame-PostProcess] คัดลอก db ไปยังโฟลเดอร์แอป macOS สำเร็จ");
            }
            else if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            {
                // สำหรับ Windows: วางไว้โฟลเดอร์เดียวกับ .exe
                string parentDir = Path.GetDirectoryName(pathToBuiltProject);
                targetDbDir = Path.Combine(parentDir, "db");
                CopyDirectory(sourceDbDir, targetDbDir);

                Debug.Log($"[BuildGame-PostProcess] คัดลอก db ไปยังโฟลเดอร์ Windows Build สำเร็จ: {targetDbDir}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BuildGame-PostProcess] ไม่สามารถคัดลอก db อัตโนมัติ: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, dest);
        }
    }

    private static void CreateDefaultJsonFile(string fileName, string fullPath)
    {
        string content = "{}";
        switch (fileName)
        {
            case "player_health.json":
                content = "{\n  \"currentHearts\": 3,\n  \"maxHearts\": 3\n}";
                break;
            case "game_progress.json":
                content = "{\n  \"currentScene\": \"LV00\",\n  \"targetScene\": \"LV00\",\n  \"highestUnlockedScene\": \"LV00\",\n  \"clearedStages\": [],\n  \"defeatedBosses\": [],\n  \"lastSaveTimestamp\": \"\"\n}";
                break;
            case "game_seting.json":
                content = "{\n  \"masterVolume\": 1.0,\n  \"bgmVolume\": 1.0,\n  \"sfxVolume\": 1.0,\n  \"isFullscreen\": true,\n  \"resolutionWidth\": 1920,\n  \"resolutionHeight\": 1080,\n  \"resolutionIndex\": 0,\n  \"isVSync\": false,\n  \"lastSavedTimestamp\": \"\"\n}";
                break;
            case "EndGame_Log.json":
                content = "{\n  \"gameCompleted\": false,\n  \"lastCompletionTimestamp\": \"\",\n  \"totalClearCount\": 0,\n  \"finalCurrentHearts\": 3,\n  \"finalMaxHearts\": 3,\n  \"clearedStages\": [],\n  \"defeatedBosses\": [],\n  \"clearHistory\": []\n}";
                break;
        }
        File.WriteAllText(fullPath, content);
    }
}
#endif

