using UnityEngine;
using TMPro;

public class InstructionSign : MonoBehaviour
{
    [Header("Sign Settings")]
    [SerializeField] private string _message = "Find your way to the exit while avoiding the zombie!";
    [SerializeField] private float _readableDistance = 3f;
    [SerializeField] private Color _signColor = new Color(0.6f, 0.4f, 0.2f);
    [SerializeField] private Color _textColor = Color.white;

    [Header("Sign Dimensions")]
    [SerializeField] private float _postHeight = 0.6f;
    [SerializeField] private float _postRadius = 0.03f;
    [SerializeField] private Vector3 _boardSize = new Vector3(0.8f, 0.4f, 0.05f);

    private GameObject _signPost;
    private GameObject _signBoard;
    private GameObject _textObject;
    private TextMeshPro _textMesh;
    private Transform _playerTransform;

    void Start()
    {
        CreateSign();
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    private void CreateSign()
    {
        _signPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _signPost.name = "SignPost";
        _signPost.transform.parent = transform;
        _signPost.transform.localPosition = new Vector3(0, _postHeight * 0.5f, 0);
        _signPost.transform.localScale = new Vector3(_postRadius * 2f, _postHeight * 0.5f, _postRadius * 2f);

        _signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _signBoard.name = "SignBoard";
        _signBoard.transform.parent = transform;
        _signBoard.transform.localPosition = new Vector3(0, _postHeight + (_boardSize.y * 0.5f), 0);
        _signBoard.transform.localScale = _boardSize;

        ApplyMaterials();

        CreateText();

        transform.rotation = Quaternion.Euler(0, 45, 0);
    }

    private void ApplyMaterials()
    {
        Renderer postRenderer = _signPost.GetComponent<Renderer>();
        if (postRenderer != null)
        {
            Material postMat = new Material(Shader.Find("Standard"));
            postMat.color = _signColor * 0.7f;
            postRenderer.material = postMat;
        }

        Renderer boardRenderer = _signBoard.GetComponent<Renderer>();
        if (boardRenderer != null)
        {
            Material boardMat = new Material(Shader.Find("Standard"));
            boardMat.color = _signColor;
            boardRenderer.material = boardMat;
        }
    }

    private void CreateText()
    {
        _textObject = new GameObject("SignText");
        _textObject.transform.parent = _signBoard.transform;

        _textMesh = _textObject.AddComponent<TextMeshPro>();

        _textMesh.text = FormatMessage(_message);
        _textMesh.fontSize = 2f;
        _textMesh.color = _textColor;
        _textMesh.alignment = TextAlignmentOptions.Center;
        _textMesh.fontStyle = FontStyles.Bold;

        RectTransform rectTransform = _textObject.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, -(_boardSize.z * 0.5f + 0.001f));
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.sizeDelta = new Vector2(_boardSize.x * 0.9f, _boardSize.y * 0.8f);

        _textObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        if (_textMesh == null)
        {
            Destroy(_textObject.GetComponent<TextMeshPro>());
            CreateFallbackText();
        }
    }

    private void CreateFallbackText()
    {
        TextMesh textMesh = _textObject.AddComponent<TextMesh>();
        textMesh.text = FormatMessage(_message);
        textMesh.fontSize = 20;
        textMesh.characterSize = 0.02f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = _textColor;

        _textObject.transform.localPosition = new Vector3(0, 0, -(_boardSize.z * 0.5f + 0.01f));
        _textObject.transform.localScale = Vector3.one;

        GameObject textBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        textBackground.name = "TextBackground";
        textBackground.transform.parent = _signBoard.transform;
        textBackground.transform.localPosition = new Vector3(0, 0, -(_boardSize.z * 0.5f + 0.005f));
        textBackground.transform.localRotation = Quaternion.identity;
        textBackground.transform.localScale = new Vector3(_boardSize.x * 0.95f, _boardSize.y * 0.9f, 1f);

        Renderer bgRenderer = textBackground.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            Material bgMat = new Material(Shader.Find("Standard"));
            bgMat.color = _signColor * 0.9f;
            bgRenderer.material = bgMat;
        }
    }

    private string FormatMessage(string message)
    {
        if (message.Length <= 30) return message;

        string[] words = message.Split(' ');
        string result = "";
        string currentLine = "";
        int maxLineLength = 25;

        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxLineLength)
            {
                if (!string.IsNullOrEmpty(result)) result += "\n";
                result += currentLine;
                currentLine = word;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine)) currentLine += " ";
                currentLine += word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += currentLine;
        }

        return result;
    }

    void Update()
    {
        if (_playerTransform == null || _textMesh == null) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);

        if (distance <= _readableDistance)
        {
            float alpha = Mathf.Clamp01(1f - (distance / _readableDistance));
            Color currentColor = _textMesh.color;
            currentColor.a = alpha;
            _textMesh.color = currentColor;

            _textObject.SetActive(true);
        }
        else
        {
            _textObject.SetActive(false);
        }
    }

    public void SetMessage(string message)
    {
        _message = message;
        if (_textMesh != null)
        {
            _textMesh.text = FormatMessage(message);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _readableDistance);
    }
}