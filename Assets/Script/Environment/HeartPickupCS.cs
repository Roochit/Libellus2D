using UnityEngine;

public class HeartPickupCS : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 1; // จำนวนหัวใจที่จะฟื้นฟู

    [Header("Spawn Effect")]
    [SerializeField] private float bounceForce = 4f; // แรงเด้งตอนกระเด็นออกจากกล่อง
    [SerializeField] private float pickupDelay = 0.5f; // ดีเลย์ความล่าช้าก่อนจะเก็บได้หลังสปอน (วินาที)

    private float pickupTimer;

    private void Start()
    {
        pickupTimer = pickupDelay;

        // ใส่แรงเด้งออกทางด้านบนแบบสุ่มเล็กน้อยหากมี Rigidbody2D
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 randomDirection = new Vector2(Random.Range(-0.5f, 0.5f), 1f).normalized;
            rb.AddForce(randomDirection * bounceForce, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        // ลดเวลาดีเลย์ลงเรื่อยๆ
        if (pickupTimer > 0f)
        {
            pickupTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้าเวลาดีเลย์ยังไม่หมด ห้ามเก็บ
        if (pickupTimer > 0f) return;

        // ตรวจจับเมื่อผู้เล่นเดินมาชน
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                playerHealth.Heal(healAmount);
                Destroy(gameObject); // ทำลายหัวใจทิ้งหลังจากเก็บแล้ว
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ถ้าเวลาดีเลย์ยังไม่หมด ห้ามเก็บ
        if (pickupTimer > 0f) return;

        // ตรวจจับชนแบบ Solid เผื่อวัตถุตั้งค่า Collider ผิดรูปแบบ
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                playerHealth.Heal(healAmount);
                Destroy(gameObject);
            }
        }
    }
}
