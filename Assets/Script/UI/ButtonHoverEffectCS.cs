using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ควบคุมเอฟเฟกต์การเปลี่ยนสีปุ่มเมื่อเมาส์ชี้ (Button Hover Effect)
/// - เปลี่ยนสีของปุ่ม (Image) และ/หรือสีตัวอักษร (TMP_Text / Text) เป็นสีขาวเมื่อเมาส์ชี้อยู่
/// - คืนค่าสีเดิมอัตโนมัติเมื่อเลื่อนเมาส์ออกจากปุ่ม
/// - รองรับทั้งการทำงานผ่านเมาส์ (Pointer Enter/Exit) และคีย์บอร์ด/จอยสติ๊ก (Select/Deselect)
/// </summary>
public class ButtonHoverEffectCS : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Hover Color Settings")]
    [Tooltip("สีเมื่อเมาส์ชี้อยู่บนปุ่ม (ค่าเริ่มต้น: สีขาวบริสุทธิ์)")]
    [SerializeField] private Color hoverColor = Color.white;

    [Tooltip("เปลี่ยนสีของรูปภาพปุ่ม (Graphic / Image) เป็นสีขาวเมื่อเมาส์ชี้")]
    [SerializeField] private bool changeImageColor = true;

    [Tooltip("เปลี่ยนสีของข้อความตัวอักษร (TMP_Text / Text) เป็นสีขาวเมื่อเมาส์ชี้")]
    [SerializeField] private bool changeTextColor = true;

    [Header("Optional Animation Settings")]
    [Tooltip("ขยายขนาดปุ่มเล็กน้อยเมื่อเมาส์ชี้อยู่")]
    [SerializeField] private bool scaleOnHover = false;

    [Tooltip("อัตราส่วนการขยายขนาดเมื่อชี้เมาส์ (เช่น 1.05 = 105%)")]
    [SerializeField] private float hoverScaleMultiplier = 1.05f;

    private Graphic targetGraphic;
    private TMP_Text targetTMP;
    private Text targetLegacyText;
    private Button button;

    private Color originalGraphicColor = Color.white;
    private Color originalTextColor = Color.white;
    private Vector3 originalScale = Vector3.one;
    private bool isHovered = false;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (isInitialized) return;
        isInitialized = true;

        button = GetComponent<Button>();
        targetGraphic = GetComponent<Graphic>();
        targetTMP = GetComponentInChildren<TMP_Text>();
        targetLegacyText = GetComponentInChildren<Text>();

        originalScale = transform.localScale;

        if (targetGraphic != null)
        {
            originalGraphicColor = targetGraphic.color;
        }

        if (targetTMP != null)
        {
            originalTextColor = targetTMP.color;
        }
        else if (targetLegacyText != null)
        {
            originalTextColor = targetLegacyText.color;
        }

        // ปรับแต่ง ColorBlock ของ Button ให้ Highlighted/Selected เป็นสีขาว
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.highlightedColor = hoverColor;
            cb.selectedColor = hoverColor;
            button.colors = cb;
        }
    }

    /// <summary>
    /// กำหนดสี Hover ใหม่ผ่านสคริปต์
    /// </summary>
    public void SetHoverColor(Color color)
    {
        hoverColor = color;
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.highlightedColor = hoverColor;
            cb.selectedColor = hoverColor;
            button.colors = cb;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyHoverState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyHoverState(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        ApplyHoverState(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ApplyHoverState(false);
    }

    private void ApplyHoverState(bool hovered)
    {
        if (!isInitialized) InitializeComponents();
        if (button != null && !button.interactable) return;

        isHovered = hovered;

        if (hovered)
        {
            // เปลี่ยนสีรูปภาพปุ่มเป็นสีขาว
            if (changeImageColor && targetGraphic != null)
            {
                targetGraphic.color = hoverColor;
            }

            // เปลี่ยนสีข้อความเป็นสีขาว
            if (changeTextColor)
            {
                if (targetTMP != null) targetTMP.color = hoverColor;
                else if (targetLegacyText != null) targetLegacyText.color = hoverColor;
            }

            // ขยายขนาดปุ่ม (ถ้าเปิดใช้)
            if (scaleOnHover)
            {
                transform.localScale = originalScale * hoverScaleMultiplier;
            }
        }
        else
        {
            // คืนค่าสีเดิมของรูปภาพปุ่ม
            if (changeImageColor && targetGraphic != null)
            {
                targetGraphic.color = originalGraphicColor;
            }

            // คืนค่าสีเดิมของข้อความ
            if (changeTextColor)
            {
                if (targetTMP != null) targetTMP.color = originalTextColor;
                else if (targetLegacyText != null) targetLegacyText.color = originalTextColor;
            }

            // คืนค่าขนาดเดิม
            if (scaleOnHover)
            {
                transform.localScale = originalScale;
            }
        }
    }

    private void OnDisable()
    {
        if (isHovered)
        {
            ApplyHoverState(false);
        }
    }
}

