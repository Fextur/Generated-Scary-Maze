using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class JumpscareManager : MonoBehaviour
{
    [Header("Jumpscare Settings")]
    [SerializeField] private int _tilesPerJumpscare = 10;
    [SerializeField] private float _minDistanceFromStartEnd = 2f;

    [Header("Jumpscare Effects")]
    [SerializeField] private Sprite[] _jumpscareImages;
    [SerializeField] private AudioClip[] _jumpscareSounds;
    [SerializeField] private float _soundVolume = 0.8f;
    [SerializeField] private float _jumpscareImageDuration = 1.5f;
    [SerializeField] private float _jumpscareImageScale = 1.2f;

    [Header("Screen Shake")]
    [SerializeField] private float _shakeDuration = 0.8f;
    [SerializeField] private float _shakeMagnitude = 0.4f;
    [SerializeField] private AnimationCurve _shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("UI References")]
    [SerializeField] private Canvas _jumpscareCanvas;
    [SerializeField] private Image _jumpscareImageUI;

    private MazeGenerator _mazeGenerator;
    private Camera _playerCamera;
    private List<Vector2Int> _jumpscarePositions = new List<Vector2Int>();
    private HashSet<Vector2Int> _triggeredPositions = new HashSet<Vector2Int>();

    public static JumpscareManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        FindPlayerCamera();
        SetupJumpscareUI();
    }

    private void FindPlayerCamera()
    {
        _playerCamera = Camera.main;
        if (_playerCamera == null)
        {
            _playerCamera = FindFirstObjectByType<Camera>();
        }
        if (_playerCamera == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
    }

    private void SetupJumpscareUI()
    {
        if (_jumpscareCanvas == null)
        {
            GameObject canvasObj = new GameObject("JumpscareCanvas");
            _jumpscareCanvas = canvasObj.AddComponent<Canvas>();
            _jumpscareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _jumpscareCanvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (_jumpscareImageUI == null)
        {
            GameObject imageObj = new GameObject("JumpscareImage");
            imageObj.transform.SetParent(_jumpscareCanvas.transform, false);

            _jumpscareImageUI = imageObj.AddComponent<Image>();
            _jumpscareImageUI.color = Color.white;

            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        _jumpscareImageUI.gameObject.SetActive(false);
    }

    public void GenerateJumpscarePositions()
    {
        if (_mazeGenerator == null) return;

        _jumpscarePositions.Clear();
        _triggeredPositions.Clear();

        int mazeSize = _mazeGenerator.GetMazeWidth();
        int totalTiles = mazeSize * mazeSize;
        int numJumpscares = Mathf.FloorToInt(totalTiles / (float)_tilesPerJumpscare);

        numJumpscares = Mathf.Max(1, numJumpscares);

        Vector2Int startPos = _mazeGenerator.GetStartPosition();
        Vector2Int endPos = _mazeGenerator.GetEndPosition();

        int attempts = 0;
        while (_jumpscarePositions.Count < numJumpscares && attempts < 100)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, mazeSize),
                Random.Range(0, mazeSize)
            );

            if (IsValidJumpscarePosition(randomPos, startPos, endPos, mazeSize))
            {
                _jumpscarePositions.Add(randomPos);
            }

            attempts++;
        }

    }

    private bool IsValidJumpscarePosition(Vector2Int pos, Vector2Int startPos, Vector2Int endPos, int mazeSize)
    {
        if (pos == startPos || pos == endPos) return false;

        if (_jumpscarePositions.Contains(pos)) return false;

        float distanceFromStart = Vector2Int.Distance(pos, startPos);
        if (distanceFromStart < _minDistanceFromStartEnd) return false;

        float distanceFromEnd = Vector2Int.Distance(pos, endPos);
        if (distanceFromEnd < _minDistanceFromStartEnd) return false;

        return true;
    }

    public void PlaceJumpscareTriggers()
    {
        GameObject[] existingTriggers = GameObject.FindGameObjectsWithTag("JumpscareTrigger");
        foreach (GameObject trigger in existingTriggers)
        {
            Destroy(trigger);
        }

        foreach (Vector2Int pos in _jumpscarePositions)
        {
            CreateJumpscareTrigger(pos);
        }
    }

    private void CreateJumpscareTrigger(Vector2Int gridPos)
    {
        float cellSpacing = 1.2f;
        Vector3 worldPos = new Vector3(gridPos.x * cellSpacing, 0.5f, gridPos.y * cellSpacing);

        GameObject triggerObj = new GameObject($"JumpscareTrigger_{gridPos.x}_{gridPos.y}");
        triggerObj.transform.position = worldPos;

        BoxCollider trigger = triggerObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.8f, 1f, 0.8f);

        JumpscareTrigger jumpscareComponent = triggerObj.AddComponent<JumpscareTrigger>();
        jumpscareComponent.Initialize(gridPos);
    }

    public void TriggerJumpscare(Vector2Int gridPosition)
    {
        if (_triggeredPositions.Contains(gridPosition)) return;

        _triggeredPositions.Add(gridPosition);

        StartCoroutine(PlayJumpscareSequence());
    }

    private IEnumerator PlayJumpscareSequence()
    {
        PlayJumpscareSound();

        yield return StartCoroutine(ShowJumpscareImage());

        StartCoroutine(ShakeCamera());
    }

    private void PlayJumpscareSound()
    {
        if (_jumpscareSounds.Length == 0) return;

        AudioClip soundToPlay = _jumpscareSounds[Random.Range(0, _jumpscareSounds.Length)];

        GameObject tempAudio = new GameObject("JumpscareAudio");
        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
        audioSource.clip = soundToPlay;
        audioSource.volume = _soundVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Destroy(tempAudio, soundToPlay.length + 0.1f);
    }

    private IEnumerator ShowJumpscareImage()
    {
        if (_jumpscareImages.Length == 0 || _jumpscareImageUI == null) yield break;

        Sprite imageToShow = _jumpscareImages[Random.Range(0, _jumpscareImages.Length)];
        _jumpscareImageUI.sprite = imageToShow;

        _jumpscareImageUI.gameObject.SetActive(true);
        _jumpscareImageUI.transform.localScale = Vector3.one * _jumpscareImageScale;
        _jumpscareImageUI.color = Color.white;

        yield return new WaitForSeconds(_jumpscareImageDuration);

        float fadeTime = 0.3f;
        float elapsed = 0f;
        Color startColor = _jumpscareImageUI.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            _jumpscareImageUI.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        _jumpscareImageUI.gameObject.SetActive(false);
        _jumpscareImageUI.transform.localScale = Vector3.one;
    }

    private IEnumerator ShakeCamera()
    {
        if (_playerCamera == null) yield break;

        Vector3 originalPosition = _playerCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < _shakeDuration)
        {
            float normalizedTime = elapsed / _shakeDuration;
            float currentIntensity = _shakeIntensityCurve.Evaluate(normalizedTime) * _shakeMagnitude;

            float x = Random.Range(-1f, 1f) * currentIntensity;
            float y = Random.Range(-1f, 1f) * currentIntensity;

            _playerCamera.transform.localPosition = new Vector3(
                originalPosition.x + x,
                originalPosition.y + y,
                originalPosition.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        _playerCamera.transform.localPosition = originalPosition;
    }

    public void ClearAllJumpscares()
    {
        _jumpscarePositions.Clear();
        _triggeredPositions.Clear();

        JumpscareTrigger[] existingTriggers = FindObjectsByType<JumpscareTrigger>(FindObjectsSortMode.None);
        foreach (JumpscareTrigger trigger in existingTriggers)
        {
            if (trigger != null && trigger.gameObject != null)
            {
                Destroy(trigger.gameObject);
            }
        }
    }
}
