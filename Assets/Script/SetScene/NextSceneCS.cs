using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวจัดการเปลี่ยนฉากเมื่อผู้เล่นชนจุดเปลี่ยนฉาก (Next Scene Trigger)
/// - จดบันทึกด่านที่เราอยู่ (currentScene) และด่านที่เราจะส่งไป (targetScene) ลงใน DB (game_progress.json)
/// - ส่งผู้เล่นไปยังหน้าโหลดดิ้งหลอกๆ (LoadingSceneCS) หรือโหลดตรงเข้าสู่ด่านเป้าหมาย
/// - สามารถเรียกใช้งานผ่าน OnClick หรือ Event ได้ด้วยฟังก์ชัน TriggerNextScene()
/// </summary>
public class NextSceneCS : MonoBehaviour
{
    [Header("Target Scene (ด่านปลายทาง)")]
    [Tooltip("ชื่อด่านปลายทางที่ต้องการส่งผู้เล่นไป (เช่น LV02, BossLV01)")]
    public string nextSceneName = "LV02";

    [Header("Loading Scene Settings")]
    [Tooltip("เปิดใช้งานหน้าโหลดดิ้งหลอกๆ ก่อนเปลี่ยนฉากหรือไม่")]
    [SerializeField] private bool useLoadingScene = true;

    [Tooltip("ชื่อฉากของหน้าโหลดดิ้งหลอกๆ (ค่าเริ่มต้น: LoadingScene)")]
    [SerializeField] private string loadingSceneName = "LoadingScene";

    [Header("Trigger Settings")]
    [Tooltip("Tag ของผู้เล่นที่จะทริกเกอร์เปลี่ยนฉาก")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("บันทึกว่าผ่านด่านปัจจุบันลง DB ทันทีเมื่อชนจุดเปลี่ยนฉาก")]
    [SerializeField] private bool markCurrentStageCleared = true;

    private bool isTransitioning = false; // ป้องกันการ trigger ซ้ำซ้อน

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag(playerTag))
        {
            TriggerNextScene();
        }
    }

    /// <summary>
    /// เรียกใช้งานเพื่อเปลี่ยนฉาก (รองรับทั้งการชน Trigger และการกดปุ่ม UI / Event)
    /// </summary>
    public void TriggerNextScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        string currentScene = SceneManager.GetActiveScene().name;

        // บันทึกผ่านด่านปัจจุบันถ้าตั้งค่าไว้
        if (markCurrentStageCleared)
        {
            GameProgressManagerCS.MarkStageCleared(currentScene, nextSceneName);
        }

        // บันทึกทั้งด่านที่เราอยู่ และด่านที่เราจะส่งไปด้วยลงใน DB สำหรับให้หน้าโหลดดิ้งหลอกๆ อ่านไปใช้งาน
        GameProgressManagerCS.SaveSceneTransition(currentScene, nextSceneName);

        // ตรวจสอบการโหลดผ่านหน้า Loading Scene
        if (useLoadingScene && !string.IsNullOrEmpty(loadingSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(loadingSceneName))
            {
                Debug.Log($"[NextSceneCS] กำลังส่งผู้เล่นเข้าสู่หน้าโหลดดิ้ง '{loadingSceneName}' (ด่านปลายทาง: {nextSceneName})");
                SceneManager.LoadScene(loadingSceneName);
                return;
            }
            else
            {
                Debug.LogWarning($"[NextSceneCS] ไม่พบฉาก '{loadingSceneName}' ใน Build Settings! กำลังโหลดเข้าด่าน '{nextSceneName}' โดยตรงแทน");
            }
        }

        // หากไม่ใช้หน้า Loading หรือไม่พบฉาก ให้โหลดด่านเป้าหมายตรงๆ
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// กำหนดด่านปลายทางใหม่แล้วสั่งเปลี่ยนฉากทันที
    /// </summary>
    public void TriggerNextScene(string targetScene)
    {
        nextSceneName = targetScene;
        TriggerNextScene();
    }
}
