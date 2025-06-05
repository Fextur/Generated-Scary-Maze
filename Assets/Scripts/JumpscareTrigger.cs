using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Debug Info")]
    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private bool _hasTriggered = false;

    [Header("Visual Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    public void Initialize(Vector2Int gridPos)
    {
        _gridPosition = gridPos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_hasTriggered && other.CompareTag("Player"))
        {
            TriggerJumpscare();
        }
    }

    private void TriggerJumpscare()
    {
        _hasTriggered = true;

        if (JumpscareManager.Instance != null)
        {
            JumpscareManager.Instance.TriggerJumpscare(_gridPosition);
        }

        Destroy(gameObject, 0.1f);
    }

    public Vector2Int GetGridPosition()
    {
        return _gridPosition;
    }

    public bool HasBeenTriggered()
    {
        return _hasTriggered;
    }

    void OnDrawGizmos()
    {
        if (!_showDebugInfo) return;

        Gizmos.color = _hasTriggered ? Color.gray : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1f, 0.8f));

        Gizmos.color = _hasTriggered ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1.2f, 1f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            $"Grid: {_gridPosition}\nTriggered: {_hasTriggered}");
#endif
    }
}