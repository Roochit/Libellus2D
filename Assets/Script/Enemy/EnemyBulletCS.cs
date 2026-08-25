using UnityEngine;

public class EnemyBulletCS : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f; // ทำลายตัวเองหลังผ่านไป 3 วินาที

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
        // ตรวจจับเมื่อชนผู้เล่น
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
        // ทำลายกระสุนเมื่อชนกำแพง/สิ่งกีดขวาง (ไม่รวมศัตรูตัวอื่นหรือ Trigger อื่นๆ)
        else if (collision.GetComponent<EnemyCS>() == null && !collision.CompareTag("Enemy_Attack") && !collision.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
