using UnityEngine;
using System.Collections;

public class EnemyCS : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }

    [Header("Enemy Type Settings")]
    [SerializeField] private EnemyType enemyType = EnemyType.Melee;

    [Header("Movement & Chase Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRadius = 8f; // ระยะที่ศัตรูเริ่มมองเห็นผู้เล่นและวิ่งเข้าหา

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f; // ระยะเข้าโจมตีด้วยดาบ (Melee) หรือ ระยะยิง (Ranged)
    [SerializeField] private float attackCooldown = 1.5f; // คูลดาวน์การโจมตีแต่ละครั้ง
    [SerializeField] private float damage = 10f; // ดาเมจที่จะทำกับผู้เล่น
    [SerializeField] private GameObject meleeHitboxPrefab; // prefab ของ Hitbox โจมตีของศัตรู
    [SerializeField] private GameObject bulletPrefab; // prefab ของกระสุน (สำหรับ Ranged)
    [SerializeField] private float attackOffsetDistance = 1.0f; // ระยะยื่นของ Hitbox หรือจุดยิงไปด้านหน้า

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 6f; // แรงผลักตอนโดนตีกระเด็น
    [SerializeField] private float knockbackDuration = 0.25f; // ระยะเวลากระเด็น (วิ)
    [SerializeField] private Color damageFlashColor = Color.red; // สีตอนกระพริบได้รับดาเมจ
    [SerializeField] private float flashDuration = 0.15f; // ระยะเวลากระพริบสีแดง

    [Header("Health Settings")]
    [SerializeField] private int maxHearts = 2; // จำนวนหัวใจสูงสุดของศัตรู
    private int currentHearts;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float nextAttackTime = 0f;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector2 knockbackDir = Vector2.zero;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            originalColor = Color.white;
        }
    }

    private void Start()
    {
        currentHearts = maxHearts;
        FindPlayer();
    }

    private void Update()
    {
        if (isKnockedBack)
        {
            // จัดการเวลาในการกระเด็นถอยหลัง
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
            }
            return; // ข้ามการทำงานเดินตาม/โจมตีหากอยู่ในสถานะกระเด็น
        }

        // ค้นหาผู้เล่นหากยังไม่มีอ้างอิง
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            HandleFlip(playerTransform.position.x - transform.position.x);

            if (distanceToPlayer <= attackRange)
            {
                // หยุดวิ่งเพื่อทำการโจมตี
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }

                if (Time.time >= nextAttackTime)
                {
                    PerformAttack();
                }
            }
            else
            {
                // เคลื่อนที่เข้าหาผู้เล่น (สำหรับอัปเดตแบบไม่มี Rigidbody2D)
                if (rb == null)
                {
                    Vector3 moveDir = (playerTransform.position - transform.position).normalized;
                    transform.position += moveDir * moveSpeed * Time.deltaTime;
                }
            }
        }
        else
        {
            // ถ้าอยู่นอกระยะตรวจจับ ให้หยุดเดิน
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void FixedUpdate()
    {
        // จัดการฟิสิกส์การเคลื่อนที่และ Knockback ผ่าน Rigidbody2D (ถ้ามี)
        if (rb == null) return;

        if (isKnockedBack)
        {
            rb.velocity = knockbackDir * knockbackForce;
        }
        else
        {
            bool isChasing = false;
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                
                // ถ้าอยู่ในระยะตามแต่ไม่อยู่ในระยะโจมตี ให้เดินตาม
                if (distanceToPlayer <= detectionRadius && distanceToPlayer > attackRange)
                {
                    Vector2 moveDir = (playerTransform.position - transform.position).normalized;
                    rb.velocity = moveDir * moveSpeed;
                    isChasing = true;
                }
            }
            
            // หากไม่ได้กระเด็นและไม่ได้กำลังเดินตามผู้เล่น ให้หยุดนิ่งเพื่อป้องกันการไหลลื่น
            if (!isChasing)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void FindPlayer()
    {
        // ค้นหาเป้าหมายด้วย Tag "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void HandleFlip(float directionX)
    {
        if (directionX > 0.01f)
        {
            // หันขวา
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < -0.01f)
        {
            // หันซ้าย
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        // เล่นอนิเมชันโจมตี (ถ้ามีพารามิเตอร์ Attack ใน Animator)
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (enemyType == EnemyType.Melee)
        {
            Debug.Log("Enemy Attacks Player with Sword!");

            // สร้าง Hitbox โจมตีประชิดของศัตรู
            if (meleeHitboxPrefab != null && playerTransform != null)
            {
                Vector3 attackDir = (playerTransform.position - transform.position).normalized;
                if (attackDir == Vector3.zero) attackDir = Vector3.right;

                Vector3 spawnPosition = transform.position + (attackDir * attackOffsetDistance);
                float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);

                // สร้าง Hitbox โดยมีศัตรูเป็น Parent (ทำให้จุดหันตามศัตรูได้)
                GameObject meleeInstance = Instantiate(meleeHitboxPrefab, spawnPosition, spawnRotation, transform);
                Destroy(meleeInstance, 0.15f); // ทำลายหลังผ่านไป 0.15 วินาที
            }
        }
        else if (enemyType == EnemyType.Ranged)
        {
            Debug.Log("Enemy Shoots Player!");

            if (bulletPrefab != null && playerTransform != null)
            {
                Vector3 attackDir = (playerTransform.position - transform.position).normalized;
                if (attackDir == Vector3.zero) attackDir = Vector3.right;

                Vector3 spawnPosition = transform.position + (attackDir * attackOffsetDistance);
                
                // สร้างกระสุน
                GameObject bulletInstance = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
                if (bulletInstance.TryGetComponent<EnemyBulletCS>(out var bullet))
                {
                    bullet.Setup(attackDir);
                }
            }
        }
    }

    public void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        if (isKnockedBack) return; // ไม่ให้โดนดาเมจซ้ำขณะกระเด็นอมตะ

        currentHearts -= amount;
        Debug.Log($"Enemy hit! Current Hearts: {currentHearts}/{maxHearts}");

        ApplyKnockback(knockbackDirection);

        if (currentHearts <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy Died!");
        Destroy(gameObject);
    }

    public void ApplyKnockback(Vector2 direction)
    {
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
        knockbackDir = direction.normalized;

        if (rb != null)
        {
            rb.velocity = Vector2.zero; // รีเซ็ตความเร็วเดิมก่อนผลักกระเด็น
        }

        // เรียกใช้งาน Effect ตัวกระพริบสีแดง
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashColorRoutine());
    }

    private IEnumerator FlashColorRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. ป้องกันไม่ให้ Enemy โดนโจมตีจากพวกเดียวกันเอง (เท็ก Enemy_Attack)
        if (collision.CompareTag("Enemy_Attack")) return;

        // 2. ตรวจจับการโดนชนด้วยการโจมตีของผู้เล่น (เท็ก Player_Attack หรือกระสุน BulletCS)
        BulletCS bullet = collision.GetComponent<BulletCS>();
        if (collision.CompareTag("Player_Attack") || bullet != null)
        {
            Vector2 direction = (transform.position - collision.transform.position).normalized;
            if (direction == Vector2.zero) direction = Vector2.right; // ป้องกันทิศทางเป็นศูนย์
            
            // หาค่าความเสียหายแบบไดนามิก
            int damageDealt = 1;
            if (bullet != null)
            {
                damageDealt = bullet.Damage;
            }
            else if (collision.TryGetComponent<PlayerMeleeAttackCS>(out var meleeAttack))
            {
                damageDealt = meleeAttack.damage;
            }
            
            TakeDamage(damageDealt, direction);
            Debug.Log($"Enemy hit by Player Attack! Damage: {damageDealt} and Knockback applied.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ป้องกันไม่ให้ Enemy โดนโจมตีจากพวกเดียวกันเอง (เท็ก Enemy_Attack)
        if (collision.gameObject.CompareTag("Enemy_Attack")) return;

        // ตรวจจับการโดนชนด้วยการโจมตีของผู้เล่น (เท็ก Player_Attack)
        if (collision.gameObject.CompareTag("Player_Attack"))
        {
            Vector2 direction = Vector2.right;
            if (playerTransform != null)
            {
                direction = (transform.position - playerTransform.position).normalized;
            }
            else
            {
                direction = (transform.position - collision.transform.position).normalized;
            }
            if (direction == Vector2.zero) direction = Vector2.right;

            // หาค่าความเสียหายแบบไดนามิก
            int damageDealt = 1;
            if (collision.gameObject.TryGetComponent<PlayerMeleeAttackCS>(out var meleeAttack))
            {
                damageDealt = meleeAttack.damage;
            }

            TakeDamage(damageDealt, direction);
            Debug.Log($"Enemy collided with Player Attack! Damage: {damageDealt} and Knockback applied.");
        }
    }
}
