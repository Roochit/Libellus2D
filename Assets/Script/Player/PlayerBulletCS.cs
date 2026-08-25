using UnityEngine;

public class BulletCS : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f; // ทำลายตัวเองหลังผ่านไป 2 วินาที

    private Vector3 moveDirection;

    public int Damage { get; private set; } = 1;

    public void Setup(Vector3 direction, int damageValue)
    {
        Damage = damageValue;
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
        // ตรวจจับเมื่อชนศัตรู (เช็คสคริปต์ EnemyCS โดยตรงเพื่อเลี่ยงการใช้ Tag "Enemy" ที่ไม่ได้สร้างในระบบ)
        if (collision.GetComponent<EnemyCS>() != null)
        {
            Destroy(gameObject);
        }
        else if (!collision.CompareTag("Player") && !collision.isTrigger)
        {
            // ทำลายกระสุนเมื่อชนกำแพง/สิ่งกีดขวาง (ไม่รวมผู้เล่นหรือ Trigger อื่นๆ)
            Destroy(gameObject);
        }
    }
}
