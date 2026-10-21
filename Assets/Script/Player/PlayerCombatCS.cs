using UnityEngine;

public class PlayerCombatCS : MonoBehaviour
{
    public enum AimMode
    {
        MousePosition,      // เล็งตามตำแหน่งเมาส์ (ฟัน/ยิงไปที่เคอร์เซอร์เมาส์)
        NearestEnemy,       // ล็อกเป้าหาศัตรูที่ใกล้ที่สุดอัตโนมัติ
        MovementDirection   // หันตามปุ่ม WASD
    }

    [Header("Aiming Settings (การเล็ง)")]
    [Tooltip("เลือกรูปแบบการเล็งทิศทางการโจมตี")]
    [SerializeField] private AimMode aimMode = AimMode.MousePosition;

    [Header("Attack Offsets & Points")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackOffsetDistance = 1.2f;

    [Header("Melee Settings (ฟัน)")]
    [SerializeField] private GameObject meleeHitboxPrefab;
    [Tooltip("ตัวคูณขนาด Hitbox ของการฟัน (ค่า 1 คือขนาดปกติใน Prefab, 1.5 คือใหญ่ขึ้น 50%)")]
    [SerializeField] private Vector2 meleeHitboxScaleMultiplier = Vector2.one;
    [SerializeField] private float meleeDuration = 0.15f;
    [SerializeField] private float meleeCooldown = 0.35f;
    [SerializeField] private int meleeDamage = 1; // ความเสียหายการฟันระยะประชิด
    private float nextMeleeTime = 0f;

    [Header("Ranged Settings (ยิง)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float rangedCooldown = 0.25f;
    [SerializeField] private int rangedDamage = 1; // ความเสียหายการยิงไกล (กระสุน)
    private float nextRangedTime = 0f;

    private Vector3 lastFacingDirection = Vector3.right; // Default หันขวา

    public System.Action OnMeleeAttackTriggered;
    public System.Action OnRangedAttackTriggered;
    public Vector3 LastFacingDirection => lastFacingDirection;
    public float MeleeDuration => meleeDuration;

    private void Awake()
    {
        if (attackPoint == null) attackPoint = transform;
    }

    private void Update()
    {
        UpdateFacingDirection();
        HandleCombatInput();
    }

    private void UpdateFacingDirection()
    {
        // หากตั้งเป็น MovementDirection หรือกดปุ่ม WASD ให้อัปเดตทิศทาง
        Vector3 inputDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) inputDir.y += 1f;
        if (Input.GetKey(KeyCode.S)) inputDir.y -= 1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x += 1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x -= 1f;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = inputDir.normalized;
        }
    }

    private void HandleCombatInput()
    {
        // คลิกซ้าย หรือ ปุ่ม J -> ฟัน
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) && Time.time >= nextMeleeTime)
        {
            UpdateAimDirection();
            PerformMeleeAttack();
            nextMeleeTime = Time.time + meleeCooldown;
        }

        // คลิกขวา หรือ ปุ่ม K -> ยิง
        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.K)) && Time.time >= nextRangedTime)
        {
            UpdateAimDirection();
            PerformRangedAttack();
            nextRangedTime = Time.time + rangedCooldown;
        }
    }

    private void UpdateAimDirection()
    {
        switch (aimMode)
        {
            case AimMode.MousePosition:
                AimTowardsMouse();
                break;
            case AimMode.NearestEnemy:
                FaceNearestEnemy();
                break;
            case AimMode.MovementDirection:
                // ใช้ทิศทางจาก WASD ตามปกติ
                break;
        }
    }

    private void AimTowardsMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector3 playerPos = transform.position;
        playerPos.z = 0f;

        Vector3 direction = (mouseWorldPos - playerPos).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = direction;
        }
    }

    private void FaceNearestEnemy()
    {
        // ค้นหาศัตรูทั้งหมดในฉากที่มีคลาส EnemyCS
        EnemyCS[] enemies = FindObjectsOfType<EnemyCS>();
        if (enemies == null || enemies.Length == 0) return;

        EnemyCS closestEnemy = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (EnemyCS enemy in enemies)
        {
            if (enemy == null) continue;
            float distance = Vector3.Distance(playerPos, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // ถ้าพบศัตรูในฉาก ให้หันไปหาศัตรูคนนั้น
        if (closestEnemy != null)
        {
            Vector3 direction = (closestEnemy.transform.position - playerPos).normalized;
            direction.z = 0f; // ล็อกแกน Z สำหรับเกม 2D

            if (direction.sqrMagnitude > 0.01f)
            {
                lastFacingDirection = direction.normalized;
            }
        }
    }

    private void PerformMeleeAttack()
    {
        if (meleeHitboxPrefab == null) return;

        Vector3 spawnPosition = attackPoint.position + (lastFacingDirection * attackOffsetDistance);
        float angle = Mathf.Atan2(lastFacingDirection.y, lastFacingDirection.x) * Mathf.Rad2Deg;
        Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);

        GameObject meleeInstance = Instantiate(meleeHitboxPrefab, spawnPosition, spawnRotation, transform);
        
        // ปรับขนาด Hitbox ตามตัวคูณที่ตั้งไว้
        Vector3 currentScale = meleeInstance.transform.localScale;
        meleeInstance.transform.localScale = new Vector3(
            currentScale.x * meleeHitboxScaleMultiplier.x,
            currentScale.y * meleeHitboxScaleMultiplier.y,
            currentScale.z
        );

        // ตั้งค่าพลังโจมตีประชิดให้กับ Hitbox
        if (meleeInstance.TryGetComponent<PlayerMeleeAttackCS>(out var meleeAttack))
        {
            meleeAttack.Setup(meleeDamage);
        }
        else
        {
            var dynamicAttack = meleeInstance.AddComponent<PlayerMeleeAttackCS>();
            dynamicAttack.Setup(meleeDamage);
        }

        Destroy(meleeInstance, meleeDuration);
        OnMeleeAttackTriggered?.Invoke();
    }

    private void PerformRangedAttack()
    {
        if (bulletPrefab == null) return;
 
        Vector3 spawnPosition = attackPoint.position + (lastFacingDirection * attackOffsetDistance);
        GameObject bulletInstance = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        if (bulletInstance.TryGetComponent<BulletCS>(out var bullet))
        {
            bullet.Setup(lastFacingDirection, rangedDamage);
        }

        OnRangedAttackTriggered?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Transform point = attackPoint != null ? attackPoint : transform;
        Vector3 dir = lastFacingDirection.sqrMagnitude > 0.01f ? lastFacingDirection : Vector3.right;
        Vector3 spawnPos = point.position + (dir * attackOffsetDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPos, 0.4f * Mathf.Max(meleeHitboxScaleMultiplier.x, meleeHitboxScaleMultiplier.y));
        Gizmos.DrawLine(point.position, spawnPos);
    }
}