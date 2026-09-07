using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// สคริปต์บอส "รัตติกาล" (Boss LV01 - อาจารย์สาวสอนวิชาวาดภาพและที่ปรึกษาธีสิส)
/// สืบทอดจาก EnemyCS เพื่อให้เข้ากับระบบการต่อสู้, กระสุนผู้เล่น, และประตูด่านเดิมได้ทันที
/// ประกอบด้วย 4 ท่าโจมตีตามเอกสาร:
/// 1. ยิงดินสอ EE พิฆาต 8 ทิศ
/// 2. เรียกกระดานวาดรูปมาวิ่งวนรอบตัวเอง
/// 3. มีดคัดเตอร์เหลาดินสอ EE สุ่มแทงออกมาจากพื้น
/// 4. ยิงลำแสงไม้บรรทัดออกมาจากปาก
/// </summary>
public class RattikanBossCS : EnemyCS
{
    public enum BossPhase { Phase1, Phase2 }
    public enum BossState { Intro, Idle, Moving, Attacking, Dead }

    [Header("=== Boss Identity & Dialogue ===")]
    [SerializeField] private string bossName = "อาจารย์รัตติกาล (LV01)";
    [TextArea(2, 4)]
    [SerializeField] private string introQuote = "ก่อนจะไปซื้อเกมมาเล่น ส่งธีสิทอาจารย์ก่อนมั้ย";
    [SerializeField] private TextMeshProUGUI dialogueTextUI; // UI แสดงข้อความพูดของบอส (ถ้ามี)
    [SerializeField] private float dialogueDuration = 3f;

    [Header("=== Boss Health & Armor ===")]
    [SerializeField] private int bossMaxHearts = 25; // หลอดเลือดบอสเริ่มต้น (เช่น 25 หัวใจ)
    [SerializeField] private bool hasSuperArmor = true; // บอสไม่กระเด็นถอยหลังจากดาเมจธรรมดา
    [SerializeField] private Slider bossHealthSlider; // แถบเลือดบอสใน UI (ถ้ามี)
    [SerializeField] private TextMeshProUGUI bossHealthText; // ตัวเลขเลือดบอสใน UI (ถ้ามี)

    [Header("=== Attack Timers & Settings ===")]
    [SerializeField] private float attackIntervalPhase1 = 2.5f; // ระยะเวลาพักระหว่างท่าใน Phase 1
    [SerializeField] private float attackIntervalPhase2 = 1.6f; // ระยะเวลาพักระหว่างท่าใน Phase 2 (เมื่อเลือดต่ำกว่า 50%)
    [SerializeField] private float hoverMoveSpeed = 2.0f; // ความเร็วลอยตัว/เดินของบอส
    [SerializeField] private float preferredPlayerDistance = 4.0f; // ระยะห่างที่บอสพยายามรักษาไว้

    [Header("=== Attack 1: ดินสอ EE พิฆาต 8 ทิศ ===")]
    [SerializeField] private GameObject eePencilPrefab; // Prefab ดินสอ EE (มีสคริปต์ RattikanEEPencilCS หรือ EnemyBulletCS)
    [SerializeField] private float pencilSpeed = 8f;
    [SerializeField] private int pencilWavesPhase1 = 1; // จำนวนระลอกในเฟส 1
    [SerializeField] private int pencilWavesPhase2 = 2; // จำนวนระลอกในเฟส 2

    [Header("=== Attack 2: กระดานวาดรูปวนรอบตัวเอง ===")]
    [SerializeField] private GameObject drawingBoardPrefab; // Prefab กระดานวาดรูป
    [SerializeField] private int boardCountPhase1 = 4; // จำนวน 4 แผ่นตาม PDF
    [SerializeField] private int boardCountPhase2 = 6; // เฟส 2 เพิ่มเป็น 6 แผ่น
    [SerializeField] private float boardOrbitRadius = 2.6f;
    [SerializeField] private float boardOrbitSpeed = 130f;
    [SerializeField] private float boardDuration = 6.0f;

