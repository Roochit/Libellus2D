using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบจัดการการตั้งค่าของเกมแบบรวมศูนย์ (Centralized Setting Manager)
/// - ควบคุมระดับเสียง (Master Volume, BGM, SFX)
/// - ควบคุมการแสดงผล (Fullscreen, Resolution, V-Sync)
/// - ควบคุมการเปิด/ปิดหน้าต่างการตั้งค่า (SettingCanvas / HomeMenuCanvas)
/// - บันทึกและโหลดค่าอัตโนมัติผ่าน PlayerPrefs
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
               (c.name.Equals("SettingCanvas", System.StringComparison.OrdinalIgnoreCase) ||
                c.name.Equals("SettingsCanvas", System.StringComparison.OrdinalIgnoreCase) ||
                c.name.Equals("SettingPanel", System.StringComparison.OrdinalIgnoreCase)))
            {
                settingsPanel = c.gameObject;
            }
            else if (homeMenuPanel == null && 
                    (c.name.Equals("HomeManuCanvas", System.StringComparison.OrdinalIgnoreCase) ||
                     c.name.Equals("HomeMenuCanvas", System.StringComparison.OrdinalIgnoreCase) ||
                     c.name.Equals("HomeMenu", System.StringComparison.OrdinalIgnoreCase)))
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
                    if (s.name.IndexOf("volume", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
                    if (t.name.IndexOf("fullscreen", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
                    if (b.name.IndexOf("ยืนยัน", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        b.name.IndexOf("confirm", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        b.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
    /// โหลดและประยุกต์ใช้การตั้งค่าทั้งหมดจาก PlayerPrefs
    /// </summary>
    public void LoadAndApplyAllSettings()
    {
        // 1. ระดับเสียง Master Volume (ค่าเริ่มต้น 1.0)
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        SetMasterVolume(masterVol);
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;

        // 2. ระดับเสียง BGM & SFX (ค่าเริ่มต้น 1.0)
        float bgmVol = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        SetBGMVolume(bgmVol);
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;

        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        SetSFXVolume(sfxVol);
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;

        // 3. โหมดเต็มจอ Fullscreen (ค่าเริ่มต้นตามขนาดหน้าจอจริง)
        bool isFull = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        SetFullscreen(isFull);
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFull;

        // 4. V-Sync (ค่าเริ่มต้น 0 = ปิด)
        bool isVSync = PlayerPrefs.GetInt(VSYNC_KEY, 0) == 1;
        SetVSync(isVSync);
        if (vsyncToggle != null) vsyncToggle.isOn = isVSync;

        // 5. ความละเอียดหน้าจอ Resolution
        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int defaultResIndex = filteredResolutions.Count - 1; // ความละเอียดสูงสุด
            int savedResIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, defaultResIndex);
            savedResIndex = Mathf.Clamp(savedResIndex, 0, filteredResolutions.Count - 1);

            resolutionDropdown.value = savedResIndex;
            resolutionDropdown.RefreshShownValue();
            SetResolutionIndex(savedResIndex);
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

            // กรองอัตราส่วน 16:9 ทั่วไปเพื่อความสวยงาม
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
        volume = Mathf.Clamp01(volume);
        AudioListener.volume = volume;

        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับระดับเสียง BGM
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับระดับเสียง SFX
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// สลับโหมดหน้าต่าง / เต็มจอ
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับความละเอียดหน้าจอตามลำดับใน Dropdown
    /// </summary>
    public void SetResolutionIndex(int index)
    {
        if (filteredResolutions.Count == 0 || index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, index);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ปรับการทำงานของ V-Sync (เปิด = ล็อกเฟรมตามจอ / ปิด = ไม่จำกัด)
    /// </summary>
    public void SetVSync(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        PlayerPrefs.SetInt(VSYNC_KEY, isVSync ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// คืนค่าการตั้งค่าทั้งหมดกลับสู่ค่าเริ่มต้นจากโรงงาน
    /// </summary>
    public void ResetToDefaults()
    {
        SetMasterVolume(1.0f);
        SetBGMVolume(1.0f);
        SetSFXVolume(1.0f);
        SetFullscreen(true);
        SetVSync(false);

        if (masterVolumeSlider != null) masterVolumeSlider.value = 1.0f;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = 1.0f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1.0f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (vsyncToggle != null) vsyncToggle.isOn = false;

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
    /// ปิดหน้าต่างการตั้งค่า และกลับสู่เมนูหลัก
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (homeMenuPanel != null)
        {
            homeMenuPanel.SetActive(true);
        }

        Debug.Log("[SettingManager] ปิดหน้าต่างการตั้งค่า");
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
}
