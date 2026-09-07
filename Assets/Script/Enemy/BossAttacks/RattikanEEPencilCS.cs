using UnityEngine;

/// <summary>
/// กระสุนดินสอ EE พิฆาต 8 ทิศ ของบอสรัตติกาล (ท่าที่ 1)
/// พุ่งไปในทิศทางที่กำหนด หมุนปลายดินสอตามทิศทาง ทำดาเมจใส่ผู้เล่น และทำลายตัวเองแน่นอน
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RattikanEEPencilCS : MonoBehaviour
{
    [Header("Pencil Flight Settings")]
    [SerializeField] private float speed = 7.5f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private int damage = 1;

    [Header("Visual Settings")]
    [SerializeField] private float rotationOffset = 0f; // องศาปรับแก้สำหรับสไปรต์ดินสอ (ถ้าหัวดินสอหันขึ้นให้ตั้ง -90)

    private Vector3 moveDirection = Vector3.right;
    private bool isInitialized = false;

    private void Awake()
    {
        gameObject.tag = "Enemy_Attack";
        // รับประกันการทำลายตัวเองเสมอ
        Destroy(gameObject, lifeTime);
    }

    public void Setup(Vector3 direction, float customSpeed = -1f, int customDamage = -1)
    {
        moveDirection = direction.normalized;
        if (customSpeed > 0f) speed = customSpeed;
        if (customDamage > 0) damage = customDamage;

        gameObject.tag = "Enemy_Attack";

        // หมุนปลายดินสอให้หันไปตามทิศทางที่พุ่ง
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        isInitialized = true;
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เมื่อชนผู้เล่น
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerHealthCS>(out var playerHealth))
            {
                Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                if (pushDir == Vector2.zero) pushDir = moveDirection;
                playerHealth.TakeDamage(damage, pushDir);
            }
            Destroy(gameObject);
        }
        // ชนกำแพงหรือสิ่งกีดขวางทึบ (ไม่รวมบอส ศัตรู หรือ Trigger อื่นๆ)
        else if (collision.GetComponent<EnemyCS>() == null && !collision.CompareTag("Enemy_Attack") && !collision.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
