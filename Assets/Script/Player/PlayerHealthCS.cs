using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;

public class PlayerHealthCS : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int defaultMaxHearts = 3; // จำนวนหัวใจเริ่มต้นเมื่อเล่นครั้งแรกสุด
    private int maxHearts; // จำนวนหัวใจสูงสุดของผู้เล่นปัจจุบัน
    private int currentHearts;

    private const int ABSOLUTE_MAX_HEARTS = 10; // ลิมิตสูงสุดของหัวใจที่เก็บได้

    [Header("UI Settings")]
    [SerializeField] private GameObject[] heartImages = default; // อาร์เรย์ของวัตถุรูปหัวใจ UI (ลากมาใส่ใน Inspector)

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
        string currentSceneName = SceneManager.GetActiveScene().name;

        // กฎ: Player_health.json จะเริ่มต้นและตายเริ่มใหม่ที่ 3 หัวใจแค่ด่าน Lv00 และ Lv01 เท่านั้น
        if (IsResetToThreeHeartsScene(currentSceneName))
        {
            maxHearts = Mathf.Min(defaultMaxHearts, ABSOLUTE_MAX_HEARTS); // เริ่มต้นที่ 3 หัวใจ
            currentHearts = maxHearts; // เลือดเริ่มต้น 3
            SaveHealth();
            Debug.Log($"[PlayerHealth] เริ่มต้นด่าน {currentSceneName}: รีเซ็ตเลือดเป็น {currentHearts}/{maxHearts} หัวใจ");
        }
        else
        {
            // ด่านอื่นๆ (เช่น LV02, BossLV01): โหลดข้อมูลเลือดและจำนวนหัวใจสูงสุดเดิมจาก DB (JSON)
            LoadHealth();
        }

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

        string currentSceneName = SceneManager.GetActiveScene().name;
        // จัดการเลือดสำหรับเตรียมเกิดใหม่:
        // ด่าน LV00 และ LV01 จะรีเซ็ตเป็น 3 หัวใจ
        // ด่านอื่นๆ จะรักษา maxHearts เดิมไว้ และฟื้นฟูเลือดเต็ม
        ResetHealthForDeathRestart(currentSceneName);

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

        // แสดงหน้าต่าง Game Over และหยุดเกม (Pause Game)
        GameOverManagerCS.Instance.ShowGameOver();
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
        // การเดินชนตัวศัตรูจะไม่ลดเลือด — เลือดจะลดเฉพาะเมื่อถูกโจมตีด้วยท่าฟัน (Hitbox Tag: "Enemy_Attack") หรือการโจมตีอื่นๆ เท่านั้น
    }

    // ==========================================
    // DATA PERSISTENCE (JSON SAVE/LOAD SYSTEM)
    // ==========================================

    [System.Serializable]
    public class SaveData
    {
        public int currentHearts;
        public int maxHearts;
    }

    /// <summary>
    /// ตรวจสอบว่าเป็นด่านที่ต้องเริ่มต้นและตายเกิดใหม่ที่ 3 หัวใจหรือไม่ (LV00, LV01 เท่านั้น)
    /// </summary>
    public static bool IsResetToThreeHeartsScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return sceneName.Equals("LV00", System.StringComparison.OrdinalIgnoreCase) ||
               sceneName.Equals("LV01", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// คืนค่า Path ของ db/player_health.json
    /// </summary>
    public static string GetSaveFilePath()
    {
        string directoryPath = Path.Combine(Application.dataPath, "../db");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        return Path.Combine(directoryPath, "player_health.json");
    }

    private string GetSavePath()
    {
        return GetSaveFilePath();
    }

    /// <summary>
    /// บันทึกข้อมูลเลือดลงไฟล์ JSON โดยตรง
    /// </summary>
    public static void SaveDataToFile(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSaveFilePath(), json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerHealth] Failed to save health data to file: {e.Message}");
        }
    }

    /// <summary>
    /// รีเซ็ตเลือดเมื่อตายและเริ่มด่านใหม่:
    /// - ด่าน LV00 และ LV01: รีเซ็ตเป็น 3 หัวใจเต็มเสมอ
    /// - ด่านอื่นๆ (LV02 ฯลฯ): รักษาระดับ maxHearts เดิมที่สะสมมาไว้ และคืนเลือดเต็มตาม maxHearts นั้น
    /// </summary>
    public static void ResetHealthForDeathRestart(string sceneName)
    {
        string path = GetSaveFilePath();

        if (IsResetToThreeHeartsScene(sceneName))
        {
            SaveData data = new SaveData
            {
                currentHearts = 3,
                maxHearts = 3
            };
            SaveDataToFile(data);
            Debug.Log($"[PlayerHealth] รีเซ็ตเลือดเริ่มใหม่เมื่อตายในด่าน {sceneName}: 3/3 หัวใจ");
        }
        else
        {
            // ด่านอื่นๆ: ค้นหาจำนวน maxHearts เดิมที่บันทึกไว้
            int maxH = 3;
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                    if (loaded != null && loaded.maxHearts > 0)
                    {
                        maxH = Mathf.Min(loaded.maxHearts, ABSOLUTE_MAX_HEARTS);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerHealth] อ่านไฟล์เซฟไม่สำเร็จ: {e.Message}");
                }
            }

            SaveData data = new SaveData
            {
                currentHearts = maxH,
                maxHearts = maxH
            };
            SaveDataToFile(data);
            Debug.Log($"[PlayerHealth] คืนเลือดเต็มเมื่อตายในด่าน {sceneName}: {maxH}/{maxH} หัวใจ (รักษา maxHearts เดิม)");
        }
    }

    private void SaveHealth()
    {
        SaveData data = new SaveData
        {
            currentHearts = currentHearts,
            maxHearts = maxHearts
        };
        SaveDataToFile(data);
        Debug.Log($"Health Saved to: {GetSavePath()} ({currentHearts}/{maxHearts})");
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
                // หากเลือดที่บันทึกไว้เป็น 0 (เพิ่งตายแล้วเริ่มใหม่) ให้คืนเลือดเต็ม maxHearts
                currentHearts = (data.currentHearts <= 0) ? maxHearts : Mathf.Clamp(data.currentHearts, 1, maxHearts);

                Debug.Log($"Health Loaded: {currentHearts}/{maxHearts}");
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load health: {e.Message}");
            }
        }

        // กรณีไม่มีไฟล์เซฟ (Fallback)
        maxHearts = Mathf.Min(defaultMaxHearts, ABSOLUTE_MAX_HEARTS); // เริ่มต้นที่ 3 หัวใจสูงสุด
        currentHearts = maxHearts; // เลือดปัจจุบันเริ่มต้นที่ 3
        SaveHealth(); // สร้างไฟล์เซฟเริ่มต้นทันที
    }

    public void DeleteSaveFile()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log("Save file deleted.");
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
        // หากพลังชีวิตเต็มอยู่แล้ว ให้ขยายช่องหัวใจสูงสุดเพิ่มแทน
        if (currentHearts >= maxHearts)
        {
            IncreaseMaxHearts(amount);
            return;
        }

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
