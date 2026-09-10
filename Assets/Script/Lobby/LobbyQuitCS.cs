using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// จัดการระบบออกจากเกม (ข้อ 4)
/// - ผูกฟังก์ชัน QuitGame() เข้ากับ OnClick ของปุ่ม "ออกจากเกม"
/// </summary>
public class LobbyQuitCS : MonoBehaviour
{
    /// <summary>
    /// ออกจากเกม (รองรับทั้งการรันใน Unity Editor และไฟล์ Build จริง)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[LobbyQuit] ผู้เล่นสั่งออกจากเกม");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
