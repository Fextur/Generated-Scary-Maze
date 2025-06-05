using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PoopScreenEffect : MonoBehaviour
{
    [Header("Screen Effect Settings")]
    [SerializeField] private Color _effectColor = new Color(0.5f, 0.3f, 0.1f, 0.3f);
    [SerializeField] private AnimationCurve _effectCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Canvas _effectCanvas;
    [SerializeField] private Image _effectImage;

    void Start()
    {
        if (_effectImage != null)
        {
            _effectImage.color = Color.clear;
        }

        if (_effectCanvas == null)
        {
            _effectCanvas = GetComponentInChildren<Canvas>();
        }

        if (_effectImage == null && _effectCanvas != null)
        {
            _effectImage = _effectCanvas.GetComponentInChildren<Image>();
        }

    }

    public void TriggerEffect(float duration)
    {
        if (_effectImage != null)
        {
            StartCoroutine(PlayEffect(duration));
        }
    }

    private IEnumerator PlayEffect(float duration)
    {
        if (_effectImage == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float intensity = _effectCurve.Evaluate(normalizedTime);

            Color currentColor = _effectColor;
            currentColor.a = _effectColor.a * intensity;
            _effectImage.color = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _effectImage.color = Color.clear;
    }
}