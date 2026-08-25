using UnityEngine;
using System.Collections;
using System.IO;

public class PlayerHealthCS : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int defaultMaxHearts = 3; // จำนวนหัวใจเริ่มต้นเมื่อเล่นครั้งแรกสุด
    private int maxHearts; // จำนวนหัวใจสูงสุดของผู้เล่นปัจจุบัน
    private int currentHearts;

    private const int ABSOLUTE_MAX_HEARTS = 10; // ลิมิตสูงสุดของหัวใจที่เก็บได้

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
        LoadHealth(); // โหลดเลือดจากไฟล์เซฟ JSON
        UpdateHeartUI(); // อัปเดตแสดงผลรูปหัวใจเริ่มต้น
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
        currentHearts = Mathf.Max(currentHearts, 0);
        UpdateHeartUI(); // อัปเดตแสดงผลรูปหัวใจ UI
        Debug.Log($"Player hit! Current Hearts: {currentHearts}/{maxHearts}");

        SaveHealth(); // บันทึกข้อมูลเลือดล่าสุดลง JSON

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
        
        DeleteSaveFile(); // ลบเซฟไฟล์เมื่อผู้เล่นตายเพื่อให้เกิดมามี 3 หัวใจใหม่

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

    // ==========================================
    // DATA PERSISTENCE (JSON SAVE/LOAD SYSTEM)
    // ==========================================

    [System.Serializable]
    private class SaveData
    {
        public int currentHearts;
        public int maxHearts;
    }

    private string GetSavePath()
    {
        string directoryPath = Path.Combine(Application.dataPath, "../db");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        return Path.Combine(directoryPath, "player_health.json");
    }

    private void SaveHealth()
    {
        try
        {
            SaveData data = new SaveData();
            data.currentHearts = currentHearts;
            data.maxHearts = maxHearts;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(), json);
            Debug.Log($"Health Saved to: {GetSavePath()} ({currentHearts}/{maxHearts})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save health: {e.Message}");
        }
    }

    private void LoadHealth()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                maxHearts = Mathf.Min(data.maxHearts, ABSOLUTE_MAX_HEARTS);
                currentHearts = Mathf.Clamp(data.currentHearts, 0, maxHearts);

                Debug.Log($"Health Loaded: {currentHearts}/{maxHearts}");
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load health: {e.Message}");
            }
        }

        // กรณีไม่มีไฟล์เซฟ (เริ่มด่านแรกครั้งแรกสุด)
        maxHearts = Mathf.Min(defaultMaxHearts, ABSOLUTE_MAX_HEARTS);
        currentHearts = maxHearts;
        SaveHealth(); // สร้างไฟล์เซฟเริ่มต้นทันที
    }

    private void DeleteSaveFile()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log("Save file deleted due to player death.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }
    }

    // ==========================================
    // HELPER METHODS (HEALING & CONTAINER UPGRADES)
    // ==========================================

    // ฟังก์ชันเพิ่มเลือด (Heal) เผื่อใช้กับไอเทมเก็บขวดเลือดตามแมป
    public void Heal(int amount)
    {
        if (currentHearts >= maxHearts) return;

        currentHearts += amount;
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        UpdateHeartUI();
        SaveHealth();
        Debug.Log($"Player healed by {amount}! Current: {currentHearts}/{maxHearts}");
    }

    // ฟังก์ชันเพิ่มหลอดเลือดสูงสุด (Heart Container) เผื่อใช้กับไอเทมเพิ่มหัวใจสูงสุดตามแมป
    public void IncreaseMaxHearts(int amount)
    {
        if (maxHearts >= ABSOLUTE_MAX_HEARTS) return;

        maxHearts += amount;
        maxHearts = Mathf.Min(maxHearts, ABSOLUTE_MAX_HEARTS);
        
        // เมื่อได้หัวใจสูงสุดเพิ่ม ให้เติมเลือดเต็ม
        currentHearts = maxHearts;

        UpdateHeartUI();
        SaveHealth();
        Debug.Log($"Player max hearts increased by {amount}! Max: {maxHearts}");
    }
}
