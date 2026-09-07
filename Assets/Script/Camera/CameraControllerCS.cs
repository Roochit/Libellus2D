using UnityEngine;

/// <summary>
/// ตัวควบคุมกล้อง 2D รองรับการติดตามผู้เล่นและการซูมเข้า/ออกอย่างนุ่มนวล (Smooth Zoom)
/// </summary>
public class CameraControllerCS : MonoBehaviour
{
    public static CameraControllerCS Instance { get; private set; }

    [Header("Follow Settings")]
    [SerializeField] private Transform target; // วัตถุที่กล้องจะติดตาม (ตัวละครผู้เล่น)
    [SerializeField] private float smoothTime = 0.12f; // เวลาที่ใช้ในการเคลื่อนกล้องตาม (ค่าน้อย = ตามเร็ว, ค่ามาก = ตามสมูท)
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // ระยะห่างระหว่างกล้องกับตัวผู้เล่น (แกน Z ควรเป็นลบสำหรับ 2D)

    [Header("Zoom Settings (Orthographic)")]
    [SerializeField] private float defaultSize = 5f; // ขนาดมุมมองกล้องปกติ
    [SerializeField] private float zoomSmoothTime = 0.35f; // ความนุ่มนวลในการซูมกล้อง

    private Camera cam;
    private float targetSize;
    private float zoomVelocity = 0f;
    private Vector3 currentVelocity = Vector3.zero;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null && cam.orthographic)
        {
            if (defaultSize <= 0.01f)
            {
                defaultSize = cam.orthographicSize;
            }
            targetSize = defaultSize;
        }
    }

    private void Start()
    {
        // หากใน Inspector ไม่ได้ระบุ Target ไว้ ให้ทำการค้นหาเป้าหมายด้วย Tag "Player" อัตโนมัติ
        if (target == null)
        {
            FindPlayerTarget();
        }
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
    }

    private void LateUpdate()
    {
        // 1. จัดการการเคลื่อนกล้องตามผู้เล่น
        if (target == null)
        {
            FindPlayerTarget();
        }

        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
            transform.position = smoothedPosition;
        }

        // 2. จัดการการซูมกล้อง (Orthographic Size)
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
        }
    }

    /// <summary>
    /// สั่งซูมกล้องไปยังขนาดที่ต้องการ (เช่น ค่ามาก = ซูมออกกว้างๆ)
    /// </summary>
    public void SetZoom(float newSize, float transitionTime = -1f)
    {
        targetSize = newSize;
        if (transitionTime > 0f)
        {
            zoomSmoothTime = transitionTime;
        }
    }

    /// <summary>
    /// สั่งคืนค่ามุมกล้องกลับสู่ขนาดเริ่มต้นปกติ
    /// </summary>
    public void ResetZoom(float transitionTime = -1f)
    {
        targetSize = defaultSize;
        if (transitionTime > 0f)
        {
            zoomSmoothTime = transitionTime;
        }
    }
}

