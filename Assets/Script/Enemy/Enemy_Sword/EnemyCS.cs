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
    [SerializeField] protected float attackRange = 2.4f; // ระยะเข้าโจมตีด้วยดาบ (Melee) หรือ ระยะยิง (Ranged)
    [SerializeField] protected float attackCooldown = 1.5f; // คูลดาวน์การโจมตีแต่ละครั้ง
    [SerializeField] protected float damage = 10f; // ดาเมจที่จะทำกับผู้เล่น
    [SerializeField] protected GameObject meleeHitboxPrefab; // prefab ของ Hitbox โจมตีของศัตรู
    [SerializeField] protected GameObject bulletPrefab; // prefab ของกระสุน (สำหรับ Ranged)
    [SerializeField] protected float attackOffsetDistance = 1.2f; // ระยะยื่นของ Hitbox หรือจุดยิงไปด้านหน้า

    [Header("Contact Damage Settings")]
    [SerializeField] protected bool dealsContactDamage = false; // ให้ศัตรูทำดาเมจเมื่อเดินชนตัวผู้เล่นโดยตรงหรือไม่ (สำหรับศัตรูถือดาบให้ทำดาเมจผ่านท่าฟันเป็นหลัก)

    [Header("Knockback Settings")]
    [SerializeField] protected float knockbackForce = 6f; // แรงผลักตอนโดนตีกระเด็น
    [SerializeField] protected float knockbackDuration = 0.25f; // ระยะเวลากระเด็น (วิ)
    [SerializeField] protected Color damageFlashColor = Color.red; // สีตอนกระพริบได้รับดาเมจ
    [SerializeField] protected float flashDuration = 0.15f; // ระยะเวลากระพริบสีแดง

    [Header("Health Settings")]
    [SerializeField] protected int maxHearts = 2; // จำนวนหัวใจสูงสุดของศัตรู
    protected int currentHearts;

    protected Transform playerTransform;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;

    protected float nextAttackTime = 0f;
    protected bool isAttacking = false;
    protected Coroutine attackCoroutine;
    protected bool isKnockedBack = false;
    protected float knockbackTimer = 0f;
    protected Vector2 knockbackDir = Vector2.zero;
    protected Color originalColor;
    protected Coroutine flashCoroutine;

    public System.Action OnAttackTriggered;
    public System.Action<Vector2> OnAttackWithDirection;
    public System.Action OnDamaged;

    public bool IsKnockedBack => isKnockedBack;
    public bool IsAttacking => isAttacking;
    public bool DealsContactDamage => dealsContactDamage;
    public Transform PlayerTarget => playerTransform;

    protected virtual void Awake()
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

    protected virtual void Start()
    {
        currentHearts = maxHearts;
        FindPlayer();
    }

    protected virtual void Update()
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

        // หากกำลังอยู่ในช่วงแอนิเมชันโจมตี ให้หยุดนิ่งเพื่อไม่ให้เลื่อนไถลขัดจังหวะการฟัน
        if (isAttacking)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
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

    protected virtual void FixedUpdate()
    {
        // จัดการฟิสิกส์การเคลื่อนที่และ Knockback ผ่าน Rigidbody2D (ถ้ามี)
        if (rb == null) return;

        if (isKnockedBack)
        {
            rb.velocity = knockbackDir * knockbackForce;
        }
        else if (isAttacking)
        {
            // ล็อกความเร็วให้อยู่กับที่ระหว่างเล่นแอนิเมชันโจมตี
            rb.velocity = Vector2.zero;
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

    protected virtual void FindPlayer()
    {
        // ค้นหาเป้าหมายด้วย Tag "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    protected virtual void HandleFlip(float directionX)
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

    protected virtual void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        Vector3 attackDir = (playerTransform != null) ? (playerTransform.position - transform.position).normalized : Vector3.right;
        if (attackDir == Vector3.zero) attackDir = Vector3.right;

        // สั่งให้ Animator เล่นท่าโจมตีผ่าน Event ไปยังสคริปต์ควบคุม Animation
        OnAttackTriggered?.Invoke();
        OnAttackWithDirection?.Invoke(attackDir);

        if (enemyType == EnemyType.Melee)
        {
            Debug.Log("Enemy Attacks Player with Sword!");
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(MeleeAttackRoutine(attackDir));
        }
        else if (enemyType == EnemyType.Ranged)
        {
            Debug.Log("Enemy Shoots Player!");
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(RangedAttackRoutine(attackDir));
        }
    }

    protected virtual IEnumerator MeleeAttackRoutine(Vector3 attackDir)
    {
        isAttacking = true;
        if (rb != null) rb.velocity = Vector2.zero;

        // 1. รอจังหวะง้างดาบ (Wind-up) ตามแอนิเมชันก่อนที่ดาบจะฟันลงมา
        yield return new WaitForSeconds(0.2f);

        // 2. สร้าง Hitbox โจมตีประชิดของศัตรู ณ จังหวะที่ฟัน
        if (meleeHitboxPrefab != null)
        {
            Vector3 spawnPosition = transform.position + (attackDir * attackOffsetDistance);
            float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
            Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);

            // ไม่ผูก parent กับ transform เพื่อป้องกัน scale ติดลบของศัตรูตอนหันซ้ายส่งผลต่อการหมุนของ Collider
            GameObject meleeInstance = Instantiate(meleeHitboxPrefab, spawnPosition, spawnRotation);
            Destroy(meleeInstance, 0.25f); // Hitbox มีผลในช่วงการฟัน 0.25 วินาที
        }

        // 3. รอให้แอนิเมชันท่าฟันเล่นจบครบวงสวิง (อีกประมาณ 0.45 วิ รวมระยะเวลาโจมตีเป็น 0.65 วิ)
        yield return new WaitForSeconds(0.45f);

        isAttacking = false;
        attackCoroutine = null;
    }

    protected virtual IEnumerator RangedAttackRoutine(Vector3 attackDir)
    {
        isAttacking = true;
        if (rb != null) rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.15f);

        if (bulletPrefab != null && playerTransform != null)
        {
            Vector3 spawnPosition = transform.position + (attackDir * attackOffsetDistance);
            GameObject bulletInstance = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            if (bulletInstance.TryGetComponent<EnemyBulletCS>(out var bullet))
            {
                bullet.Setup(attackDir);
            }
        }

        yield return new WaitForSeconds(0.35f);

        isAttacking = false;
        attackCoroutine = null;
    }

    public virtual void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        if (isKnockedBack) return; // ไม่ให้โดนดาเมจซ้ำขณะกระเด็นอมตะ

        currentHearts -= amount;
        Debug.Log($"Enemy hit! Current Hearts: {currentHearts}/{maxHearts}");

        OnDamaged?.Invoke();

        ApplyKnockback(knockbackDirection);

        if (currentHearts <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log("Enemy Died!");
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
        Destroy(gameObject);
    }

    public virtual void ApplyKnockback(Vector2 direction)
    {
        // ยกเลิกการโจมตีที่กำลังทำงานอยู่หากโดนผลักกระเด็น
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;

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

    protected virtual IEnumerator FlashColorRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
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

    protected virtual void OnCollisionEnter2D(Collision2D collision)
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
