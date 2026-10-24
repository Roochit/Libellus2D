using UnityEngine;

/// <summary>
/// สคริปต์ควบคุม Animation สำหรับตัวร้ายยิงธนู (Ranged Enemy)
/// ทำงานผ่านระบบ Animator Parameters และ Transitions (Trigger "Attack") 100%
/// เมื่อศัตรูถึงระยะยิงและคูลดาวน์พร้อม สคริปต์จะยิง Trigger "Attack" เข้า Animator ทันที
/// </summary>
[RequireComponent(typeof(Animator))]
public class RangedEnemyAnimationCS : MonoBehaviour
{
    [Header("การตั้งค่า Parameter ใน Animator")]
    [Tooltip("ชื่อพารามิเตอร์ Trigger ใน Animator สำหรับสั่งท่าโจมตียิงธนู")]
    [SerializeField] private string attackTriggerParam = "Attack";

    [Header("การหันหน้า (Flip)")]
    [Tooltip("เปิดถ้าต้องการให้สคริปต์นี้สลับ spriteRenderer.flipX เอง (หาก EnemyCS ควบคุม localScale อยู่แล้ว ให้ปิดไว้)")]
    [SerializeField] private bool useSpriteFlipX = false;

    private Animator animator = default;
    private EnemyCS enemyCS = default;
    private SpriteRenderer spriteRenderer = default;
    private Transform playerTransform = default;

    private int attackTriggerHash;
    private bool hasAttackParam = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        enemyCS = GetComponent<EnemyCS>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        attackTriggerHash = Animator.StringToHash(attackTriggerParam);
        CheckAnimatorParameters();
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
            enemyCS.OnAttackTriggered += PlayAttackAnimation;
        }
    }

    private void OnDisable()
    {
        if (enemyCS != null)
        {
            enemyCS.OnAttackTriggered -= PlayAttackAnimation;
        }
    }

    private void Update()
    {
        // จัดการการหันหน้าเข้าหาผู้เล่นผ่าน flipX (ถ้าเปิดใช้งาน)
        if (useSpriteFlipX && playerTransform != null && spriteRenderer != null)
        {
            float diffX = playerTransform.position.x - transform.position.x;
            if (diffX > 0.05f)
            {
                spriteRenderer.flipX = false; // หันขวา
            }
            else if (diffX < -0.05f)
            {
                spriteRenderer.flipX = true; // หันซ้าย
            }
        }
    }

    /// <summary>
    /// สั่ง Trigger ท่ายิงโจมตี (Attack) เข้า Animator
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator == null) return;

        if (hasAttackParam)
        {
            animator.SetTrigger(attackTriggerHash);
        }
        else
        {
            animator.SetTrigger("Attack");
        }
    }

    private void CheckAnimatorParameters()
    {
        if (animator == null) return;

        foreach (var param in animator.parameters)
        {
            if (param.name == attackTriggerParam)
            {
                hasAttackParam = true;
                break;
            }
        }
    }
}
