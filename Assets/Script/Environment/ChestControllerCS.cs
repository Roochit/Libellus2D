using UnityEngine;
using System.Collections;

public class ChestControllerCS : MonoBehaviour
{
    [Header("Chest Visuals")]
    [SerializeField] private Sprite openChestSprite; // รูปภาพกล่องตอนเปิดออกแล้ว

    [Header("Drop Settings")]
    [SerializeField] private GameObject itemToDropPrefab; // วัตถุที่จะดรอป (เช่น Prefab หัวใจ)
    [SerializeField] private int dropCount = 2; // จำนวนของที่จะดรอป
    [SerializeField] private float dropForce = 5f; // แรงผลักกระสุน/ไอเทมกระเด็นออกจากกล่อง

    private bool isOpened = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D chestCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        chestCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เปิดกล่องเมื่อถูกดาบฟัน (Tag "Player_Attack") หรือถูกลูกปืนชน (มีสคริปต์ BulletCS)
        if (!isOpened && (collision.CompareTag("Player_Attack") || collision.GetComponent<BulletCS>() != null))
        {
            OpenChest();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // เปิดกล่องเมื่อสัมผัสสิ่งกีดขวางดาบฟันที่เป็น Solid (Tag "Player_Attack")
        if (!isOpened && collision.gameObject.CompareTag("Player_Attack"))
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        isOpened = true;
        Debug.Log("Chest opened!");

        // เปลี่ยนภาพเป็นกล่องเปิดออก
        if (spriteRenderer != null && openChestSprite != null)
        {
            spriteRenderer.sprite = openChestSprite;
        }

        // ปิดการทำงานของคอลไลเดอร์ เพื่อให้ผู้เล่นเดินผ่านตัวกล่องได้หลังจากเปิดแล้ว
        if (chestCollider != null)
        {
            chestCollider.enabled = false;
        }

        // ดรอปไอเทมออกมาแบบทีละชิ้นพร้อมดีเลย์และเยื้องตำแหน่งเพื่อกระจายตัว
        StartCoroutine(SpawnDropsRoutine());
    }

    private IEnumerator SpawnDropsRoutine()
    {
        if (itemToDropPrefab != null)
        {
            for (int i = 0; i < dropCount; i++)
            {
                // เพิ่มจุดเยื้องเล็กน้อยเพื่อป้องกันไม่ให้สปอนจุดเดียวกันเป๊ะ
                Vector3 spawnOffset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.25f), 0f);
                GameObject spawnedItem = Instantiate(itemToDropPrefab, transform.position + spawnOffset, Quaternion.identity);

                // หากไอเทมมี Rigidbody2D ให้ส่งแรงผลักกระจายออกไปแบบสุ่มกระเด็นรอบๆ กล่อง
                if (spawnedItem.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 randomDir = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.8f, 1.5f)).normalized;
                    rb.AddForce(randomDir * dropForce, ForceMode2D.Impulse);
                }

                // รอ 0.15 วินาที ก่อนที่จะดรอปชิ้นถัดไป
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