    [Header("=== Attack 3: มีดคัดเตอร์สุ่มแทงจากพื้น ===")]
    [SerializeField] private GameObject cutterTrapPrefab; // Prefab มีดคัดเตอร์แทงพื้น
    [SerializeField] private int cutterCountPhase1 = 6; // สุ่มแทง 6 จุด
    [SerializeField] private int cutterCountPhase2 = 10; // เฟส 2 สุ่มแทง 10 จุด
    [SerializeField] private float cutterSpawnRadius = 5.0f; // รัศมีสุ่มรอบตัวผู้เล่น

    [Header("=== Attack 4: ลำแสงไม้บรรทัดยิงจากปาก ===")]
    [SerializeField] private GameObject rulerBeamPrefab; // Prefab ลำแสงไม้บรรทัด
    [SerializeField] private Transform mouthPoint; // จุดยิงบริเวณปากของบอส
    [SerializeField] private float beamLength = 30f; // ความยาวลำแสง (ครอบคลุมทั่วห้อง)
    [SerializeField] private float beamDuration = 1.6f; // ระยะเวลาที่ลำแสงพุ่งค้างอยู่
    [SerializeField] private float horizontalYSpread = 2.5f; // ระยะสุ่มความสูงแนวนอน (กระจายขึ้น/ลง)
    [SerializeField] private int beamShotsPhase1 = 1; // จำนวนการยิงลำแสงแนวนอนใน Phase 1
    [SerializeField] private int beamShotsPhase2 = 2; // จำนวนการยิงลำแสงแนวนอนใน Phase 2

    [Header("=== Camera Zoom Settings (ตอนยิงลำแสง) ===")]
    [SerializeField] private bool zoomCameraOnBeam = true; // ซูมกล้องออกตอนยิงลำแสงไม้บรรทัด
    [SerializeField] private float beamCameraZoomSize = 10.0f; // ซูมกล้องออกเยอะๆ (เช่น 9-11 จากปกติ 5)
    [SerializeField] private float beamCameraZoomTime = 0.4f; // ความเร็วในการซูมกล้อง

    [Header("=== Boss Activation Settings ===")]
    [SerializeField] private bool waitForTrigger = false; // รอให้ผู้เล่นเดินเข้ามาใกล้หรือสั่งผ่าน Trigger ก่อนบอสจึงจะเริ่มสู้
    [SerializeField] private float triggerRadius = 8.0f; // ระยะที่บอสเริ่มสู้เมื่อผู้เล่นเดินเข้ามาใกล้
    private bool isBattleStarted = false;

    // สถานะภายใน
    private BossPhase currentPhase = BossPhase.Phase1;
    private BossState currentState = BossState.Intro;
    private int currentAttackIndex = 0;
    private Coroutine activeAttackRoutine;
    private bool isDialogueActive = false;

    protected override void Awake()
    {
        base.Awake();
        maxHearts = bossMaxHearts;
        currentHearts = bossMaxHearts;

        // หากไม่มีจุดปาก ให้ใช้ตำแหน่งหัวบอสเป็นจุดกำเนิด
        if (mouthPoint == null)
        {
            GameObject mouthObj = new GameObject("MouthPoint");
            mouthObj.transform.SetParent(transform);
            mouthObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            mouthPoint = mouthObj.transform;
        }
    }

    protected override void Start()
    {
        base.Start();
        currentHearts = bossMaxHearts;
        UpdateBossHealthUI();

        if (!waitForTrigger)
        {
            ActivateBossBattle();
        }
    }

    /// <summary>
    /// สั่งให้บอสเริ่มการต่อสู้ (สามารถเรียกผ่าน RoomTriggerCS หรือเมื่อผู้เล่นเข้าห้องได้)
    /// </summary>
    public void ActivateBossBattle()
    {
        if (isBattleStarted) return;
        isBattleStarted = true;
        StartCoroutine(BossIntroRoutine());
    }

    private IEnumerator BossIntroRoutine()
    {
        currentState = BossState.Intro;
        ShowDialogue(introQuote);

        yield return new WaitForSeconds(dialogueDuration);

        currentState = BossState.Idle;
        nextAttackTime = Time.time + 1.0f; // เริ่มต้นการโจมตีแรกหลังคุยจบ 1 วิ
    }

