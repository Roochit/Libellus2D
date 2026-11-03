using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// โครงสร้างข้อมูลสำหรับการบันทึกการตั้งค่าลงไฟล์ db/game_seting.json
/// </summary>
[System.Serializable]
public class GameSettingData
{
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public bool isFullscreen = true;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public int resolutionIndex = 0;
    public bool isVSync = true;
    public string lastSavedTimestamp = "";
}

/// <summary>
/// ระบบจัดการการตั้งค่าของเกมแบบรวมศูนย์ (Centralized Setting Manager)
/// - ควบคุมระดับเสียง (Master Volume, BGM, SFX)
/// - ควบคุมการแสดงผล (Fullscreen, Resolution, V-Sync)
/// - ควบคุมการเปิด/ปิดหน้าต่างการตั้งค่า (SettingCanvas / HomeMenuCanvas)
/// - บันทึกและโหลดข้อมูลลงทั้ง PlayerPrefs และ db/game_seting.json อัตโนมัติ
/// - มีระบบ Auto-Find UI ช่วยค้นหาและผูก Event อัตโนมัติแม้ไม่ได้ลากใส่ใน Inspector
/// </summary>
public class SettingManagerCS : MonoBehaviour
{
    private static SettingManagerCS instance;
    public static SettingManagerCS Instance => instance;

    [Header("UI Panels (หน้าต่างเมนู)")]
    [Tooltip("หน้าต่างพาเนล/แคนวาสของการตั้งค่า (เช่น SettingCanvas)")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("หน้าต่างพาเนล/แคนวาสของเมนูหลัก (เช่น HomeManuCanvas) สำหรับสลับการแสดงผล")]
    [SerializeField] private GameObject homeMenuPanel;

    [Header("Audio UI References (เสียง)")]
    [Tooltip("Slider ปรับระดับเสียง Master Volume (0.0 ถึง 1.0)")]
    [SerializeField] private Slider masterVolumeSlider;

    [Tooltip("ข้อความแสดงเปอร์เซ็นต์ระดับเสียง Master เช่น '100%'")]
    [SerializeField] private TMP_Text masterVolumeText;

    [Tooltip("Slider ปรับระดับเสียงเพลงประกอบ BGM (0.0 ถึง 1.0)")]
    [SerializeField] private Slider bgmVolumeSlider;

    [Tooltip("Slider ปรับระดับเสียงเอฟเฟกต์ SFX (0.0 ถึง 1.0)")]
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Display UI References (การแสดงผล)")]
    [Tooltip("Toggle สลับโหมดเต็มจอ Fullscreen")]
    [SerializeField] private Toggle fullscreenToggle;

    [Tooltip("Dropdown เลือกความละเอียดหน้าจอ (Resolution)")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Tooltip("Toggle เปิด/ปิด V-Sync")]
    [SerializeField] private Toggle vsyncToggle;

    [Header("Action Buttons (ปุ่ม)")]
    [Tooltip("ปุ่มยืนยัน/ปิดหน้าต่างการตั้งค่า (เช่น 'ยืนยัน-btn')")]
    [SerializeField] private Button confirmCloseButton;

    [Tooltip("ปุ่มคืนค่าเริ่มต้นของการตั้งค่า (Reset)")]
    [SerializeField] private Button resetDefaultsButton;

    [Header("Auto Find Settings")]
    [Tooltip("ค้นหา UI ในฉากอัตโนมัติหากยังไม่ได้ลากใส่ Inspector")]
    [SerializeField] private bool autoFindUI = true;

    // PlayerPrefs Keys
    public const string MASTER_VOLUME_KEY = "MasterVolume";
    public const string BGM_VOLUME_KEY = "BGMVolume";
    public const string SFX_VOLUME_KEY = "SFXVolume";
    public const string FULLSCREEN_KEY = "IsFullscreen";
    public const string RESOLUTION_INDEX_KEY = "ResolutionIndex";
    public const string VSYNC_KEY = "VSync";

