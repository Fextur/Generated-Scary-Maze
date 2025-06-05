using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class SignTextSetup : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] private string _message = "Find your way to the exit\nwhile avoiding the zombie!";
    [SerializeField] private float _fontSize = 0.3f;
    [SerializeField] private Color _textColor = new Color(0.2f, 0.1f, 0.05f);
    [SerializeField] private Font _textFont;
    [SerializeField] private TMP_FontAsset _tmpFont;

    [Header("Text Positioning")]
    [SerializeField] private Vector3 _textOffset = new Vector3(0, 0, -0.01f);
    [SerializeField] private Vector3 _textRotation = new Vector3(0, 0, 0);
    [SerializeField] private float _textWidth = 1f;
    [SerializeField] private float _textHeight = 0.5f;

    [Header("Distance Settings")]
    [SerializeField] private float _readableDistance = 4f;
    [SerializeField] private bool _fadeWithDistance = true;

    private GameObject _textObject;
    private TextMeshPro _tmpText;
    private TextMesh _legacyText;
    private Transform _playerTransform;

    void Start()
    {
        if (!Application.isPlaying) return;

        SetupText();
        FindPlayer();
    }

    void OnValidate()
    {
        if (!Application.isPlaying && _textObject != null)
        {
            UpdateTextProperties();
        }
    }

    public void SetupText()
    {
        if (_textObject != null)
        {
            if (Application.isPlaying)
                Destroy(_textObject);
            else
                DestroyImmediate(_textObject);
        }

        _textObject = new GameObject("SignText");
        _textObject.transform.parent = transform;
        _textObject.transform.localPosition = _textOffset;
        _textObject.transform.localRotation = Quaternion.Euler(_textRotation);

        _tmpText = _textObject.AddComponent<TextMeshPro>();
        if (_tmpText != null)
        {
            SetupTMPText();
        }
        else
        {
            Destroy(_tmpText);
            SetupLegacyText();
        }
    }

    private void SetupTMPText()
    {
        _tmpText.text = _message;
        _tmpText.fontSize = _fontSize;
        _tmpText.color = _textColor;
        _tmpText.alignment = TextAlignmentOptions.Center;
        _tmpText.fontStyle = FontStyles.Bold;

        if (_tmpFont != null)
        {
            _tmpText.font = _tmpFont;
        }

        RectTransform rect = _textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(_textWidth, _textHeight);
    }

    private void SetupLegacyText()
    {
        _legacyText = _textObject.AddComponent<TextMesh>();
        _legacyText.text = _message;
        _legacyText.fontSize = Mathf.RoundToInt(_fontSize * 50);
        _legacyText.characterSize = _fontSize * 0.1f;
        _legacyText.color = _textColor;
        _legacyText.anchor = TextAnchor.MiddleCenter;
        _legacyText.alignment = TextAlignment.Center;

        if (_textFont != null)
        {
            _legacyText.font = _textFont;
            _textObject.GetComponent<MeshRenderer>().material = _textFont.material;
        }

        MeshRenderer textRenderer = _textObject.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 1;
        }
    }

    private void UpdateTextProperties()
    {
        if (_tmpText != null)
        {
            _tmpText.text = _message;
            _tmpText.fontSize = _fontSize;
            _tmpText.color = _textColor;
        }
        else if (_legacyText != null)
        {
            _legacyText.text = _message;
            _legacyText.fontSize = Mathf.RoundToInt(_fontSize * 50);
            _legacyText.characterSize = _fontSize * 0.1f;
            _legacyText.color = _textColor;
        }

        if (_textObject != null)
        {
            _textObject.transform.localPosition = _textOffset;
            _textObject.transform.localRotation = Quaternion.Euler(_textRotation);
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (_playerTransform == null || _textObject == null) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);

        if (_fadeWithDistance)
        {
            if (distance <= _readableDistance)
            {
                float alpha = Mathf.Clamp01(1f - (distance / _readableDistance));

                if (_tmpText != null)
                {
                    Color color = _tmpText.color;
                    color.a = alpha;
                    _tmpText.color = color;
                }
                else if (_legacyText != null)
                {
                    Color color = _legacyText.color;
                    color.a = alpha;
                    _legacyText.color = color;
                }

                _textObject.SetActive(true);
            }
            else
            {
                _textObject.SetActive(false);
            }
        }
    }

    public void SetMessage(string message)
    {
        _message = message;
        UpdateTextProperties();
    }

    void OnDrawGizmosSelected()
    {
        if (_fadeWithDistance)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _readableDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.TransformPoint(_textOffset), new Vector3(_textWidth, _textHeight, 0.01f) * 0.1f);
    }

    [ContextMenu("Create/Update Text")]
    public void CreateTextInEditor()
    {
        SetupText();
    }
}