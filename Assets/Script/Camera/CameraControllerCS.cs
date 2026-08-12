using UnityEngine;

public class CameraControllerCS : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target; // วัตถุที่กล้องจะติดตาม (ตัวละครผู้เล่น)
    [SerializeField] private float smoothTime = 0.12f; // เวลาที่ใช้ในการเคลื่อนกล้องตาม (ค่าน้อย = ตามเร็ว, ค่ามาก = ตามสมูท)
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // ระยะห่างระหว่างกล้องกับตัวผู้เล่น (แกน Z ควรเป็นลบสำหรับ 2D)

    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        // หากใน Inspector ไม่ได้ระบุ Target ไว้ ให้ทำการค้นหาเป้าหมายด้วย Tag "Player" อัตโนมัติ
        if (target == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // คำนวณตำแหน่งเป้าหมายที่กล้องควรอยู่
        Vector3 targetPosition = target.position + offset;

        // ใช้ SmoothDamp แทน Lerp เพื่อลดอาการสั่นของภาพกล้องอย่างเป็นธรรมชาติและมีประสิทธิภาพสูงขึ้น
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // อัปเดตตำแหน่งกล้องปัจจุบัน
        transform.position = smoothedPosition;
    }
}
