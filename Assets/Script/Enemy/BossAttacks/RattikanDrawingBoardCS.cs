using UnityEngine;

/// <summary>
/// กระสุนกระดานวาดรูปวิ่งวนรอบตัวเองของบอสรัตติกาล (ท่าที่ 2)
/// โคจรรอบตัวบอส ทำดาเมจใส่ผู้เล่นหากเข้าใกล้ บล็อกกระสุนของผู้เล่น และทำลายตัวเองเมื่อครบเวลา
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RattikanDrawingBoardCS : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 120f; // องศาต่อวินาที
    [SerializeField] private int damage = 1;
    [SerializeField] private bool canBlockBullets = true;

    [Header("End of Orbit Behavior")]
    [SerializeField] private bool shootAtPlayerOnExpire = false; // เมื่อหมดเวลา ให้พุ่งใส่ผู้เล่นหรือไม่
    [SerializeField] private float shootSpeed = 10f;

    private Transform centerTarget;
    private float currentAngle;
    private float currentRadius;
    private bool isOrbiting = false;
    private bool isLaunched = false;
    private Vector3 launchDirection;

    private void Awake()
    {
        gameObject.tag = "Enemy_Attack";
        // Safety destroy เผื่อ Setup ไม่ถูกเรียก หรือเกิดข้อผิดพลาด
        Destroy(gameObject, 12f);
    }

    public void Setup(Transform center, float startAngle, float radius = 2.5f, float speed = 120f, float duration = 6f)
    {
        centerTarget = center;
        currentAngle = startAngle;
        orbitRadius = radius;
        currentRadius = 0.5f; // ขยายรัศมีจากตัวบอสออกมาอย่างนุ่มนวล
        orbitSpeed = speed;
        isOrbiting = true;
        isLaunched = false;

        gameObject.tag = "Enemy_Attack";

        // รีเซ็ตการทำลายตัวเองตาม duration
        CancelInvoke(nameof(OnOrbitTimeUp));
        if (duration > 0f)
        {
            Invoke(nameof(OnOrbitTimeUp), duration);
        }
    }

    private void Update()
    {
        if (isLaunched)
        {
            transform.position += launchDirection * shootSpeed * Time.deltaTime;
            transform.Rotate(0, 0, orbitSpeed * 2f * Time.deltaTime);
            return;
        }

        if (!isOrbiting || centerTarget == null)
        {
            if (centerTarget == null)
            {
                // ถ้าบอสตายหรือหายไป ให้ทำลายกระดานทิ้งทันที
                Destroy(gameObject);
            }
            return;
        }

        // ค่อยๆ ขยายรัศมีจาก 0.5 สู่ orbitRadius
        currentRadius = Mathf.MoveTowards(currentRadius, orbitRadius, 4f * Time.deltaTime);

        // หมุนมุมรอบตัวบอส
        currentAngle += orbitSpeed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * currentRadius;
        transform.position = centerTarget.position + offset;

        // หันหน้ากระดานตามทิศทางวงโคจร
        transform.rotation = Quaternion.Euler(0, 0, currentAngle - 90f);
    }

    private void OnOrbitTimeUp()
    {
        if (shootAtPlayerOnExpire)
        {
            // พุ่งใส่ตำแหน่งผู้เล่น
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                launchDirection = (playerObj.transform.position - transform.position).normalized;
            }
            else
            {
                launchDirection = (transform.position - (centerTarget != null ? centerTarget.position : transform.position)).normalized;
            }

            isOrbiting = false;
            isLaunched = true;
            Destroy(gameObject, 2.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ทำดาเมจใส่ผู้เล่น
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                if (pushDir == Vector2.zero) pushDir = Vector2.up;
                playerHealth.TakeDamage(damage, pushDir);
            }
        }
        // บล็อกกระสุนของผู้เล่น
        else if (canBlockBullets)
        {
            if (collision.GetComponent<BulletCS>() != null || collision.CompareTag("Player_Attack"))
            {
                if (collision.GetComponent<BulletCS>() != null)
                {
                    Destroy(collision.gameObject);
                }
            }
        }
    }
}
