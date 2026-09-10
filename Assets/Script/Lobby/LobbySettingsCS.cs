using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จัดการระบบการตั้งค่าในล็อบบี้ (ข้อ 3)
/// - ปรับระดับเสียง Master Volume (ผูกฟังก์ชัน SetMasterVolume เข้ากับ Slider)
/// - สลับโหมดเต็มจอ Fullscreen (ผูกฟังก์ชัน SetFullscreen เข้ากับ Toggle)
/// - บันทึกและโหลดค่าอัตโนมัติผ่าน PlayerPrefs
/// </summary>
public class LobbySettingsCS : MonoBehaviour
{
    [Header("UI References (Optional)")]
    [Tooltip("ลาก Slider ปรับเสียงมาใส่ (ถ้ามี) เพื่อให้สคริปต์ตั้งค่าเริ่มต้นของ Slider ให้ตรงกับค่าที่เคยบันทึก")]
    [SerializeField] private Slider volumeSlider;

    [Tooltip("ลาก Toggle ปรับเต็มจอมาใส่ (ถ้ามี) เพื่อให้สคริปต์ตั้งค่าเริ่มต้นของ Toggle ให้ตรงกับค่าที่เคยบันทึก")]
    [SerializeField] private Toggle fullscreenToggle;

    private const string VOLUME_KEY = "MasterVolume";
    private const string FULLSCREEN_KEY = "IsFullscreen";

    private void Start()
    {
        LoadAndApplySettings();
    }

    /// <summary>
    /// โหลดค่าการตั้งค่าเดิมที่เคยเซฟไว้ และอัปเดตระบบ/UI ทันที
    /// </summary>
    public void LoadAndApplySettings()
    {
        // 1. โหลดระดับเสียง (ค่าเริ่มต้น 1.0 = 100%)
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        AudioListener.volume = savedVolume;
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }

        // 2. โหลดโหมดเต็มจอ
        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
        }
    }

    /// <summary>
    /// ปรับระดับเสียง Master Volume (0.0 ถึง 1.0)
    /// ผูกกับ OnValueChanged(float) ของ Slider ใน Inspector
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// เปิด/ปิดการแสดงผลแบบเต็มจอ
    /// ผูกกับ OnValueChanged(bool) ของ Toggle ใน Inspector
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
