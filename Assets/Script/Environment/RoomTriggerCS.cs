using UnityEngine;

public class RoomTriggerCS : MonoBehaviour
{
    [Header("Room Door System Link")]
    [SerializeField] private RoomDoorControllerCS doorController; // เชื่อมโยงสคริปต์ควบคุมประตูของห้องนี้

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ตรวจสอบว่าผู้เล่นเดินเข้ามาในพื้นที่ของห้องหรือไม่
        if (collision.CompareTag("Player"))
        {
            if (doorController != null)
            {
                doorController.ActivateRoom();
            }
            else
            {
                Debug.LogWarning("RoomTriggerCS: doorController is not assigned!");
            }
        }
    }
}
