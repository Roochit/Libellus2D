using System.Collections;
using UnityEngine;

/// <summary>
/// ลำแสงไม้บรรทัดยิงพุ่งทะลุจอมาจากข้างจอ (ท่าที่ 4)
/// ชาร์จเตือน แล้วยิงลำแสงไม้บรรทัดขนาดใหญ่พุ่งมาจากข้างจอทะลุผ่านทั้งหน้าจออย่างรวดเร็ว
/// ทำดาเมจใส่ผู้เล่นที่ขวางทาง และทำลายตัวเองเมื่อหมดเวลา
/// </summary>
public class RattikanRulerBeamCS : MonoBehaviour
{
    [Header("Beam Dimensions & Damage")]
    [SerializeField] private float beamLength = 120f; // ความยาวลำแสงให้พุ่งทะลุข้ามทั้งจอ
    [SerializeField] private float beamWidth = 10f; // ความกว้าง/ความสูงของลำแสง
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageTickInterval = 0.5f; // ความถี่ในการทำดาเมจหากผู้เล่นยืนแช่ในลำแสง

    [Header("Timings")]
    [SerializeField] private float chargeDuration = 0.8f; // เวลาชาร์จเตือนก่อนยิง (วิ)
    [SerializeField] private float beamDuration = 1.5f; // เวลาที่ลำแสงพุ่งค้างอยู่ (วิ)
    [SerializeField] private float beamRushDuration = 0.1f; // เวลาที่ลำแสงพุ่งออกจากปากไปจนสุดแมป (รวดเร็ว)

    [Header("Visuals & References")]
    [SerializeField] private LineRenderer chargeLineRenderer; // เส้นเลเซอร์เตือนตอนชาร์จ
    [SerializeField] private GameObject beamVisualObject; // วัตถุแสดงผลภาพไม้บรรทัด
    [SerializeField] private BoxCollider2D beamCollider; // คอลไลเดอร์ตรวจจับดาเมจ

    private Vector3 fireDirection = Vector3.right;
    private Vector3 spawnOrigin = Vector3.zero;
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

        if (beamCollider == null)
        {
            beamCollider = GetComponent<BoxCollider2D>();
            if (beamCollider == null)
            {
                beamCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }
        beamCollider.isTrigger = true;
        beamCollider.enabled = false;

        // สร้าง LineRenderer สำหรับเส้นเล็งเตือน (Telegraph) อัตโนมัติหากยังไม่มี
        if (chargeLineRenderer == null)
        {
            chargeLineRenderer = GetComponent<LineRenderer>();
            if (chargeLineRenderer == null)
            {
                chargeLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.useWorldSpace = true;
            chargeLineRenderer.startWidth = 0.2f;
            chargeLineRenderer.endWidth = 0.2f;
            chargeLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            chargeLineRenderer.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            chargeLineRenderer.endColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            chargeLineRenderer.positionCount = 2;
            chargeLineRenderer.sortingLayerName = "Default";
            chargeLineRenderer.sortingOrder = 10;
            chargeLineRenderer.enabled = false;
        }
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

        // รับประกันความยาวอย่างน้อย 120 หน่วยเพื่อให้พุ่งทะลุข้ามทั้งจอ
        if (length > 0f)
        {
            beamLength = Mathf.Max(length, 120f);
        }
        else
        {
            beamLength = Mathf.Max(beamLength, 120f);
        }

        if (duration > 0f) beamDuration = duration;

        // จุดกำเนิดมาจากข้างจอ
        spawnOrigin = (transform.parent != null) ? transform.parent.position : transform.position;

        // ปรับแท็กให้เป็น Enemy_Attack
        gameObject.tag = "Enemy_Attack";

        // ตั้งค่าสเกลเริ่มต้นให้ติดอยู่ที่ปากบอส
        UpdateBeamTransformAndCollider(0.1f);

        // รีเซ็ตตัวนับทำลายตัวเองตาม duration จริง
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), chargeDuration + beamDuration + 0.3f);

