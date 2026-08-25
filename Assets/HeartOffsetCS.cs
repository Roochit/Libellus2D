using UnityEngine;

public class HeartOffsetCS : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private string animationStateName = "HeartBeat"; // ชื่อ State ของอนิเมชันหัวใจใน Animator
    [SerializeField] private int frameOffset = 10; // ระยะห่างจำนวนเฟรมระหว่างแต่ละหัวใจ (เช่น ขยับทุกๆ 10 เฟรม)
    [SerializeField] private int totalFrames = 60; // จำนวนเฟรมทั้งหมดใน 1 ลูปของอนิเมชัน (60 เฟรม)

    private void OnEnable()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // ดึงลำดับ (Sibling Index) ของหัวใจใน UI (ดวงที่ 0, 1, 2, ...)
            int siblingIndex = transform.GetSiblingIndex();

            // คำนวณจุดเริ่มต้นของอนิเมชัน (Normalized Time: 0.0 - 1.0)
            // ตัวอย่าง: ดวงที่ 0 เริ่มที่ 0/60, ดวงที่ 1 เริ่มที่ 10/60, ดวงที่ 2 เริ่มที่ 20/60
            float normalizedOffset = (float)(siblingIndex * frameOffset) / totalFrames;

            // สั่งให้เล่นอนิเมชันที่ Layer 0 ตั้งแต่จุดเวลา (Offset) ที่คำนวณไว้
            animator.Play(animationStateName, 0, normalizedOffset);
        }
    }
}
