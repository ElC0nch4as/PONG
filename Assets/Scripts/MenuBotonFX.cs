using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuBotonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform border;
    [SerializeField] private Image borderImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color hoverColor = new Color(0f, 1f, 1f, 1f);

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.8f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Hover FX")]
    [SerializeField] private TronBorderGlowUI glowScript;


    private bool isHover;
    private Vector3 baseScale;

    private void Awake()
    {
        if (border != null) baseScale = border.localScale;
        if (borderImage != null) borderImage.color = normalColor;

        if (glowScript != null) glowScript.enabled = false;
    }

    private void Update()
    {
        if (border == null) return;

        Vector3 target = baseScale * (isHover ? hoverScale : 1f);
        border.localScale = Vector3.Lerp(border.localScale, target, Time.unscaledDeltaTime * scaleSpeed);
    }
    public void OnPointerEnter(PointerEventData eventData)

    {
        isHover = true;
        if (borderImage != null) borderImage.color = hoverColor;

        if (glowScript != null) glowScript.SetVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
        if (borderImage != null) borderImage.color = normalColor;

        if (glowScript != null) glowScript.SetVisible(false);
    }
}