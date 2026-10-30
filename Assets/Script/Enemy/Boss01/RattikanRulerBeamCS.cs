using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ลำแสงไม้บรรทัดต่อกันเป็นแนวยาวเต็มจอ (ท่าที่ 4 ของบอสรัตติกาล)
/// - นำภาพไม้บรรทัด (Ruler.PNG) มาเรียงต่อกันเป็นแนวยาวพาดผ่านจอแบบเต็มจอเสมอ ไม่ดึงยืดภาพ
/// - เอฟเฟกต์ยิงเป็นเส้นเล็กๆ ในเสี้ยววินาทีแรก แล้วสเกลขยายความหนาขึ้นมาสู่ขนาดเต็ม (Scale Up)
/// - มีจังหวะพุ่งเคลื่อนที่จากซ้ายไปขวาด้วยความเร็วสูงระหว่างขั้น 4 (Full Power) กับขั้น 5 (Shrink)
/// - ปรับขนาด Hitbox BoxCollider2D ให้ตรงกับลำแสงแบบ Real-time ตลอดเวลา
/// </summary>
public class RattikanRulerBeamCS : MonoBehaviour
{
    [Header("Ruler Tiling (วางต่อกัน 10 อัน)")]
    [Tooltip("จำนวนไม้บรรทัดที่นำมาวางเรียงต่อกันเป็นแนวยาว (ค่าเริ่มต้น 10 อัน)")]
    [SerializeField] private int segmentCount = 10;
    [Tooltip("ความหนาของลำแสงเมื่อขยายเต็มที่ (World Scale)")]
    [SerializeField] private float beamThickness = 1.6f;
    [Tooltip("สัดส่วนความหนาเริ่มต้นตอนเป็นเส้นเล็ก (เช่น 0.08 คือ 8% ของขนาดเต็ม)")]
    [SerializeField] private float thinThicknessRatio = 0.08f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageTickInterval = 0.35f;

    [Header("Continuous Slide Movement (เลื่อนจากซ้ายไปขวาอย่างต่อเนื่องตลอดเวลา)")]
    [Tooltip("ระยะที่เลื่อนไปทางขวาอย่างรวดเร็วในตอนขยาย (ช่วง A)")]
    [SerializeField] private float fastSlideDistance = 35f;
    [Tooltip("ระยะเวลาในการเลื่อนเร็วพร้อมขยายความหนา (วินาที)")]
    [SerializeField] private float fastExpandDuration = 0.35f;

    [Tooltip("ระยะที่เลื่อนต่อไปทางขวาในตอนค้างพลังเต็มที่ (ช่วง B - ไม่หยุดนิ่ง)")]
    [SerializeField] private float holdSlideDistance = 30f;
    [Tooltip("ระยะเวลาคงพลังเต็มที่ขณะเลื่อนต่อเนื่อง (วินาที)")]
    [SerializeField] private float holdSlideDuration = 0.85f;

    [Tooltip("ระยะที่เลื่อนต่อไปทางขวาในตอนจบขณะย่อขนาดลง (ช่วง C)")]
    [SerializeField] private float shrinkSlideDistance = 12f;
    [Tooltip("ระยะเวลาในการย่อขนาดลงจนหายไป (วินาที)")]
    [SerializeField] private float shrinkDuration = 0.2f;

    [Header("Charge Telegraph (เส้นเล็งเตือน)")]
    [Tooltip("ระยะเวลาเส้นเล็งเตือนสีแดง (Telegraph) ก่อนยิง")]
    [SerializeField] private float chargeDuration = 0.8f;

    [Header("Visuals & References")]
    [SerializeField] private LineRenderer chargeLineRenderer; // เส้นเลเซอร์เตือนตอนชาร์จ
    [SerializeField] private BoxCollider2D beamCollider; // คอลไลเดอร์ตรวจจับดาเมจ

    public float TotalSlideDistance => fastSlideDistance + holdSlideDistance + shrinkSlideDistance;
    public int SegmentCount => segmentCount;
    public float TotalActiveDuration => fastExpandDuration + holdSlideDuration + shrinkDuration;

