using UnityEngine;
using System.Collections;

public class PlayerMovementCS : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 3f; 
    [SerializeField] private float dashDuration = 0.15f; 
    [SerializeField] private float dashCooldown = 0.5f;

    private Vector3 lastMoveDirection = Vector3.down; // ทิศทางล่าสุด (Default หันลงล่าง)
    private bool isDashing = false;
    private bool canDash = true;  

    private Rigidbody2D rb;
    private PlayerHealthCS playerHealth;
    private Vector3 moveInput = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealthCS>();
    }

    private void Update()
    {
        if (isDashing) {
            return;
        }

        HandleMovementInput();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        if (isDashing) {
            return;
        }

        // ป้องกันการทับซ้อนความเร็วถ้าผู้เล่นอยู่ในสถานะกระเด็น (Knockback)
        if (playerHealth != null && playerHealth.IsKnockedBack) {
            return;
        }

        MovePlayer();
    }

    private void HandleMovementInput()
    {
        moveInput = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveInput.y += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            moveInput.y -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            moveInput.x += 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveInput.x -= 1f;
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            moveInput.Normalize();
            lastMoveDirection = moveInput; 
        }
    }

    private void MovePlayer()
    {
        if (rb != null)
        {
            rb.velocity = moveInput * moveSpeed;
        }
        else
        {
            // fallback หากไม่มี Rigidbody2D ในตัวละคร
            transform.position += moveInput * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleDashInput()
    {
        // กด Spacebar เพื่อพุ่ง
        if (Input.GetKeyDown(KeyCode.Space) && canDash && !isDashing)
        {
            StartCoroutine(PerformDashRoutine());
        }
    }

    private IEnumerator PerformDashRoutine()
    {
        canDash = false;
        isDashing = true;

        // รีเซ็ตความเร็วของ Rigidbody ก่อนเริ่มพุ่ง
        if (rb != null) rb.velocity = Vector2.zero;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (lastMoveDirection * dashDistance);

        // จำกัดขอบเขตการแดชไม่ให้ออกนอกแมป (ตรวจจับสิ่งกีดขวางล่วงหน้า)
        Collider2D myCollider = GetComponent<Collider2D>();
        bool originalEnabled = myCollider != null ? myCollider.enabled : true;
        if (myCollider != null) myCollider.enabled = false;

        // ยิงเรย์เพื่อดูว่าข้างหน้ามีกำแพงหรือไม่
        RaycastHit2D hit = Physics2D.Raycast(startPosition, lastMoveDirection, dashDistance);
        if (myCollider != null) myCollider.enabled = originalEnabled;

        // ถ้าชนสิ่งกีดขวางแข็ง (ไม่ใช่ Trigger)
        if (hit.collider != null && !hit.collider.isTrigger)
        {
            // ถอยจุดหมายกลับมาเท่าระยะรัศมีตัวละครประมาณ 0.45f เพื่อไม่ให้ตัวผู้เล่นสไลด์จมกำแพง
            float hitDistance = hit.distance;
            float safeDistance = Mathf.Max(0f, hitDistance - 0.45f);
            targetPosition = startPosition + (lastMoveDirection * safeDistance);
        }

        float elapsedTime = 0f;

        // คำนวณการเคลื่อนที่แบบ Lerp ในช่วงเวลา dashDuration
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentage = elapsedTime / dashDuration;

            transform.position = Vector3.Lerp(startPosition, targetPosition, percentage);
            yield return null;
        }

        transform.position = targetPosition;
        isDashing = false;

        // รอคูลดาวน์
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Public Property ให้สคริปต์อื่นมาอ่านสถานะได้
    public bool IsDashing => isDashing;
}