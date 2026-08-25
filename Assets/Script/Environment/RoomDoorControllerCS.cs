using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RoomDoorControllerCS : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private List<EnemyCS> enemiesToDefeat = new List<EnemyCS>();
    [SerializeField] private GameObject doorVisual; // วัตถุแสดงผลประตู (เช่น Sprite Renderer)
    [SerializeField] private Collider2D doorCollider; // คอลไลเดอร์ที่ใช้บล็อกทางผู้เล่น

    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI enemyCountText; // UI Text (TextMeshPro) สำหรับแสดงจำนวนศัตรู
    [SerializeField] private string textPrefix = "Enemies: "; // ข้อความนำหน้าตัวเลข เช่น "ศัตรู: "
    [SerializeField] private bool activateOnStart = false; // เริ่มต้นมาให้ทำงานและแสดงผล UI เลยหรือไม่

    private int totalEnemies;
    private bool isDoorOpen = false;
    private bool isRoomActive = false;

    private void Start()
    {
        // บันทึกจำนวนศัตรูทั้งหมดตอนเริ่มต้น
        totalEnemies = enemiesToDefeat.Count;

        // ค้นหาคอลไลเดอร์อัตโนมัติหากไม่ได้ลากมาใส่ใน Inspector
        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider2D>();
        }

        // ค้นหาวัตถุแสดงผลอัตโนมัติหากไม่ได้ลากมาใส่
        if (doorVisual == null)
        {
            doorVisual = gameObject;
        }

        // ตั้งค่าการทำงานและ UI เริ่มต้น
        if (activateOnStart)
        {
            ActivateRoom();
        }
        else
        {
            isRoomActive = false;
            if (enemyCountText != null)
            {
                enemyCountText.gameObject.SetActive(false);
            }
        }

        // หากไม่มีศัตรูเลยตั้งแต่เริ่ม ให้เปิดประตูทันที
        if (totalEnemies == 0)
        {
            OpenDoor();
        }
    }

    private void Update()
    {
        if (isDoorOpen) return;

        // ตรวจสอบจำนวนศัตรูที่ยังมีชีวิตอยู่
        int activeCountBefore = enemiesToDefeat.Count;
        
        // ลบศัตรูที่โดน Destroy (เป็น null) ออกจากลิสต์
        enemiesToDefeat.RemoveAll(enemy => enemy == null);

        // หากมีศัตรูตาย (จำนวนในลิสต์ลดลง) ให้ทำการอัปเดต UI
        if (enemiesToDefeat.Count < activeCountBefore)
        {
            UpdateUI();
        }

        // หากกำจัดศัตรูหมดแล้ว ให้เปิดประตู
        if (enemiesToDefeat.Count == 0)
        {
            OpenDoor();
        }
    }

    public void ActivateRoom()
    {
        if (isRoomActive) return; // หากทำงานอยู่แล้ว ให้ข้าม

        isRoomActive = true;
        
        if (enemyCountText != null)
        {
            enemyCountText.gameObject.SetActive(true);
        }

        UpdateUI();
        Debug.Log($"Room activated. Total enemies to defeat: {totalEnemies}");
    }

    private void UpdateUI()
    {
        if (isRoomActive && enemyCountText != null)
        {
            int defeatedEnemies = totalEnemies - enemiesToDefeat.Count;
            enemyCountText.text = $"{textPrefix}{defeatedEnemies}/{totalEnemies}";
        }
    }

    private void OpenDoor()
    {
        isDoorOpen = true;
        Debug.Log("All enemies defeated! Door opened.");

        // ซ่อน UI บอกจำนวนศัตรูเมื่อผ่านห้องแล้ว
        if (enemyCountText != null)
        {
            enemyCountText.gameObject.SetActive(false);
        }

        // ปิดกั้นคอลไลเดอร์เพื่อยอมให้ผู้เล่นเดินผ่านได้
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        // ซ่อนประตู (หรือสามารถเปลี่ยนเป็นการเล่นแอนิเมชันเปิดประตูได้ที่นี่)
        if (doorVisual != null)
        {
            // ถ้า doorVisual คือตัว GameObject เอง ให้ปิดเฉพาะ Renderer/Collider เพื่อไม่ให้สคริปต์นี้หยุดทำงาน
            if (doorVisual == gameObject)
            {
                if (TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    renderer.enabled = false;
                }
            }
            else
            {
                doorVisual.SetActive(false);
            }
        }
    }
}
