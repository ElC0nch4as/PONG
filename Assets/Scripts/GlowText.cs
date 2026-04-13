using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class GlowText : MonoBehaviour
{
    [SerializeField] private TMP_Text source;

    private TMP_Text target;

    private void Awake()
    {
        target = GetComponent<TMP_Text>();
    }

    private void LateUpdate()
    {
        if (source == null)
        {
            return;
        }

        if (target.text != source.text)
        {
            target.text = source.text;
        }
    }
}