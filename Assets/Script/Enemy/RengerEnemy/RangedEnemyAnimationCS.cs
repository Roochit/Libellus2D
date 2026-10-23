using System.Collections;
using UnityEngine;

/// <summary>
/// สคริปต์ควบคุม Animation สำหรับตัวร้ายยิงธนู (Ranged Enemy)
/// มี 2 ท่าหลัก:
/// 1. ท่ายืน (Idle) - Enemy_Bullet_idel
/// 2. ท่ายิง/โจมตี (Attack) - Enemy_Bullet_Attack
/// รองรับทั้งโหมด DirectPlay (สั่งเล่นผ่านโค้ดได้ทันที) และโหมด Animator Trigger ("Attack")
/// </summary>
[RequireComponent(typeof(Animator))]
public class RangedEnemyAnimationCS : MonoBehaviour
{
    public enum AnimationMode
    {
        DirectPlay,      // สั่งเล่น State โดยตรงผ่านโค้ด (ง่ายที่สุด ไม่ต้องตั้งค่า Parameter ใน Animator)
        AnimatorTrigger  // ใช้ Trigger "Attack" ใน Animator ตามมาตรฐาน Unity
    }

    [Header("โหมดการทำงาน")]
    [Tooltip("DirectPlay = เล่นชื่อคลิปตรงๆ ไม่ต้องต่อสาย, AnimatorTrigger = ใช้ Trigger Attack ใน Animator")]
    [SerializeField] private AnimationMode animationMode = AnimationMode.DirectPlay;

    [Header("ชื่อ State ใน Animator")]
    [SerializeField] private string idleStateName = "Enemy_Bullet_idel";
    [SerializeField] private string attackStateName = "Enemy_Bullet_Attack";

    [Header("การตั้งค่าเวลาโจมตี")]
    [Tooltip("ระยะเวลาการแสดงท่าโจมตี (วินาที) ก่อนจะสลับกลับเป็นท่ายืน")]
    [SerializeField] private float attackDuration = 0.35f;

    [Header("การหันหน้า (Flip)")]
    [Tooltip("เปิดถ้าต้องการให้สคริปต์นี้สลับ spriteRenderer.flipX เอง (หาก EnemyCS ควบคุม localScale อยู่แล้ว ให้ปิดไว้)")]
    [SerializeField] private bool useSpriteFlipX = false;

    private Animator animator;
    private EnemyCS enemyCS;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    private bool isAttacking = false;
    private Coroutine attackCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        enemyCS = GetComponent<EnemyCS>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // เริ่มต้นด้วยท่ายืน
        PlayIdle();
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
    /// สั่งเล่นแอนิเมชันท่ายืน (Idle)
    /// </summary>
    public void PlayIdle()
    {
        if (isAttacking) return;

        if (animator != null && animationMode == AnimationMode.DirectPlay)
        {
            animator.Play(idleStateName);
        }
    }

    /// <summary>
    /// สั่งเล่นแอนิเมชันท่ายิงโจมตี (Attack)
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator == null) return;

        if (animationMode == AnimationMode.DirectPlay)
        {
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(AttackAnimationRoutine());
        }
        else
        {
            // โหมด Animator Trigger
            animator.SetTrigger("Attack");
        }
    }

    private IEnumerator AttackAnimationRoutine()
    {
        isAttacking = true;
        animator.Play(attackStateName, 0, 0f);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        animator.Play(idleStateName);
        attackCoroutine = null;
    }
}
