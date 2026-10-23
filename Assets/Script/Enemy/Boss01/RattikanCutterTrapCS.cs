using System.Collections;
using UnityEngine;

/// <summary>
/// กับดักมีดคัดเตอร์เหลาดินสอ EE สุ่มแทงออกมาจากพื้น ของบอสรัตติกาล (ท่าที่ 3)
/// มีสัญญาณเตือนก่อนแทงขึ้นมาทำดาเมจ แล้วดึงกลับลงพื้น และทำลายตัวเองอย่างแน่นอน
/// </summary>
public class RattikanCutterTrapCS : MonoBehaviour
{
    [Header("Phase Timings")]
    [SerializeField] private float warningDuration = 0.8f; // เวลาแสดงสัญญาณเตือนก่อนแทง (วิ)
    [SerializeField] private float thrustSpeed = 15f; // ความเร็วในการพุ่งแทงขึ้น
    [SerializeField] private float activeDuration = 0.5f; // เวลาที่ใบมีดค้างอยู่ข้างบน (วิ)
    [SerializeField] private float retractSpeed = 10f; // ความเร็วในการหดกลับลงพื้น
    [SerializeField] private int damage = 1;

    [Header("Visual References")]
    [SerializeField] private GameObject warningIndicator; // วัตถุแสดงสัญญาณเตือน (เช่น รอยตัด/วงกลมสีแดง)
    [SerializeField] private Transform bladeTransform; // วัตถุใบมีดที่จะแทงขึ้นมา
    [SerializeField] private Collider2D damageCollider; // คอลไลเดอร์ทำดาเมจของใบมีด
    [SerializeField] private float thrustHeight = 1.5f; // ความสูงที่ใบมีดแทงขึ้นมา

    private Vector3 initialBladeLocalPos;
    private bool hasHitPlayer = false;

    private void Awake()
    {
        gameObject.tag = "Enemy_Attack";

        // Safety destroy ไม่ว่าจะเกิดอะไรขึ้น ทำลายตัวเองแน่นอนหลัง 4 วินาที
        Destroy(gameObject, warningDuration + activeDuration + 3.0f);

        // หากไม่มีการกำหนด Collider ให้ค้นหาจากตัววัตถุหรือลูก
        if (damageCollider == null)
        {
            damageCollider = GetComponentInChildren<Collider2D>();
        }

        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
            damageCollider.enabled = false; // ปิดไว้ก่อนตอนเตือน
            damageCollider.gameObject.tag = "Enemy_Attack";
        }

        if (bladeTransform == null && transform.childCount > 0)
        {
            bladeTransform = transform.GetChild(0);
        }

        if (bladeTransform != null)
        {
            initialBladeLocalPos = bladeTransform.localPosition;
            // ซ่อนใบมีดไว้ใต้พื้นก่อนแทง
            bladeTransform.localPosition = initialBladeLocalPos - new Vector3(0, thrustHeight, 0);
        }
    }

    private void Start()
    {
        StartCoroutine(TrapSequenceRoutine());
    }

    private IEnumerator TrapSequenceRoutine()
    {
        // 1. Warning Phase: แสดงสัญญาณเตือน
        if (warningIndicator != null)
        {
            warningIndicator.SetActive(true);
        }

        yield return new WaitForSeconds(warningDuration);

        if (warningIndicator != null)
        {
            warningIndicator.SetActive(false);
        }

        // 2. Thrust Phase: แทงมีดขึ้นมาอย่างรวดเร็ว
        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        if (bladeTransform != null)
        {
            Vector3 targetPos = initialBladeLocalPos;
            float timeout = 0f;
            while (Vector3.Distance(bladeTransform.localPosition, targetPos) > 0.05f && timeout < 0.6f)
            {
                bladeTransform.localPosition = Vector3.MoveTowards(bladeTransform.localPosition, targetPos, thrustSpeed * Time.deltaTime);
                timeout += Time.deltaTime;
                yield return null;
            }
            bladeTransform.localPosition = targetPos;
        }

        // 3. Active Phase: ค้างใบมีดไว้ทำดาเมจชั่วขณะ
        yield return new WaitForSeconds(activeDuration);

        // ปิดดาเมจ
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        // 4. Retract Phase: หดใบมีดกลับลงพื้น
        if (bladeTransform != null)
        {
            Vector3 retractPos = initialBladeLocalPos - new Vector3(0, thrustHeight, 0);
            float timeout = 0f;
            while (Vector3.Distance(bladeTransform.localPosition, retractPos) > 0.05f && timeout < 0.6f)
            {
                bladeTransform.localPosition = Vector3.MoveTowards(bladeTransform.localPosition, retractPos, retractSpeed * Time.deltaTime);
                timeout += Time.deltaTime;
                yield return null;
            }
        }

        // ทำลาย Trap ทิ้งหลังเสร็จสิ้น
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitPlayer) return;

        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                hasHitPlayer = true;
                playerHealth.TakeDamage(damage, Vector2.up);
            }
        }
    }
}
