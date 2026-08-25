using UnityEngine;

public class SetObjectTriggerCS : MonoBehaviour
{
    [Header("Objects to Activate (เปิดการทำงาน)")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Objects to Deactivate (ปิดการทำงาน)")]
    [SerializeField] private GameObject[] objectsToDeactivate;

    [Header("Trigger Behavior settings")]
    [SerializeField] private bool triggerOnlyOnce = true; // ทำงานแค่ครั้งแรกครั้งเดียวหรือไม่

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ตรวจจับชนด้วย Trigger
        if (collision.CompareTag("Player"))
        {
            TriggerActions();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ตรวจจับชนแบบฟิสิกส์ทั่วไป (Solid Collision)
        if (collision.collider.CompareTag("Player"))
        {
            TriggerActions();
        }
    }

    private void TriggerActions()
    {
        // หากตั้งค่าให้ทำแค่ครั้งเดียว และได้ทำไปแล้ว ให้ข้ามการทำงาน
        if (triggerOnlyOnce && hasTriggered) return;

        hasTriggered = true;

        // เปิดการทำงานของ GameObjects ที่กำหนด
        if (objectsToActivate != null)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        // ปิดการทำงานของ GameObjects ที่กำหนด
        if (objectsToDeactivate != null)
        {
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        Debug.Log($"[SetObjectTrigger] Triggered by Player! Activated: {objectsToActivate?.Length ?? 0}, Deactivated: {objectsToDeactivate?.Length ?? 0}");
    }
}