    private Resolution[] availableResolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();
    private GameSettingData currentSettingData = new GameSettingData();

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
        InitializeResolutions();
        LoadAndApplyAllSettings();
        BindUIEvents();
    }

    /// <summary>
    /// คืนค่าที่อยู่ของไฟล์ db/game_seting.json
    /// </summary>
    public static string GetSettingFilePath()
    {
        string dbDir = Path.Combine(Application.dataPath, "..", "db");
        if (!Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }
        return Path.Combine(dbDir, "game_seting.json");
    }

    /// <summary>
    /// บันทึกข้อมูลการตั้งค่าลงไฟล์ db/game_seting.json
    /// </summary>
    public void SaveSettingsToJsonFile()
    {
        try
        {
            currentSettingData.lastSavedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(currentSettingData, true);
            File.WriteAllText(GetSettingFilePath(), json);
            Debug.Log($"[SettingManager] บันทึกการตั้งค่าลง JSON สำเร็จ: {GetSettingFilePath()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SettingManager] ไม่สามารถบันทึก game_seting.json ได้: {ex.Message}");
        }
    }

    /// <summary>
    /// ค้นหา UI อัตโนมัติจากชื่อ Object ทั่วไปในฉาก (รองรับทั้ง Active และ Inactive Canvas)
    /// </summary>
    private void AutoFindUIElements()
    {
        // 1. ค้นหา Canvas ทั้งหมดในฉาก รวมถึงที่ปิดอยู่ (Inactive) เช่น SettingCanvas
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c == null) continue;

            if (settingsPanel == null && 
               (c.name.Equals("SettingCanvas", StringComparison.OrdinalIgnoreCase) ||
                c.name.Equals("SettingsCanvas", StringComparison.OrdinalIgnoreCase) ||
                c.name.Equals("SettingPanel", StringComparison.OrdinalIgnoreCase)))
            {
                settingsPanel = c.gameObject;
            }
            else if (homeMenuPanel == null && 
                    (c.name.Equals("HomeManuCanvas", StringComparison.OrdinalIgnoreCase) ||
                     c.name.Equals("HomeMenuCanvas", StringComparison.OrdinalIgnoreCase) ||
                     c.name.Equals("HomeMenu", StringComparison.OrdinalIgnoreCase)))
            {
                homeMenuPanel = c.gameObject;
            }
        }

        // 2. ค้นหา UI Elements ภายใน settingsPanel
        if (settingsPanel != null)
        {
            if (masterVolumeSlider == null)
            {
                Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
                foreach (var s in sliders)
                {
                    if (s.name.IndexOf("volume", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        masterVolumeSlider = s;
                        break;
                    }
                }
            }

            if (fullscreenToggle == null)
            {
                Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
                foreach (var t in toggles)
                {
                    if (t.name.IndexOf("fullscreen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        fullscreenToggle = t;
                        break;
                    }
                }
            }

            if (confirmCloseButton == null)
            {
                Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b.name.IndexOf("ยืนยัน", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        b.name.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        b.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        confirmCloseButton = b;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ผูก Event เข้ากับ UI Elements
    /// </summary>
    private void BindUIEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(SetBGMVolume);
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolutionIndex);
            resolutionDropdown.onValueChanged.AddListener(SetResolutionIndex);
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveListener(SetVSync);
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }

        if (confirmCloseButton != null)
        {
            confirmCloseButton.onClick.RemoveListener(CloseSettings);
            confirmCloseButton.onClick.AddListener(CloseSettings);
        }

        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.onClick.RemoveListener(ResetToDefaults);
            resetDefaultsButton.onClick.AddListener(ResetToDefaults);
        }
    }

    /// <summary>
    /// โหลดและประยุกต์ใช้การตั้งค่าทั้งหมดจาก db/game_seting.json หรือ PlayerPrefs
    /// </summary>
    public void LoadAndApplyAllSettings()
    {
        string jsonPath = GetSettingFilePath();
        bool hasJsonFile = false;

        // 1. ลองอ่านจาก db/game_seting.json ก่อน
        if (File.Exists(jsonPath))
        {
            try
            {
                string json = File.ReadAllText(jsonPath).Trim();
                if (!string.IsNullOrEmpty(json))
                {
                    GameSettingData data = JsonUtility.FromJson<GameSettingData>(json);
                    if (data != null)
                    {
                        currentSettingData = data;
                        hasJsonFile = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SettingManager] โหลด game_seting.json ล้มเหลว: {ex.Message}");
            }
        }

        // 2. หากยังไม่มีข้อมูลจาก JSON ให้อ่านจาก PlayerPrefs
        if (!hasJsonFile)
        {
            currentSettingData.masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            currentSettingData.bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
            currentSettingData.sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            currentSettingData.isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
            currentSettingData.isVSync = PlayerPrefs.GetInt(VSYNC_KEY, 1) == 1;
            currentSettingData.resolutionIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, 0);

            // บันทึกสร้างไฟล์ game_seting.json ตั้งต้นทันที
            SaveSettingsToJsonFile();
        }

        // 3. ปรับใช้ค่าเสียง
        SetMasterVolumeInternal(currentSettingData.masterVolume);
        if (masterVolumeSlider != null) masterVolumeSlider.value = currentSettingData.masterVolume;

        SetBGMVolumeInternal(currentSettingData.bgmVolume);
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = currentSettingData.bgmVolume;

        SetSFXVolumeInternal(currentSettingData.sfxVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSettingData.sfxVolume;

        // 4. ปรับใช้การแสดงผล
        SetFullscreenInternal(currentSettingData.isFullscreen);
        if (fullscreenToggle != null) fullscreenToggle.isOn = currentSettingData.isFullscreen;

        SetVSyncInternal(currentSettingData.isVSync);
        if (vsyncToggle != null) vsyncToggle.isOn = currentSettingData.isVSync;

        // 5. ปรับใช้ความละเอียดหน้าจอ
        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int savedResIndex = Mathf.Clamp(currentSettingData.resolutionIndex, 0, filteredResolutions.Count - 1);
            resolutionDropdown.value = savedResIndex;
            resolutionDropdown.RefreshShownValue();
            SetResolutionIndexInternal(savedResIndex);
        }
    }

    /// <summary>
    /// เตรียมรายการความละเอียดหน้าจอที่จอภาพรองรับ
    /// </summary>
    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        filteredResolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution res = availableResolutions[i];

            string option = $"{res.width} x {res.height} @{res.refreshRateRatio.value:F0}Hz";
            if (!options.Contains(option))
            {
                filteredResolutions.Add(res);
                options.Add(option);

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentResIndex = filteredResolutions.Count - 1;
                }
            }
        }

        if (options.Count == 0)
        {
            options.Add($"{Screen.width} x {Screen.height}");
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // ==========================================
    // PUBLIC SETTERS (ผูกกับ UI Inspector)
    // ==========================================

    /// <summary>
    /// ปรับระดับเสียง Master Volume (0.0 ถึง 1.0)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetMasterVolumeInternal(volume);
        SaveSettingsToJsonFile();
    }

    private void SetMasterVolumeInternal(float volume)
    {
        volume = Mathf.Clamp01(volume);
        AudioListener.volume = volume;

        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }

        currentSettingData.masterVolume = volume;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับระดับเสียง BGM
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        SetBGMVolumeInternal(volume);
        SaveSettingsToJsonFile();
    }

    private void SetBGMVolumeInternal(float volume)
    {
        volume = Mathf.Clamp01(volume);
        currentSettingData.bgmVolume = volume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับระดับเสียง SFX
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetSFXVolumeInternal(volume);
        SaveSettingsToJsonFile();
    }

    private void SetSFXVolumeInternal(float volume)
    {
        volume = Mathf.Clamp01(volume);
        currentSettingData.sfxVolume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// สลับโหมดหน้าต่าง / เต็มจอ
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        SetFullscreenInternal(isFullscreen);
        SaveSettingsToJsonFile();
    }

    private void SetFullscreenInternal(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        currentSettingData.isFullscreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับความละเอียดหน้าจอตามลำดับใน Dropdown
    /// </summary>
    public void SetResolutionIndex(int index)
    {
        SetResolutionIndexInternal(index);
        SaveSettingsToJsonFile();
    }

    private void SetResolutionIndexInternal(int index)
    {
        if (filteredResolutions.Count == 0 || index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        currentSettingData.resolutionIndex = index;
        currentSettingData.resolutionWidth = res.width;
        currentSettingData.resolutionHeight = res.height;

        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, index);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับการทำงานของ V-Sync (เปิด = ล็อกเฟรมตามจอ / ปิด = ไม่จำกัด)
    /// </summary>
    public void SetVSync(bool isVSync)
    {
        SetVSyncInternal(isVSync);
        SaveSettingsToJsonFile();
    }

    private void SetVSyncInternal(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        Application.targetFrameRate = isVSync ? -1 : 60;
        currentSettingData.isVSync = isVSync;
        PlayerPrefs.SetInt(VSYNC_KEY, isVSync ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// คืนค่าการตั้งค่าทั้งหมดกลับสู่ค่าเริ่มต้นจากโรงงาน
    /// </summary>
    public void ResetToDefaults()
    {
        SetMasterVolumeInternal(1.0f);
        SetBGMVolumeInternal(1.0f);
        SetSFXVolumeInternal(1.0f);
        SetFullscreenInternal(true);
        SetVSyncInternal(true);

        if (masterVolumeSlider != null) masterVolumeSlider.value = 1.0f;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = 1.0f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1.0f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (vsyncToggle != null) vsyncToggle.isOn = true;

        SaveSettingsToJsonFile();
        Debug.Log("[SettingManager] คืนค่าการตั้งค่ากลับสู่ค่าเริ่มต้นเรียบร้อยแล้ว");
    }

    // ==========================================
    // PANEL CONTROLS (เปิด/ปิดหน้าต่างการตั้งค่า)
    // ==========================================

    /// <summary>
    /// เปิดหน้าต่างการตั้งค่า (Settings Panel)
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (homeMenuPanel != null)
        {
            homeMenuPanel.SetActive(false);
        }

        Debug.Log("[SettingManager] เปิดหน้าต่างการตั้งค่า");
    }

    /// <summary>
    /// ปิดหน้าต่างการตั้งค่า และกลับสู่เมนูหลัก พร้อมบันทึกลง JSON
    /// </summary>
    public void CloseSettings()
    {
        SaveSettingsToJsonFile();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (homeMenuPanel != null)
        {
            homeMenuPanel.SetActive(true);
        }

        Debug.Log("[SettingManager] ปิดหน้าต่างการตั้งค่า และบันทึกลง game_seting.json");
    }

    /// <summary>
    /// สลับเปิด/ปิดหน้าต่างการตั้งค่า
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    public GameSettingData CurrentSettingData => currentSettingData;
}
