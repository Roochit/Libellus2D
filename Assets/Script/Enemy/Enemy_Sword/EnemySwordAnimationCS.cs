using UnityEngine;

/// <summary>
/// สคริปต์ควบคุม Animation สำหรับศัตรูถือดาบ (Enemy_Sword)
/// ควบคุมผ่าน Parameters ใน Animator ทั้งหมด (100% Animator Parameters & Transitions):
/// 1. isMoving (Bool) : ศัตรูกำลังเดิน (เดินลง หรือเดินข้าง)
/// 2. isWalkingUp (Bool) : ศัตรูกำลังเดินขึ้นด้านบน
/// 3. AttackSide (Trigger) : สั่งเล่นท่าฟันแนวนอน (ซ้าย/ขวา)
/// 4. AttackDown (Trigger) : สั่งเล่นท่าฟันลงล่าง
/// 5. AttackUp (Trigger) : สั่งเล่นท่าฟันขึ้นบน
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemySwordAnimationCS : MonoBehaviour
{
    [Header("ชื่อ Parameters ใน Animator")]
    [Tooltip("ชื่อพารามิเตอร์ Bool เมื่อศัตรูกำลังเคลื่อนที่")]
    [SerializeField] private string isMovingParam = "isMoving";

    [Tooltip("ชื่อพารามิเตอร์ Bool เมื่อศัตรูกำลังเดินขึ้นด้านบน")]
    [SerializeField] private string isWalkingUpParam = "isWalkingUp";

    [Tooltip("ชื่อพารามิเตอร์ Trigger ท่าฟันดาบไปข้างๆ")]
    [SerializeField] private string attackSideParam = "AttackSide";

    [Tooltip("ชื่อพารามิเตอร์ Trigger ท่าฟันดาบลงล่าง")]
    [SerializeField] private string attackDownParam = "AttackDown";

    [Tooltip("ชื่อพารามิเตอร์ Trigger ท่าฟันดาบขึ้นบน")]
    [SerializeField] private string attackUpParam = "AttackUp";

    [Header("การหันหน้า (Flip)")]
    [Tooltip("เปิดหากต้องการให้สคริปต์นี้ปรับ spriteRenderer.flipX เอง (หาก EnemyCS ควบคุม localScale อยู่แล้ว ให้ปิดไว้)")]
    [SerializeField] private bool useSpriteFlipX = false;

    // References
    private Animator animator = default;
    private EnemyCS enemyCS = default;
    private Rigidbody2D rb = default;
    private SpriteRenderer spriteRenderer = default;
    private Transform playerTransform = default;

    // Hashes เพื่อประสิทธิภาพและความเร็ว
    private int isMovingHash;
    private int isWalkingUpHash;
    private int attackSideHash;
    private int attackDownHash;
    private int attackUpHash;
    private int generalAttackHash;

    // ตรวจสอบ Parameter ที่มีอยู่จริงใน Animator
    private bool hasParamIsMoving = false;
    private bool hasParamIsWalkingUp = false;
    private bool hasParamAttackSide = false;
    private bool hasParamAttackDown = false;
    private bool hasParamAttackUp = false;
    private bool hasParamGeneralAttack = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        enemyCS = GetComponent<EnemyCS>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // แฮชชื่อ Parameter
        isMovingHash = Animator.StringToHash(isMovingParam);
        isWalkingUpHash = Animator.StringToHash(isWalkingUpParam);
        attackSideHash = Animator.StringToHash(attackSideParam);
        attackDownHash = Animator.StringToHash(attackDownParam);
        attackUpHash = Animator.StringToHash(attackUpParam);
        generalAttackHash = Animator.StringToHash("Attack");

        CheckAvailableParameters();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void OnEnable()
    {
        if (enemyCS != null)
        {
            enemyCS.OnAttackWithDirection += HandleAttackWithDirection;
        }
    }

    private void OnDisable()
    {
        if (enemyCS != null)
        {
            enemyCS.OnAttackWithDirection -= HandleAttackWithDirection;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // อ้างอิงผู้เล่นเพื่อคำนวณการหันหน้าและทิศทาง
        if (playerTransform == null)
        {
            if (enemyCS != null && enemyCS.PlayerTarget != null)
            {
                playerTransform = enemyCS.PlayerTarget;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }

        // การหันหน้าซ้าย-ขวาผ่าน spriteRenderer.flipX (ถ้าเปิดใช้งาน)
        if (useSpriteFlipX && playerTransform != null && spriteRenderer != null)
        {
            float diffX = playerTransform.position.x - transform.position.x;
            if (diffX > 0.05f) spriteRenderer.flipX = false;
            else if (diffX < -0.05f) spriteRenderer.flipX = true;
        }

        UpdateMovementParameters();
    }

    /// <summary>
    /// อัปเดตพารามิเตอร์การเคลื่อนที่เข้า Animator
    /// </summary>
    private void UpdateMovementParameters()
    {
        // หากศัตรูกำลังเล่นท่าโจมตีอยู่ ให้ตั้ง isMoving เป็น false เสมอเพื่อไม่ให้ไปตัดแอนิเมชันโจมตี
        if (enemyCS != null && enemyCS.IsAttacking)
        {
            if (hasParamIsMoving) animator.SetBool(isMovingHash, false);
            return;
        }

        Vector2 velocity = (rb != null) ? rb.velocity : Vector2.zero;
        bool isMoving = velocity.magnitude > 0.1f;

        // เช็คว่ากำลังเดินขึ้นด้านบนหรือไม่ (แกน Y เป็นบวกและมากกว่าแกน X)
        bool isWalkingUp = false;
        if (isMoving)
        {
            isWalkingUp = (velocity.y > 0.2f && velocity.y > Mathf.Abs(velocity.x));
        }

        if (hasParamIsMoving)
        {
            animator.SetBool(isMovingHash, isMoving);
        }

        if (hasParamIsWalkingUp)
        {
            animator.SetBool(isWalkingUpHash, isWalkingUp);
        }
    }

    /// <summary>
    /// สั่ง Trigger ท่าฟันตามทิศทางที่ส่งมาจาก EnemyCS
    /// </summary>
    private void HandleAttackWithDirection(Vector2 attackDir)
    {
        if (animator == null) return;

        // คำนวณว่าทิศทางเป้าหมายเน้นแนวตั้ง (ขึ้น/ลง) หรือแนวนอน (ข้าง)
        if (Mathf.Abs(attackDir.y) > Mathf.Abs(attackDir.x))
        {
            if (attackDir.y > 0)
            {
                // ฟันขึ้นบน (AttackUp)
                if (hasParamAttackUp) animator.SetTrigger(attackUpHash);
                else if (hasParamGeneralAttack) animator.SetTrigger(generalAttackHash);
            }
            else
            {
                // ฟันลงล่าง (AttackDown)
                if (hasParamAttackDown) animator.SetTrigger(attackDownHash);
                else if (hasParamGeneralAttack) animator.SetTrigger(generalAttackHash);
            }
        }
        else
        {
            // ฟันแนวนอน (AttackSide) ระบบจะหันซ้าย/ขวาผ่าน Flip localScale หรือ FlipX
            if (hasParamAttackSide) animator.SetTrigger(attackSideHash);
            else if (hasParamGeneralAttack) animator.SetTrigger(generalAttackHash);
        }
    }

    /// <summary>
    /// ตรวจสอบ Parameters ที่มีอยู่จริงใน Animator Controller ปัจจุบัน
    /// </summary>
    private void CheckAvailableParameters()
    {
        if (animator == null) return;

        foreach (var param in animator.parameters)
        {
            if (param.name == isMovingParam) hasParamIsMoving = true;
            else if (param.name == isWalkingUpParam) hasParamIsWalkingUp = true;
            else if (param.name == attackSideParam) hasParamAttackSide = true;
            else if (param.name == attackDownParam) hasParamAttackDown = true;
            else if (param.name == attackUpParam) hasParamAttackUp = true;
            else if (param.name == "Attack") hasParamGeneralAttack = true;
        }
    }
}