        // เริ่มลำดับการยิง
        StopAllCoroutines();
        StartCoroutine(FireSequenceRoutine());
    }

    /// <summary>
    /// ปรับสเกลและตำแหน่งของลำแสง ให้เริ่มจากจุดข้างจอ (spawnOrigin) และยื่นพุ่งทะลุข้ามจอตาม currentLength เสมอ
    /// </summary>
    private void UpdateBeamTransformAndCollider(float currentLength)
    {
        float targetWidth = (transform.localScale.y > 0.5f) ? transform.localScale.y : beamWidth;
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;

        // จัดตำแหน่ง World Space ให้โคนลำแสงเริ่มจากข้างจอ (spawnOrigin)
        // จุดกึ่งกลางของลำแสงยื่นไปข้างหน้าตามทิศทางยิงครึ่งหนึ่งของความยาว
        transform.rotation = Quaternion.Euler(0, 0, angle);
        transform.position = spawnOrigin + (fireDirection * (currentLength * 0.5f));
        transform.localScale = new Vector3(currentLength, targetWidth, 1f);

        if (beamCollider != null)
        {
            beamCollider.size = Vector2.one;
            beamCollider.offset = Vector2.zero;
        }
    }

    private IEnumerator FireSequenceRoutine()
    {
        // 1. Charge Phase: แสดงเส้นเล็งเตือนพุ่งไปสุดแมป
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = true;
            chargeLineRenderer.SetPosition(0, spawnOrigin);
            chargeLineRenderer.SetPosition(1, spawnOrigin + (fireDirection * beamLength));
        }

        // ซ่อนภาพไม้บรรทัดไว้ระหว่างชาร์จ
        SetAllSpriteRenderersVisible(false);

        yield return new WaitForSeconds(chargeDuration);

        // ปิดเส้นเล็งเตือน
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = false;
        }

        // 2. Fire Phase: เปิดลำแสงไม้บรรทัด และให้พุ่งผ่านทะลุออกไปจนสุดแมปอย่างรวดเร็ว
        isFiringBeam = true;
        if (beamCollider != null)
        {
            beamCollider.enabled = true;
        }

        SetAllSpriteRenderersVisible(true);

        // ลำแสงพุ่งทะยานจากปากบอสผ่านผู้เล่นไปจนสุดแมป (Rush across the map)
        float elapsed = 0f;
        while (elapsed < beamRushDuration)
        {
            elapsed += Time.deltaTime;
            float currentL = Mathf.Lerp(0.1f, beamLength, elapsed / beamRushDuration);
            UpdateBeamTransformAndCollider(currentL);
            yield return null;
        }

        // ยืดเต็มความยาวทะลุสุดแมปตลอดช่วงระยะเวลาของลำแสง
        UpdateBeamTransformAndCollider(beamLength);

        yield return new WaitForSeconds(beamDuration);

        // 3. ปิดลำแสงและทำลายทิ้งทันที
        DestroySelf();
    }

    private void SetAllSpriteRenderersVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers != null)
        {
            foreach (var sr in renderers)
            {
                sr.enabled = visible;
            }
        }

        if (beamVisualObject != null && beamVisualObject != gameObject)
        {
            beamVisualObject.SetActive(visible);
        }
    }

    private void DestroySelf()
    {
        isFiringBeam = false;
        if (beamCollider != null) beamCollider.enabled = false;
        SetAllSpriteRenderersVisible(false);

        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnTriggerStay2D(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isFiringBeam) return;

        if (collision.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                nextDamageTime = Time.time + damageTickInterval;
                Vector2 pushDir = (collision.transform.position - spawnOrigin).normalized;
                if (pushDir == Vector2.zero) pushDir = fireDirection;
                playerHealth.TakeDamage(damage, pushDir);
            }
        }
    }
}