    protected override void Update()
    {
        if (currentState == BossState.Dead) return;

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // หากตั้งค่าให้รอผู้เล่นเดินเข้าใกล้
        if (!isBattleStarted)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= triggerRadius)
            {
                ActivateBossBattle();
            }
            return;
        }

        // หันหน้าเข้าหาผู้เล่นเสมอ
        HandleFlip(playerTransform.position.x - transform.position.x);

        // จัดการสถานะและเริ่มโจมตีเมื่อถึงเวลา
        if (currentState != BossState.Intro && currentState != BossState.Attacking)
        {
            HandleBossMovement();

            float currentAttackInterval = (currentPhase == BossPhase.Phase1) ? attackIntervalPhase1 : attackIntervalPhase2;

            if (Time.time >= nextAttackTime)
            {
                ExecuteNextBossAttack();
            }
        }
    }

    protected override void FixedUpdate()
    {
        // จัดการฟิสิกส์เฉพาะตอนไม่ได้อยู่ในท่าโจมตี
        if (currentState == BossState.Attacking || currentState == BossState.Dead)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        base.FixedUpdate();
    }

    /// <summary>
    /// การลอยตัวและรักษาระยะห่างเชิงกลยุทธ์จากผู้เล่น
    /// </summary>
    private void HandleBossMovement()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        Vector2 moveDir = Vector2.zero;

        if (distanceToPlayer < preferredPlayerDistance - 0.5f)
        {
            // ถอยห่างหากผู้เล่นประชิดเกินไป
            moveDir = (transform.position - playerTransform.position).normalized;
        }
        else if (distanceToPlayer > preferredPlayerDistance + 1.5f)
        {
            // ขยับเข้าหาหากผู้เล่นอยู่ไกลเกินไป
            moveDir = (playerTransform.position - transform.position).normalized;
        }
        else
        {
            // ลอยวนเวียนรอบๆ สร้างจังหวะการต่อสู้
            Vector2 tangent = new Vector2(-(playerTransform.position.y - transform.position.y), playerTransform.position.x - transform.position.x).normalized;
            moveDir = tangent * Mathf.Sin(Time.time * 2f);
        }

        if (rb != null)
        {
            rb.velocity = moveDir * hoverMoveSpeed;
        }
        else
        {
            transform.position += (Vector3)moveDir * hoverMoveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// ระบบหมุนเวียนท่าโจมตีทั้ง 4 ท่า
    /// </summary>
    private void ExecuteNextBossAttack()
    {
        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
        }

        currentState = BossState.Attacking;
        if (rb != null) rb.velocity = Vector2.zero;

        // สลับท่าตามลำดับ 1 -> 2 -> 3 -> 4
        switch (currentAttackIndex)
        {
            case 0:
                activeAttackRoutine = StartCoroutine(Attack1_EEPencilRadialRoutine());
                break;
            case 1:
                activeAttackRoutine = StartCoroutine(Attack2_OrbitingDrawingBoardsRoutine());
                break;
            case 2:
                activeAttackRoutine = StartCoroutine(Attack3_FloorCutterTrapsRoutine());
                break;
            case 3:
                activeAttackRoutine = StartCoroutine(Attack4_RulerBeamRoutine());
                break;
        }

        currentAttackIndex = (currentAttackIndex + 1) % 4;
    }

    // =========================================================================
    // ท่าที่ 1: ยิงดินสอ EE พิฆาต 8 ทิศ
    // =========================================================================
    private IEnumerator Attack1_EEPencilRadialRoutine()
    {
        Debug.Log("[Rattikan Boss] ใช้ท่าที่ 1: ยิงดินสอ EE พิฆาต 8 ทิศ!");

        if (animator != null) animator.SetTrigger("Attack");

        int waves = (currentPhase == BossPhase.Phase1) ? pencilWavesPhase1 : pencilWavesPhase2;

        for (int w = 0; w < waves; w++)
        {
            // ปรับมุมเหลื่อมระหว่างระลอก (เช่น ระลอกสองเอียง 22.5 องศาเพื่อเพิ่มความหลบยาก)
            float angleOffset = (w % 2 == 1) ? 22.5f : 0f;

            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f) + angleOffset;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 shootDirection = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                SpawnEEPencil(shootDirection);
            }

            yield return new WaitForSeconds(0.4f);
        }

        EndAttackAction();
    }

    private void SpawnEEPencil(Vector3 direction)
    {
        Vector3 spawnPos = mouthPoint != null ? mouthPoint.position : transform.position;
        GameObject pencilObj = null;

        if (eePencilPrefab != null)
        {
            pencilObj = Instantiate(eePencilPrefab, spawnPos, Quaternion.identity);
        }
        else if (bulletPrefab != null)
        {
            pencilObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        }

        if (pencilObj != null)
        {
            pencilObj.tag = "Enemy_Attack";
            Destroy(pencilObj, 5.0f); // รับประกันการทำลายตัวเองหลังจาก 5 วินาที

            if (pencilObj.TryGetComponent<RattikanEEPencilCS>(out var pencilScript))
            {
                pencilScript.Setup(direction, pencilSpeed);
            }
            else if (pencilObj.TryGetComponent<EnemyBulletCS>(out var fallbackBullet))
            {
                fallbackBullet.Setup(direction);
            }
            else
            {
                // หาก Prefab ไม่มีสคริปต์ ให้ใส่ RattikanEEPencilCS ให้อัตโนมัติ
                var autoPencil = pencilObj.AddComponent<RattikanEEPencilCS>();
                autoPencil.Setup(direction, pencilSpeed);
            }
        }
    }

    // =========================================================================
    // ท่าที่ 2: เรียกกระดานวาดรูปมาวิ่งวนรอบตัวเอง
    // =========================================================================
    private IEnumerator Attack2_OrbitingDrawingBoardsRoutine()
    {
        Debug.Log("[Rattikan Boss] ใช้ท่าที่ 2: เรียกกระดานวาดรูปมาวิ่งวนรอบตัวเอง!");

        if (animator != null) animator.SetTrigger("Attack");

        int boardCount = (currentPhase == BossPhase.Phase1) ? boardCountPhase1 : boardCountPhase2;
        float angleStep = 360f / boardCount;

        for (int i = 0; i < boardCount; i++)
        {
            float startAngle = i * angleStep;

            if (drawingBoardPrefab != null)
            {
                GameObject boardObj = Instantiate(drawingBoardPrefab, transform.position, Quaternion.identity);
                boardObj.tag = "Enemy_Attack";
                Destroy(boardObj, boardDuration + 3.0f); // รับประกันการทำลายตัวเอง

                if (!boardObj.TryGetComponent<RattikanDrawingBoardCS>(out var boardScript))
                {
                    boardScript = boardObj.AddComponent<RattikanDrawingBoardCS>();
                }
                boardScript.Setup(transform, startAngle, boardOrbitRadius, boardOrbitSpeed, boardDuration);
            }
        }

        yield return new WaitForSeconds(0.8f);
        EndAttackAction();
    }

    // =========================================================================
    // ท่าที่ 3: มีดคัดเตอร์เหลาดินสอ EE สุ่มแทงออกมาจากพื้น
    // =========================================================================
    private IEnumerator Attack3_FloorCutterTrapsRoutine()
    {
        Debug.Log("[Rattikan Boss] ใช้ท่าที่ 3: มีดคัดเตอร์สุ่มแทงออกมาจากพื้น!");

        if (animator != null) animator.SetTrigger("Attack");

        int cutterCount = (currentPhase == BossPhase.Phase1) ? cutterCountPhase1 : cutterCountPhase2;
        Vector3 targetCenter = playerTransform != null ? playerTransform.position : transform.position;

        for (int i = 0; i < cutterCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * cutterSpawnRadius;
            Vector3 spawnPos = targetCenter + new Vector3(randomCircle.x, randomCircle.y, 0f);

            if (cutterTrapPrefab != null)
            {
                GameObject trapObj = Instantiate(cutterTrapPrefab, spawnPos, Quaternion.identity);
                trapObj.tag = "Enemy_Attack";
                Destroy(trapObj, 4.0f); // รับประกันการทำลายตัวเองหลังจาก 4 วินาที

                if (!trapObj.TryGetComponent<RattikanCutterTrapCS>(out var trapScript))
                {
                    trapScript = trapObj.AddComponent<RattikanCutterTrapCS>();
                }
            }

            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.6f);
        EndAttackAction();
    }

    // =========================================================================
    // ท่าที่ 4: ยิงลำแสงไม้บรรทัดออกมาจากปาก (สุ่มเป็นเส้นตรงแนวนอน + ซูมกล้องออก)
    // =========================================================================
    private IEnumerator Attack4_RulerBeamRoutine()
    {
        Debug.Log("[Rattikan Boss] ใช้ท่าที่ 4: ยิงลำแสงไม้บรรทัดออกมาจากปาก (แนวนอน)!");

        // 1. สั่งซูมกล้องออกเยอะๆ เพื่อให้เห็นลานประลองกว้างๆ และลำแสงขนาดยักษ์
        ZoomCameraOut();
        yield return new WaitForSeconds(beamCameraZoomTime); // รอให้กล้องเริ่มซูมออก

        int shotCount = (currentPhase == BossPhase.Phase1) ? beamShotsPhase1 : beamShotsPhase2;

        for (int shot = 0; shot < shotCount; shot++)
        {
            if (currentState == BossState.Dead) break;

            // 2. กำหนดทิศทางเป็นเส้นตรงแนวนอน (Horizontal Straight Line: ซ้ายหรือขวา)
            float dirX = 1f;
            if (playerTransform != null)
            {
                // เล็งไปทางฝั่งที่ผู้เล่นอยู่ตามแนวแกน X
                dirX = (playerTransform.position.x >= transform.position.x) ? 1f : -1f;
            }
            else
            {
                dirX = (Random.value > 0.5f) ? 1f : -1f;
            }

            Vector3 horizontalDir = new Vector3(dirX, 0f, 0f);

            // 3. สุ่มระดับความสูงแนวนอน (Random Y Height)
            float randomYOffset = Random.Range(-horizontalYSpread, horizontalYSpread);
            float targetY = (playerTransform != null ? playerTransform.position.y : transform.position.y) + randomYOffset;

            // เคลื่อนตัวบอสตามแกน Y อย่างรวดเร็วเพื่อให้ปากตรงกับเส้นแนวนอนที่จะยิง
            float moveTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetBossPos = new Vector3(transform.position.x, targetY, transform.position.z);
            while (moveTime < 0.25f)
            {
                moveTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetBossPos, moveTime / 0.25f);
                yield return null;
            }
            transform.position = targetBossPos;

            // หันหน้าตามทิศทางแนวนอนที่จะยิง
            HandleFlip(dirX);

            if (animator != null) animator.SetTrigger("Attack");

            // 4. เสกลำแสงไม้บรรทัดแนวนอน
            Vector3 spawnPos = mouthPoint != null ? mouthPoint.position : transform.position;

            if (rulerBeamPrefab != null)
            {
                GameObject beamObj = Instantiate(rulerBeamPrefab, spawnPos, Quaternion.identity);
                beamObj.tag = "Enemy_Attack";
                Destroy(beamObj, beamDuration + 1.5f); // รับประกันการทำลายตัวเองแน่นอน

                if (!beamObj.TryGetComponent<RattikanRulerBeamCS>(out var beamScript))
                {
                    beamScript = beamObj.AddComponent<RattikanRulerBeamCS>();
                }
                beamScript.Setup(horizontalDir, beamLength, beamDuration);
            }

            // ค้างท่าระหว่างยิงลำแสง
            yield return new WaitForSeconds(beamDuration + 0.5f);
        }

        // 5. คืนค่ามุมกล้องกลับสู่ระยะปกติเมื่อยิงเสร็จ
        ZoomCameraReset();
        yield return new WaitForSeconds(beamCameraZoomTime * 0.5f);

        EndAttackAction();
    }

    private void ZoomCameraOut()
    {
        if (!zoomCameraOnBeam) return;

        if (CameraControllerCS.Instance != null)
        {
            CameraControllerCS.Instance.SetZoom(beamCameraZoomSize, beamCameraZoomTime);
        }
        else if (Camera.main != null && Camera.main.orthographic)
        {
            StopCoroutine(nameof(LerpCameraSizeRoutine));
            StartCoroutine(LerpCameraSizeRoutine(beamCameraZoomSize, beamCameraZoomTime));
        }
    }

    private void ZoomCameraReset()
    {
        if (!zoomCameraOnBeam) return;

        if (CameraControllerCS.Instance != null)
        {
            CameraControllerCS.Instance.ResetZoom(beamCameraZoomTime);
        }
        else if (Camera.main != null && Camera.main.orthographic)
        {
            StopCoroutine(nameof(LerpCameraSizeRoutine));
            StartCoroutine(LerpCameraSizeRoutine(5f, beamCameraZoomTime));
        }
    }

    private IEnumerator LerpCameraSizeRoutine(float targetSize, float duration)
    {
        if (Camera.main == null) yield break;
        float startSize = Camera.main.orthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            }
            yield return null;
        }
        if (Camera.main != null) Camera.main.orthographicSize = targetSize;
    }

    private void EndAttackAction()
    {
        currentState = BossState.Idle;
        float interval = (currentPhase == BossPhase.Phase1) ? attackIntervalPhase1 : attackIntervalPhase2;
        nextAttackTime = Time.time + interval;
    }

    // =========================================================================
    // ระบบรับดาเมจ เลือด และ Super Armor
    // =========================================================================
    public override void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        if (currentState == BossState.Dead) return;

        currentHearts -= amount;
        currentHearts = Mathf.Max(currentHearts, 0);

        Debug.Log($"[Rattikan Boss] โดนดาเมจ! เลือดคงเหลือ: {currentHearts}/{maxHearts}");

        UpdateBossHealthUI();

        // ตรวจสอบการเข้าสู่ Phase 2 (เมื่อเลือด <= 50%)
        if (currentPhase == BossPhase.Phase1 && currentHearts <= maxHearts / 2)
        {
            EnterPhase2();
        }

        // Super Armor: บอสไม่กระเด็นถอยหลังตามแรงผลัก แต่ยังคงกระพริบสีแดงเตือน
        if (hasSuperArmor)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashColorRoutine());
        }
        else
        {
            base.ApplyKnockback(knockbackDirection);
        }

        if (currentHearts <= 0)
        {
            Die();
        }
    }

    private void EnterPhase2()
    {
        currentPhase = BossPhase.Phase2;
        Debug.Log("[Rattikan Boss] เข้าสู่ Phase 2: เอาจริงแล้วนะ!");

        // เอฟเฟกต์กระพริบตัวเร็วหรือย้อมสีเพื่อบ่งบอกความดุดัน
        ShowDialogue("ส่งธีสิทตอนนี้ หรืออยากจะโดนแก้จนไม่จบ!");
    }

    protected override void Die()
    {
        currentState = BossState.Dead;
        Debug.Log("[Rattikan Boss] พ่ายแพ้แล้ว!");

        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
        }

        // ทำลายกระดานที่ยังค้างอยู่ในฉาก
        var activeBoards = FindObjectsOfType<RattikanDrawingBoardCS>();
        foreach (var b in activeBoards)
        {
            if (b != null) Destroy(b.gameObject);
        }

        ShowDialogue("รอบหน้า... อย่าลืมส่งงานให้ตรงเวลาล่ะ...");

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // ซ่อนแถบเลือด
        if (bossHealthSlider != null) bossHealthSlider.gameObject.SetActive(false);
        if (bossHealthText != null) bossHealthText.gameObject.SetActive(false);

        // คืนค่ามุมกล้องกลับสู่ขนาดปกติหากบอสตาย
        ZoomCameraReset();

        // รอ 1 วินาทีก่อนทำลายตัวบอส เพื่อเปิดทางให้ RoomDoorControllerCS ปลดล็อกประตู
        Destroy(gameObject, 1.0f);
    }

    private void UpdateBossHealthUI()
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHearts;
            bossHealthSlider.value = currentHearts;
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = $"{bossName}: {currentHearts}/{maxHearts}";
        }
    }

    public void ShowDialogue(string quote)
    {
        Debug.Log($"[Rattikan Boss] \"{quote}\"");

        if (dialogueTextUI != null)
        {
            dialogueTextUI.gameObject.SetActive(true);
            dialogueTextUI.text = quote;

            if (!isDialogueActive)
            {
                StartCoroutine(HideDialogueRoutine(dialogueDuration));
            }
        }
    }

    private IEnumerator HideDialogueRoutine(float delay)
    {
        isDialogueActive = true;
        yield return new WaitForSeconds(delay);
        if (dialogueTextUI != null)
        {
            dialogueTextUI.gameObject.SetActive(false);
        }
        isDialogueActive = false;
    }
}
