using UnityEngine;
using System.Collections;

public class PlayerHealthCS : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHearts = 3; // จำนวนหัวใจสูงสุดของผู้เล่น
    private int currentHearts;

    [Header("UI Settings")]
    [SerializeField] private GameObject[] heartImages; // อาร์เรย์ของวัตถุรูปหัวใจ UI (ลากมาใส่ใน Inspector)

    [Header("Invincibility & Flash Settings")]
    [SerializeField] private float invincibleDuration = 1.0f; // ระยะเวลาอมตะหลังโดนตี (วินาที)
    [SerializeField] private float flashInterval = 0.1f; // ความถี่ในการกระพริบ (วินาที)
    [SerializeField] private float knockbackDuration = 0.2f; // ระยะเวลากระเด็นถอยหลัง (วินาที)

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor;
    private bool isInvincible = false;
    private bool isKnockedBack = false;
    private Coroutine invincibilityCoroutine;

    public bool IsInvincible => isInvincible;
    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentHearts = maxHearts;
        UpdateHeartUI(); // รีเซ็ตการแสดงผลรูปหัวใจเริ่มต้น
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            originalColor = Color.white;
        }
    }

    public void TakeDamage(int amount, Vector2 pushDirection)
    {
        if (isInvincible) return;

        currentHearts -= amount;
        UpdateHeartUI(); // อัปเดตแสดงผลรูปหัวใจ UI
        Debug.Log($"Player hit! Current Hearts: {currentHearts}/{maxHearts}");

        if (currentHearts <= 0)
        {
            Die();
        }
        else
        {
            // ผลักตัวผู้เล่นเล็กน้อย (ถ้ามี Rigidbody2D)
            if (rb != null && pushDirection != Vector2.zero)
            {
                rb.velocity = Vector2.zero;
                isKnockedBack = true;
                // ผลักผู้เล่นออกห่างจากทิศทางการโจมตีเล็กน้อย
                rb.AddForce(pushDirection * 4f, ForceMode2D.Impulse);
            }

            // เริ่มสถานะอมตะและกระพริบ
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
            }
            invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool isVisible = true;

        while (elapsed < invincibleDuration)
        {
            // หยุดแรงกระเด็นถอยหลังเมื่อครบกำหนดเวลา (เช่น 0.2 วินาที)
            if (elapsed >= knockbackDuration)
            {
                isKnockedBack = false;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
            }

            if (spriteRenderer != null)
            {
                // กระพริบโดยปรับความโปร่งแสง (Alpha) ของ SpriteRenderer
                Color flashColor = originalColor;
                flashColor.a = isVisible ? 0.2f : 1.0f;
                spriteRenderer.color = flashColor;
                isVisible = !isVisible;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // คืนค่าสีและความโปร่งแสงเดิม
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isKnockedBack = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        isInvincible = false;
    }



    private void Die()
    {
        Debug.Log("Player Died!");
        
        // ปิดการควบคุมตัวละคร (การเดินและการฟัน/ยิง)
        if (TryGetComponent<PlayerMovementCS>(out var movement)) movement.enabled = false;
        if (TryGetComponent<PlayerCombatCS>(out var combat)) combat.enabled = false;
        
        // หยุดและปิดการจำลองระบบฟิสิกส์ Rigidbody2D
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; 
        }

        // ซ่อนภาพตัวละคร (หรือแสดงแอนิเมชันตายก่อนได้)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    private void UpdateHeartUI()
    {
        if (heartImages == null) return;

        // วนลูปเพื่อเปิด/ปิดรูปภาพหัวใจตามหัวใจปัจจุบันที่มีอยู่
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                // ถ้าดัชนีของรูปหัวใจน้อยกว่าจำนวนหัวใจปัจจุบัน ให้แสดงรูปหัวใจ (Active) ถ้าไม่ใช่ให้ซ่อน (Inactive)
                heartImages[i].SetActive(i < currentHearts);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isInvincible) return;

        // 1. ตรวจจับชนกับ Hitbox โจมตีประชิดของศัตรู (เช็ค Tag "Enemy_Attack")
        if (collision.CompareTag("Enemy_Attack"))
        {
            Vector2 pushDir = (transform.position - collision.transform.position).normalized;
            if (pushDir == Vector2.zero) pushDir = Vector2.left;

            TakeDamage(1, pushDir); // โดนโจมตีลด 1 หัวใจ
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInvincible) return;

        // 2. ตรวจจับชนตัว Enemy โดยตรง (เช่น เดินชนตัวศัตรู)
        if (collision.gameObject.GetComponent<EnemyCS>() != null)
        {
            Vector2 pushDir = (transform.position - collision.transform.position).normalized;
            if (pushDir == Vector2.zero) pushDir = Vector2.left;

            TakeDamage(1, pushDir); // โดนชนลด 1 หัวใจ
        }
    }
}