    private Vector3 fireDirection = Vector3.right;
    private Vector3 spawnOrigin = Vector3.zero;
    private bool isFiringBeam = false;
    private float nextDamageTime = 0f;
    private bool isSetupCalled = false;
    private SpriteRenderer selfSpriteRenderer;
    private Vector2 nativeSpriteSize = new Vector2(20.48f, 1.28f);
    private readonly List<GameObject> activeSegments = new List<GameObject>();

    private void Awake()
    {
        gameObject.tag = "Enemy_Attack";
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (selfSpriteRenderer == null)
        {
            selfSpriteRenderer = GetComponent<SpriteRenderer>();
            if (selfSpriteRenderer == null)
            {
                selfSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (selfSpriteRenderer != null)
        {
            if (selfSpriteRenderer.sprite != null)
            {
                nativeSpriteSize = selfSpriteRenderer.sprite.bounds.size;
            }

            if (selfSpriteRenderer.sortingOrder <= 0)
            {
                selfSpriteRenderer.sortingOrder = 8;
            }
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

        // LineRenderer เส้นเล็งเตือนตอนชาร์จ
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
            chargeLineRenderer.startWidth = 0.25f;
            chargeLineRenderer.endWidth = 0.25f;
            chargeLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            chargeLineRenderer.startColor = new Color(1f, 0.2f, 0.2f, 0.8f);
            chargeLineRenderer.endColor = new Color(1f, 0.2f, 0.2f, 0.8f);
            chargeLineRenderer.positionCount = 2;
            chargeLineRenderer.sortingLayerName = "Default";
            chargeLineRenderer.sortingOrder = 10;
            chargeLineRenderer.enabled = false;
        }
    }

    private void Start()
    {
        EnsureComponents();

        if (!isSetupCalled)
        {
            float totalDuration = chargeDuration + fastExpandDuration + holdSlideDuration + shrinkDuration + 0.5f;
            CancelInvoke(nameof(DestroySelf));
            Invoke(nameof(DestroySelf), totalDuration);
            StartCoroutine(BeamSequenceRoutine());
        }
    }

    /// <summary>
    /// กำหนดทิศทาง ความยาวลำแสงทั้งหมด และระยะเวลาคงอยู่
    /// </summary>
    public void Setup(Vector3 direction, float distance = -1f, float duration = -1f)
    {
        isSetupCalled = true;
        EnsureComponents();

        fireDirection = (direction != Vector3.zero) ? direction.normalized : Vector3.right;

        if (duration > 0f)
        {
            float animParts = fastExpandDuration + shrinkDuration;
            holdSlideDuration = Mathf.Max(duration - animParts, 0.4f);
        }

        spawnOrigin = (transform.parent != null) ? transform.parent.position : transform.position;
        gameObject.tag = "Enemy_Attack";

        // วางวัตถุไว้ที่จุดกำเนิดและหันตามแกนแนวนอน
        transform.rotation = Quaternion.identity;
        transform.position = spawnOrigin;
        transform.localScale = Vector3.one;

        // นำภาพไม้บรรทัดมาเรียงต่อกัน 10 อันเป็นแนวยาว (Tiled Segments)
        BuildTiledBeam();

        // ซ่อนชิ้นส่วนไว้ก่อนในระหว่างชาร์จเตือน
        SetAllSpriteRenderersVisible(false);

        float totalDuration = chargeDuration + fastExpandDuration + holdSlideDuration + shrinkDuration + 1.0f;
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), totalDuration);

        StopAllCoroutines();
        StartCoroutine(BeamSequenceRoutine());
    }

