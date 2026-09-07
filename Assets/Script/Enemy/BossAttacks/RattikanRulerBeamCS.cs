using System.Collections;
using UnityEngine;

/// <summary>
/// ลำแสงไม้บรรทัดยิงออกจากปากของบอสรัตติกาล (ท่าที่ 4)
/// ชาร์จเส้นเล็งเตือน แล้วยิงลำแสงไม้บรรทัดขนาดใหญ่ออกมาเป็นแนวยาวทำดาเมจ และทำลายตัวเองเมื่อหมดเวลา
/// </summary>
public class RattikanRulerBeamCS : MonoBehaviour
{
    [Header("Beam Dimensions & Damage")]
    [SerializeField] private float beamLength = 15f;
    [SerializeField] private float beamWidth = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageTickInterval = 0.5f; // ความถี่ในการทำดาเมจหากผู้เล่นยืนแช่ในลำแสง

    [Header("Timings")]
    [SerializeField] private float chargeDuration = 0.8f; // เวลาชาร์จเตือนก่อนยิง (วิ)
    [SerializeField] private float beamDuration = 1.5f; // เวลาที่ลำแสงพุ่งค้างอยู่ (วิ)

    [Header("Visuals & References")]
    [SerializeField] private LineRenderer chargeLineRenderer; // เส้นเลเซอร์เตือนตอนชาร์จ
    [SerializeField] private GameObject beamVisualObject; // วัตถุแสดงผลภาพไม้บรรทัด
    [SerializeField] private BoxCollider2D beamCollider; // คอลไลเดอร์ตรวจจับดาเมจ

    private Vector3 fireDirection = Vector3.right;
    private bool isFiringBeam = false;
    private float nextDamageTime = 0f;
    private bool isSetupCalled = false;
    private SpriteRenderer selfSpriteRenderer;

    private void Awake()
    {
        gameObject.tag = "Enemy_Attack";
        selfSpriteRenderer = GetComponent<SpriteRenderer>();

        if (beamVisualObject == null && selfSpriteRenderer != null)
        {
            beamVisualObject = gameObject;
        }

        SetupBeamCollider();
    }

    private void Start()
    {
        // Safety destroy เผื่อ Setup ไม่ถูกเรียก จะทำลายตัวเองอัตโนมัติเสมอ
        Destroy(gameObject, chargeDuration + beamDuration + 0.5f);

        if (!isSetupCalled)
        {
            StartCoroutine(FireSequenceRoutine());
        }
    }

    public void Setup(Vector3 direction, float length = -1f, float duration = -1f)
    {
        isSetupCalled = true;
        fireDirection = direction.normalized;
        if (length > 0f) beamLength = length;
        if (duration > 0f) beamDuration = duration;

        // ปรับทิศทางการหมุนของลำแสง
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // ปรับแท็กให้เป็น Enemy_Attack
        gameObject.tag = "Enemy_Attack";

        // ตั้งค่า Collider
        SetupBeamCollider();

        // รีเซ็ตตัวนับทำลายตัวเองตาม duration จริง
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), chargeDuration + beamDuration + 0.2f);

        // เริ่มลำดับการยิง
        StopAllCoroutines();
        StartCoroutine(FireSequenceRoutine());
    }

    private void SetupBeamCollider()
    {
        if (beamCollider == null)
        {
            beamCollider = GetComponent<BoxCollider2D>();
            if (beamCollider == null)
            {
                beamCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        beamCollider.isTrigger = true;
        
        // หาก Prefab มีการปรับขนาดสเกลมาแล้ว (เช่น BoosBeem สเกล 55x7) ให้ใช้สเกลเดิม
        if (transform.localScale.x > 5f)
        {
            beamCollider.size = Vector2.one;
            beamCollider.offset = Vector2.zero;
        }
        else
        {
            // จัดให้ Collider เริ่มจากจุดยิง (ปาก) ยื่นไปข้างหน้าตามความยาว beamLength
            beamCollider.size = new Vector2(beamLength, beamWidth);
            beamCollider.offset = new Vector2(beamLength * 0.5f, 0);
        }
        
        beamCollider.enabled = false; // ปิดไว้ก่อนตอนชาร์จ
    }

    private IEnumerator FireSequenceRoutine()
    {
        // 1. Charge Phase: แสดงเส้นเล็งเตือน
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = true;
            chargeLineRenderer.SetPosition(0, transform.position);
            chargeLineRenderer.SetPosition(1, transform.position + (fireDirection * beamLength));
        }

        // ซ่อนภาพไม้บรรทัดไว้ระหว่างชาร์จ
        if (selfSpriteRenderer != null && beamVisualObject == gameObject)
        {
            selfSpriteRenderer.enabled = false;
        }
        else if (beamVisualObject != null && beamVisualObject != gameObject)
        {
            beamVisualObject.SetActive(false);
        }

        yield return new WaitForSeconds(chargeDuration);

        // ปิดเส้นเล็งเตือน
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = false;
        }

        // 2. Fire Phase: เปิดลำแสงไม้บรรทัด
        isFiringBeam = true;
        if (beamCollider != null)
        {
            beamCollider.enabled = true;
        }

        if (selfSpriteRenderer != null && beamVisualObject == gameObject)
        {
            selfSpriteRenderer.enabled = true;
        }
        else if (beamVisualObject != null && beamVisualObject != gameObject)
        {
            beamVisualObject.SetActive(true);
            beamVisualObject.transform.localScale = new Vector3(beamLength, beamWidth, 1f);
        }

        yield return new WaitForSeconds(beamDuration);

        // 3. ปิดลำแสงและทำลายทิ้งทันที
        DestroySelf();
    }

    private void DestroySelf()
    {
        isFiringBeam = false;
        if (beamCollider != null) beamCollider.enabled = false;
        if (selfSpriteRenderer != null) selfSpriteRenderer.enabled = false;
        if (beamVisualObject != null) beamVisualObject.SetActive(false);

        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isFiringBeam) return;

        if (collision.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                nextDamageTime = Time.time + damageTickInterval;
                Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                if (pushDir == Vector2.zero) pushDir = fireDirection;
                playerHealth.TakeDamage(damage, pushDir);
            }
        }
    }
}
