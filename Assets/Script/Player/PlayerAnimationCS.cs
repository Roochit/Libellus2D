using System.Collections;
using UnityEngine;

/// <summary>
/// ระบบควบคุมการเล่น Animation, การกลับด้านตัวละคร (FlipX)
/// และรองรับท่าเดิน, ท่ายืนนิ่ง, ท่าหลบ/พุ่ง (Dash), และท่าฟันโจมตี 3 ทิศทาง (Melee Attack)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimationCS : MonoBehaviour
{
    public enum ControlMode
    {
        AnimatorParameters, // ใช้ Parameters & Transitions ใน Animator (state, isMoving, moveX, moveY, triggers)
        DirectPlay          // สั่งเล่น State โดยตรงตามชื่อคลิป (animator.Play) ไม่ต้องเซ็ต Transitions ให้ยุ่งยาก
    }

    [Header("โหมดการควบคุม Animation")]
    [Tooltip("เลือกวิธีที่ต้องการควบคุม Animator")]
    [SerializeField] private ControlMode controlMode = ControlMode.AnimatorParameters;

    [Header("ชื่อ State ท่ายืนและท่าเดิน")]
    [SerializeField] private string idleStateName = "Player_idel";
    [SerializeField] private string walkDownStateName = "Player_Walk_down";
    [SerializeField] private string walkUpStateName = "Player_Walk_Up";
    [SerializeField] private string walkSideStateName = "Player_side";

    [Header("ชื่อ State ท่าหลบ (Dash)")]
    [SerializeField] private string dashStateName = "Player_Dash";

    [Header("ชื่อ State ท่าฟันโจมตี (Melee Attack)")]
    [SerializeField] private string attackUpStateName = "PlayerAttack_Melee_Up";
    [SerializeField] private string attackDownStateName = "PlayerAttack_Melee_Down";
    [SerializeField] private string attackSideStateName = "PlayerAttack_Melee_Side";

    [Header("การหันหน้าตัวละคร (Sprite Flipping)")]
    [Tooltip("กลับด้าน SpriteRenderer แนวนอนเมื่อเดินหรือฟันไปทางซ้าย")]
    [SerializeField] private bool flipOnMoveLeft = true;

    // References
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerMovementCS playerMovement;
    private PlayerCombatCS playerCombat;
    private Rigidbody2D rb;

    // Hashes เพื่อประสิทธิภาพ
    private int stateHash;
    private int isMovingHash;
    private int moveXHash;
    private int moveYHash;
    private int isDashingHash;
    private int dashTriggerHash;
    private int attackTriggerHash;
    private int attackUpHash;
    private int attackDownHash;
    private int attackSideHash;

    // ตรวจสอบ Parameter ที่มีอยู่ใน Animator จริง
    private bool hasParamState = false;
    private bool hasParamIsMoving = false;
    private bool hasParamMoveX = false;
    private bool hasParamMoveY = false;
    private bool hasParamIsDashing = false;
    private bool hasParamDashTrigger = false;
    private bool hasParamAttackTrigger = false;
    private bool hasParamAttackUp = false;
    private bool hasParamAttackDown = false;
    private bool hasParamAttackSide = false;

    // Tracking ปัจจุบัน
    private string currentPlayingState = "";
    private int currentStateInt = 0; // 0=Idle, 1=WalkDown, 2=WalkUp, 3=WalkSide, 4=Dash, 5=AttackDown, 6=AttackUp, 7=AttackSide
    private bool isAttacking = false;
    private Coroutine attackRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        playerMovement = GetComponent<PlayerMovementCS>();
        playerCombat = GetComponent<PlayerCombatCS>();
        rb = GetComponent<Rigidbody2D>();

        // แฮชชื่อ Parameter
        stateHash = Animator.StringToHash("state");
        isMovingHash = Animator.StringToHash("isMoving");
        moveXHash = Animator.StringToHash("moveX");
        moveYHash = Animator.StringToHash("moveY");
        isDashingHash = Animator.StringToHash("isDashing");
        dashTriggerHash = Animator.StringToHash("Dash");
        attackTriggerHash = Animator.StringToHash("Attack");
        attackUpHash = Animator.StringToHash("AttackUp");
        attackDownHash = Animator.StringToHash("AttackDown");
        attackSideHash = Animator.StringToHash("AttackSide");

        CheckAnimatorParameters();
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnDashTriggered += HandleDash;
        }

        if (playerCombat != null)
        {
            playerCombat.OnMeleeAttackTriggered += HandleMeleeAttack;
            playerCombat.OnRangedAttackTriggered += HandleRangedAttack;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnDashTriggered -= HandleDash;
        }

        if (playerCombat != null)
        {
            playerCombat.OnMeleeAttackTriggered -= HandleMeleeAttack;
            playerCombat.OnRangedAttackTriggered -= HandleRangedAttack;
        }
    }

    private void CheckAnimatorParameters()
    {
        if (animator == null) return;

        foreach (var param in animator.parameters)
        {
            if (param.name == "state") hasParamState = true;
            if (param.name == "isMoving") hasParamIsMoving = true;
            if (param.name == "moveX") hasParamMoveX = true;
            if (param.name == "moveY") hasParamMoveY = true;
            if (param.name == "isDashing") hasParamIsDashing = true;
            if (param.name == "Dash") hasParamDashTrigger = true;
            if (param.name == "Attack") hasParamAttackTrigger = true;
            if (param.name == "AttackUp") hasParamAttackUp = true;
            if (param.name == "AttackDown") hasParamAttackDown = true;
            if (param.name == "AttackSide") hasParamAttackSide = true;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // หากกำลังติดแอ็กชันโจมตี จะล็อกแอนิเมชันไว้จนกว่าจะฟันเสร็จ
        if (isAttacking)
        {
            return;
        }

        // ตรวจสอบสถานะการพุ่ง (Dash)
        if (playerMovement != null && playerMovement.IsDashing)
        {
            UpdateDashAnimation();
            return;
        }

        Vector3 moveInput = GetMoveInput();
        UpdateLocomotionAnimation(moveInput);
    }

    /// <summary>
    /// อ่านค่าการเดิน
    /// </summary>
    private Vector3 GetMoveInput()
    {
        if (playerMovement != null)
        {
            return playerMovement.MoveInput;
        }

        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        return input.normalized;
    }

    /// <summary>
    /// ควบคุมแอนิเมชันการยืนนิ่งและการเดิน
    /// </summary>
    private void UpdateLocomotionAnimation(Vector3 moveInput)
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        string targetStateName;
        int targetStateInt;

        if (!isMoving)
        {
            targetStateName = idleStateName;
            targetStateInt = 0;
        }
        else
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                targetStateName = walkSideStateName;
                targetStateInt = 3;

                ApplyHorizontalFlip(moveInput.x);
            }
            else
            {
                if (moveInput.y > 0.01f)
                {
                    targetStateName = walkUpStateName;
                    targetStateInt = 2;
                }
                else
                {
                    targetStateName = walkDownStateName;
                    targetStateInt = 1;
                }
            }
        }

        ApplyAnimation(targetStateName, targetStateInt, isMoving, moveInput, false);
    }

    /// <summary>
    /// ควบคุมแอนิเมชันท่าพุ่ง (Dash)
    /// </summary>
    private void UpdateDashAnimation()
    {
        Vector3 dashDir = playerMovement != null ? playerMovement.LastMoveDirection : Vector3.down;
        ApplyHorizontalFlip(dashDir.x);

        ApplyAnimation(dashStateName, 4, true, dashDir, true);
    }

    /// <summary>
    /// ควบคุมการหันซ้าย-ขวา
    /// </summary>
    private void ApplyHorizontalFlip(float xDir)
    {
        if (!flipOnMoveLeft || spriteRenderer == null) return;

        if (xDir < -0.01f)
        {
            spriteRenderer.flipX = true; // หันซ้าย
        }
        else if (xDir > 0.01f)
        {
            spriteRenderer.flipX = false; // หันขวา
        }
    }

    /// <summary>
    /// ส่งคำสั่งไปยัง Animator ตามโหมดที่เลือก
    /// </summary>
    private void ApplyAnimation(string stateName, int stateInt, bool isMoving, Vector3 dir, bool isDashing)
    {
        currentStateInt = stateInt;

        if (controlMode == ControlMode.DirectPlay)
        {
            if (currentPlayingState != stateName)
            {
                currentPlayingState = stateName;
                animator.Play(stateName);
            }
        }
        else
        {
            if (hasParamState) animator.SetInteger(stateHash, stateInt);
            if (hasParamIsMoving) animator.SetBool(isMovingHash, isMoving);
            if (hasParamMoveX) animator.SetFloat(moveXHash, dir.x);
            if (hasParamMoveY) animator.SetFloat(moveYHash, dir.y);
            if (hasParamIsDashing) animator.SetBool(isDashingHash, isDashing);
        }
    }

    /// <summary>
    /// ทำงานเมื่อเริ่มพุ่ง (Dash Trigger)
    /// </summary>
    private void HandleDash()
    {
        if (hasParamDashTrigger)
        {
            animator.SetTrigger(dashTriggerHash);
        }

        UpdateDashAnimation();
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นทำการฟัน (Melee Attack)
    /// </summary>
    private void HandleMeleeAttack()
    {
        Vector3 facingDir = playerCombat != null ? playerCombat.LastFacingDirection : Vector3.down;
        float attackDuration = playerCombat != null ? Mathf.Max(0.15f, playerCombat.MeleeDuration) : 0.2f;

        string attackStateName;
        int attackStateInt;

        // คำนวณทิศทางของการฟัน
        if (Mathf.Abs(facingDir.x) > Mathf.Abs(facingDir.y))
        {
            attackStateName = attackSideStateName;
            attackStateInt = 7;
            ApplyHorizontalFlip(facingDir.x);

            if (hasParamAttackSide) animator.SetTrigger(attackSideHash);
        }
        else
        {
            if (facingDir.y > 0.01f)
            {
                attackStateName = attackUpStateName;
                attackStateInt = 6;
                if (hasParamAttackUp) animator.SetTrigger(attackUpHash);
            }
            else
            {
                attackStateName = attackDownStateName;
                attackStateInt = 5;
                if (hasParamAttackDown) animator.SetTrigger(attackDownHash);
            }
        }

        if (hasParamAttackTrigger)
        {
            animator.SetTrigger(attackTriggerHash);
        }

        // เริ่มเล่นท่าโจมตีและล็อกไว้ตามระยะเวลาโจมตี
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(PerformAttackAnimationRoutine(attackStateName, attackStateInt, attackDuration));
    }

    private IEnumerator PerformAttackAnimationRoutine(string attackStateName, int attackStateInt, float duration)
    {
        isAttacking = true;
        currentPlayingState = attackStateName;
        currentStateInt = attackStateInt;

        if (controlMode == ControlMode.DirectPlay)
        {
            animator.Play(attackStateName, 0, 0f);
        }
        else if (hasParamState)
        {
            animator.SetInteger(stateHash, attackStateInt);
        }

        yield return new WaitForSeconds(duration);

        isAttacking = false;
        attackRoutine = null;

        // เมื่อฟันเสร็จ ให้กลับไปสถานะการเดิน/ยืนตามปกติทันที
        Vector3 currentInput = GetMoveInput();
        UpdateLocomotionAnimation(currentInput);
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นยิงกระสุน (Ranged Attack)
    /// </summary>
    private void HandleRangedAttack()
    {
        if (hasParamAttackTrigger)
        {
            animator.SetTrigger(attackTriggerHash);
        }
    }
}
