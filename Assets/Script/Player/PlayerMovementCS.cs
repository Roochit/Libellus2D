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

    private void Update()
    {
        if (isDashing) {
            return;
        }

        HandleDirectMovement();
        HandleDashInput();
    }

    private void HandleDirectMovement()
    {
        Vector3 moveInput = Vector3.zero;


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


        if (moveInput.sqrMagnitude > 0.01f)
        {
            moveInput.Normalize();
        }

        transform.position += moveInput * moveSpeed * Time.deltaTime;
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

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (lastMoveDirection * dashDistance);

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

    // Public Property ให้สคริปต์อื่นมาอ่านสถานะได้ (เช่น เอาไปเปิดอมตะ iFrame ในอนาคต)
    public bool IsDashing => isDashing;
}