    /// <summary>
    /// นำไม้บรรทัดมาเรียงต่อกัน 10 อันตามที่กำหนดโดยไม่ดึงยืดภาพ
    /// แต่ละชิ้นส่วนคงสัดส่วน 1:1 เดิม คมชัด ไม่เบี้ยว
    /// </summary>
    private void BuildTiledBeam()
    {
        float segWidth = nativeSpriteSize.x > 0.001f ? nativeSpriteSize.x : 20.48f;
        int count = Mathf.Max(segmentCount, 10);

        // ล้างชิ้นส่วนเก่าหากมี
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] != null && activeSegments[i] != gameObject)
            {
                Destroy(activeSegments[i]);
            }
        }
        activeSegments.Clear();

        // ซ่อน SpriteRenderer ของตัวแม่ เพราะใช้ชิ้นส่วนลูกแสดงผลแทน
        if (selfSpriteRenderer != null)
        {
            selfSpriteRenderer.enabled = false;
        }

        // เสกชิ้นส่วนไม้บรรทัด 10 อันต่อกันไปตามแกน Local X (ยื่นไปข้างหน้าตามทิศทางยิง)
        for (int i = 0; i < count; i++)
        {
            GameObject segObj = new GameObject($"RulerSeg_{i}");
            segObj.transform.SetParent(transform, false);

            float segCenterX = (i * segWidth) + (segWidth * 0.5f);
            segObj.transform.localPosition = new Vector3(segCenterX, 0f, 0f);
            segObj.transform.localRotation = Quaternion.identity;
            segObj.transform.localScale = Vector3.one;

            var sr = segObj.AddComponent<SpriteRenderer>();
            sr.sprite = (selfSpriteRenderer != null) ? selfSpriteRenderer.sprite : null;
            if (selfSpriteRenderer != null && selfSpriteRenderer.sharedMaterial != null)
            {
                sr.sharedMaterial = selfSpriteRenderer.sharedMaterial;
            }
            sr.color = (selfSpriteRenderer != null) ? selfSpriteRenderer.color : Color.white;
            sr.sortingLayerID = (selfSpriteRenderer != null) ? selfSpriteRenderer.sortingLayerID : 0;
            sr.sortingOrder = (selfSpriteRenderer != null) ? selfSpriteRenderer.sortingOrder : 8;

            activeSegments.Add(segObj);
        }

        // ตั้งค่าขนาดเริ่มต้นของ BoxCollider2D ให้ครอบคลุมตลอดความยาว 10 ไม้บรรทัด
        if (beamCollider != null)
        {
            float actualLength = count * segWidth;
            beamCollider.size = new Vector2(actualLength, nativeSpriteSize.y * beamThickness);
            beamCollider.offset = new Vector2(actualLength * 0.5f, 0f);
        }
    }

    /// <summary>
    /// ปรับความหนา (Y-Scale) ของลำแสงไม้บรรทัดทั้งหมดในเวลาเดียวกัน
    /// พร้อมปรับขนาดความสูงของ BoxCollider2D ให้ตรงกับภาพเป๊ะๆ
    /// </summary>
    private void UpdateBeamThickness(float thickness)
    {
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] != null)
            {
                activeSegments[i].transform.localScale = new Vector3(1f, thickness, 1f);
            }
        }

        if (beamCollider != null)
        {
            float segWidth = nativeSpriteSize.x > 0.001f ? nativeSpriteSize.x : 20.48f;
            float actualLength = activeSegments.Count * segWidth;
            beamCollider.size = new Vector2(actualLength, nativeSpriteSize.y * thickness);
            beamCollider.offset = new Vector2(actualLength * 0.5f, 0f);
        }
    }

    private IEnumerator BeamSequenceRoutine()
    {
        Camera cam = Camera.main;
        float camX = cam != null ? cam.transform.position.x : transform.position.x;
        float camHalfWidth = cam != null ? (cam.orthographicSize * cam.aspect) : 40f;

        // 1. Charge Phase: แสดงเส้นเล็งเตือนสีแดงพาดผ่านจอแบบเต็มจอเสมอ
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = true;
            float leftTelegraphX = camX - camHalfWidth - 40f;
            float rightTelegraphX = camX + camHalfWidth + 40f;

            chargeLineRenderer.SetPosition(0, new Vector3(leftTelegraphX, spawnOrigin.y, 0f));
            chargeLineRenderer.SetPosition(1, new Vector3(rightTelegraphX, spawnOrigin.y, 0f));
        }

        SetAllSpriteRenderersVisible(false);

        float chargeElapsed = 0f;
        while (chargeElapsed < chargeDuration)
        {
            chargeElapsed += Time.deltaTime;
            if (chargeLineRenderer != null)
            {
                float alpha = Mathf.PingPong(chargeElapsed * 6f, 0.6f) + 0.35f;
                chargeLineRenderer.startColor = new Color(1f, 0.15f, 0.15f, alpha);
                chargeLineRenderer.endColor = new Color(1f, 0.15f, 0.15f, alpha);
            }
            yield return null;
        }

        // ปิดเส้นเล็งเตือน
        if (chargeLineRenderer != null)
        {
            chargeLineRenderer.enabled = false;
        }

        // 2. เริ่มเปิดการทำงานของลำแสง 10 ไม้บรรทัด
        isFiringBeam = true;
        if (beamCollider != null)
        {
            beamCollider.enabled = true;
        }

        SetAllSpriteRenderersVisible(true);
        float thinThickness = Mathf.Max(beamThickness * thinThicknessRatio, 0.05f);
        UpdateBeamThickness(thinThickness);

        Vector3 initialPos = transform.position;

        // =========================================================================
        // Step A: ตอนย่อขยายให้เลื่อนจากซ้ายไปขวาแบบเร็วๆ (Fast Slide & Expand)
        // =========================================================================
        Vector3 step1Start = initialPos;
        Vector3 step1End = step1Start + (Vector3.right * fastSlideDistance);

        float elapsed = 0f;
        while (elapsed < fastExpandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fastExpandDuration);
            float smoothT = t * t * (3f - 2f * t);

            // เลื่อนจากซ้ายไปขวาเร็วๆ
            transform.position = Vector3.Lerp(step1Start, step1End, smoothT);

            // ขยายสเกลความหนาจากเส้นบางเป็นขนาดเต็มไปพร้อมกัน
            float currentT = Mathf.Lerp(thinThickness, beamThickness, smoothT);
            UpdateBeamThickness(currentT);

            yield return null;
        }

        transform.position = step1End;
        UpdateBeamThickness(beamThickness);

        // =========================================================================
        // Step B: ตอนค้างก็เลื่อนด้วย ไม่ค้างนิ่ง (Continuous Slide at Full Power)
        // =========================================================================
        Vector3 step2Start = step1End;
        Vector3 step2End = step2Start + (Vector3.right * holdSlideDistance);

        elapsed = 0f;
        while (elapsed < holdSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / holdSlideDuration);

            // เลื่อนไปทางขวาอย่างต่อเนื่อง ไม่หยุดนิ่งอยู่กับที่
            transform.position = Vector3.Lerp(step2Start, step2End, t);

            yield return null;
        }

        transform.position = step2End;

        // =========================================================================
        // Step C: ตอนจบเลื่อนไปเเล้วย่อขนาดลงจนหายไป (Slide & Shrink to Vanish)
        // =========================================================================
        Vector3 step3Start = step2End;
        Vector3 step3End = step3Start + (Vector3.right * shrinkSlideDistance);

        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);

            // เลื่อนไปข้างหน้าอย่างต่อเนื่อง
            transform.position = Vector3.Lerp(step3Start, step3End, t);

            // ย่อขนาดความหนาลงเรื่อยๆ จนหายไป
            float currentT = Mathf.Lerp(beamThickness, 0.005f, t * t);
            UpdateBeamThickness(currentT);

            yield return null;
        }

        transform.position = step3End;
        UpdateBeamThickness(0f);

        // ดับลำแสงและทำลายตัวเอง
        DestroySelf();
    }

    private void SetAllSpriteRenderersVisible(bool visible)
    {
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] != null)
            {
                activeSegments[i].SetActive(visible);
            }
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
                // ผลักผู้เล่นไปทางขวาและเสยขึ้นเล็กน้อยตามทิศทางการเลื่อนของลำแสง
                Vector2 pushDir = (Vector2.right * 0.85f + Vector2.up * 0.35f).normalized;
                playerHealth.TakeDamage(damage, pushDir);
            }
        }
    }
}
