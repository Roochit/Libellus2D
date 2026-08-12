using UnityEngine;

public class BulletCS : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f; // ทำลายตัวเองหลังผ่านไป 2 วินาที

    private Vector3 moveDirection;

    public void Setup(Vector3 direction)
    {
        moveDirection = direction.normalized;
        
        // หมุนตัวกระสุนให้หันตามทิศทางที่ยิง
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ตรวจจับเมื่อชนศัตรู/กำแพง (สามารถใส่ Tag "Enemy" ในอนาคตได้)
        if (collision.CompareTag("Enemy"))
        {
            // ทำดาเมจศัตรูที่นี่
            Destroy(gameObject);
        }
    }
}
