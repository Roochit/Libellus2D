using UnityEngine;

/// <summary>
/// ระบบควบคุมการเล่น Animation และการหันหน้า (FlipX) ของตัวละคร Player
/// รองรับทั้งระบบ Animator Parameters (มาตรฐาน Unity) และระบบ Direct Play (animator.Play)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimationCS : MonoBehaviour
{
    public enum ControlMode
    {
        AnimatorParameters, // ใช้ Parameters & Transitions ใน Animator (state, isMoving, moveX, moveY)
        DirectPlay          // สั่งเล่น State โดยตรงตามชื่อคลิป (animator.Play) ไม่ต้องเซ็ต Transitions ให้ยุ่งยาก
    }

    [Header("โหมดการควบคุม Animation")]
    [Tooltip("เลือกวิธีที่ต้องการควบคุม Animator")]
    [SerializeField] private ControlMode controlMode = ControlMode.AnimatorParameters;

    [Header("ชื่อ State / คลิปอนิเมชันใน Animator")]
    [SerializeField] private string idleStateName = "Player_idel";
    [SerializeField] private string walkDownStateName = "Player_Walk_down";
    [SerializeField] private string walkUpStateName = "Player_Walk_Up";
    [SerializeField] private string walkSideStateName = "Player_side";

    [Header("การหันหน้าตัวละคร (Sprite Flipping)")]
    [Tooltip("กลับด้าน SpriteRenderer แนวนอนเมื่อเดินไปทางซ้าย")]
    [SerializeField] private bool flipOnMoveLeft = true;

    [Header("การโจมตี (ทางเลือกเสริม)")]
    [Tooltip("ชื่อ Trigger สำหรับการโจมตีใน Animator (ถ้ามี)")]
    [SerializeField] private string attackTriggerName = "Attack";

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
    private int attackTriggerHash;

    // ตรวจสอบ Parameter ที่มีอยู่ใน Animator จริง
    private bool hasParamState = false;
    private bool hasParamIsMoving = false;
    private bool hasParamMoveX = false;
    private bool hasParamMoveY = false;
    private bool hasParamAttack = false;

    // Tracking ปัจจุบัน
    private string currentPlayingState = "";
    private int currentStateInt = 0; // 0=Idle, 1=WalkDown, 2=WalkUp, 3=WalkSide

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            // ลองค้นหาในลูกของ GameObject เผื่อแยก Model ไว้ข้างใน
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
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        CheckAnimatorParameters();
    }

    private void OnEnable()
    {
        if (playerCombat != null)
        {
            playerCombat.OnMeleeAttackTriggered += HandleMeleeAttack;
            playerCombat.OnRangedAttackTriggered += HandleRangedAttack;
        }
    }

    private void OnDisable()
    {
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
            if (param.name == attackTriggerName) hasParamAttack = true;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        Vector3 moveInput = GetMoveInput();
        UpdateAnimationAndDirection(moveInput);
    }

    /// <summary>
    /// อ่านค่าเวกเตอร์การเคลื่อนที่ของผู้เล่น (อ่านจาก PlayerMovementCS หรือ Input โดยตรง)
    /// </summary>
    private Vector3 GetMoveInput()
    {
        if (playerMovement != null)
        {
            return playerMovement.MoveInput;
        }

        // Fallback หากไม่มี PlayerMovementCS
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        return input.normalized;
    }

    /// <summary>
    /// ประมวลผลทิศทาง การกลับด้านตัวละคร และเลือกคลิป Animation ที่เหมาะสม
    /// </summary>
    private void UpdateAnimationAndDirection(Vector3 moveInput)
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        string targetStateName;
        int targetStateInt;

        if (!isMoving)
        {
            // หยุดนิ่ง (Idle)
            targetStateName = idleStateName;
            targetStateInt = 0;
        }
        else
        {
            // กำลังเคลื่อนที่: ตรวจดูว่าเคลื่อนที่แนวนอนหรือแนวตั้งเด่นกว่า
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                // เคลื่อนที่แนวนอน (ซ้าย / ขวา)
                targetStateName = walkSideStateName;
                targetStateInt = 3;

                // หันหน้าซ้าย-ขวา
                if (flipOnMoveLeft && spriteRenderer != null)
                {
                    if (moveInput.x < -0.01f)
                    {
                        spriteRenderer.flipX = true; // หันซ้าย
                    }
                    else if (moveInput.x > 0.01f)
                    {
                        spriteRenderer.flipX = false; // หันขวา
                    }
                }
            }
            else
            {
                // เคลื่อนที่แนวตั้ง (บน / ล่าง)
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

        // ส่งข้อมูลไปยัง Animator ตามโหมดที่เลือก
        if (controlMode == ControlMode.DirectPlay)
        {
            PlayDirect(targetStateName);
        }
        else
        {
            UpdateParameters(targetStateInt, isMoving, moveInput);
        }
    }

    /// <summary>
    /// สั่งเล่น State โดยตรงผ่าน animator.Play() เมื่อ State เปลี่ยนไป
    /// </summary>
    private void PlayDirect(string stateName)
    {
        if (currentPlayingState == stateName) return;

        currentPlayingState = stateName;
        animator.Play(stateName);
    }

    /// <summary>
    /// อัปเดตค่า Animator Parameters
    /// </summary>
    private void UpdateParameters(int stateInt, bool isMoving, Vector3 moveInput)
    {
        currentStateInt = stateInt;

        if (hasParamState)
        {
            animator.SetInteger(stateHash, stateInt);
        }

        if (hasParamIsMoving)
        {
            animator.SetBool(isMovingHash, isMoving);
        }

        if (hasParamMoveX)
        {
            animator.SetFloat(moveXHash, moveInput.x);
        }

        if (hasParamMoveY)
        {
            animator.SetFloat(moveYHash, moveInput.y);
        }
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นทำการฟัน (Melee Attack)
    /// </summary>
    private void HandleMeleeAttack()
    {
        if (hasParamAttack)
        {
            animator.SetTrigger(attackTriggerHash);
        }
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นทำการยิง (Ranged Attack)
    /// </summary>
    private void HandleRangedAttack()
    {
        if (hasParamAttack)
        {
            animator.SetTrigger(attackTriggerHash);
        }
    }
}
