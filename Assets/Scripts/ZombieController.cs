using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 0.8f;
    [SerializeField] private float _rotationSpeed = 120f;
    [SerializeField] private float _attackRange = 0.4f;

    [Header("Dynamic Speed Settings")]
    [SerializeField] private float _speedBoostDistance = 8f;
    [SerializeField] private float _boostedSpeed = 1.5f;

    [Header("AI Settings")]
    [SerializeField] private float _lineOfSightDistance = 3f;
    [SerializeField] private float _targetUpdateInterval = 0.2f;
    [SerializeField] private float _stoppingDistance = 0.5f;

    [Header("NavMesh Settings")]
    [SerializeField] private float _acceleration = 8f;
    [SerializeField] private float _angularSpeed = 120f;

    private Transform _playerTransform;
    private NavMeshAgent _navAgent;
    private bool _isActive = false;

    private Vector3 _lastKnownPlayerPosition;
    private bool _hasLineOfSight = false;

    private float _originalMoveSpeed;
    private bool _isSlowedByPoop = false;

    void Start()
    {
        InitializeComponents();
        SetupNavMeshAgent();
        SubscribeToEvents();
        CheckIfNavMeshReady();

        _originalMoveSpeed = _moveSpeed;
    }

    private void InitializeComponents()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            enabled = false;
        }
    }

    private void SetupNavMeshAgent()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        if (_navAgent == null)
        {
            _navAgent = gameObject.AddComponent<NavMeshAgent>();
        }

        _navAgent.speed = _moveSpeed;
        _navAgent.acceleration = _acceleration;
        _navAgent.angularSpeed = _angularSpeed;
        _navAgent.stoppingDistance = _stoppingDistance;
        _navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        _navAgent.radius = 0.2f;
        _navAgent.height = 1.5f;
        _navAgent.baseOffset = 0f;
        _navAgent.updateRotation = false;
        _navAgent.updateUpAxis = false;
        _navAgent.enabled = false;
    }

    private void SubscribeToEvents()
    {
        RuntimeNavMeshBaker.OnNavMeshReady += HandleNavMeshReady;
    }

    private void CheckIfNavMeshReady()
    {
        if (RuntimeNavMeshBaker.Instance != null && RuntimeNavMeshBaker.Instance.IsNavMeshReady)
        {
            HandleNavMeshReady();
        }
    }

    private void HandleNavMeshReady()
    {
        if (_isActive) return;

        _navAgent.enabled = true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            _isActive = true;
            StartChasing();
            InvokeRepeating(nameof(UpdateTarget), 0.1f, _targetUpdateInterval);
        }
    }

    private void StartChasing()
    {
        if (_playerTransform != null)
        {
            _lastKnownPlayerPosition = _playerTransform.position;
            _navAgent.SetDestination(_lastKnownPlayerPosition);
        }
    }

    void Update()
    {
        if (!_isActive || !_navAgent.isOnNavMesh) return;

        UpdateLineOfSight();
        UpdateDynamicSpeed();
        UpdateRotation();
        CheckAttackRange();
    }

    private void UpdateTarget()
    {
        if (!_isActive || _playerTransform == null || !_navAgent.isOnNavMesh) return;

        Vector3 currentPlayerPosition = _playerTransform.position;
        float distanceMoved = Vector3.Distance(currentPlayerPosition, _lastKnownPlayerPosition);

        if (_hasLineOfSight || distanceMoved > 1f)
        {
            _lastKnownPlayerPosition = currentPlayerPosition;
            if (_navAgent.CalculatePath(currentPlayerPosition, new NavMeshPath()))
            {
                _navAgent.SetDestination(currentPlayerPosition);
            }
        }
        else if (!_navAgent.hasPath || _navAgent.remainingDistance < 0.5f)
        {
            PatrolAroundLastKnownPosition();
        }
    }

    private void PatrolAroundLastKnownPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 3f;
        randomDirection.y = 0;
        Vector3 targetPosition = _lastKnownPlayerPosition + randomDirection;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            _navAgent.SetDestination(hit.position);
        }
    }

    private void UpdateLineOfSight()
    {
        if (_playerTransform == null) return;

        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        _hasLineOfSight = !Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            directionToPlayer,
            out RaycastHit hit,
            _lineOfSightDistance
        ) || hit.transform.CompareTag("Player");
    }

    private void UpdateDynamicSpeed()
    {
        if (_playerTransform == null || _isSlowedByPoop) return;

        float navigationDistance = GetNavigationDistanceToPlayer();

        if (navigationDistance > _speedBoostDistance)
        {
            _moveSpeed = _boostedSpeed;
        }
        else
        {
            _moveSpeed = _originalMoveSpeed;
        }

        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.speed = _moveSpeed;
        }
    }

    private float GetNavigationDistanceToPlayer()
    {
        if (_playerTransform == null || _navAgent == null) return float.MaxValue;

        NavMeshPath path = new NavMeshPath();
        if (_navAgent.CalculatePath(_playerTransform.position, path))
        {
            float totalDistance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                totalDistance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return totalDistance;
        }
        else
        {
            return Vector3.Distance(transform.position, _playerTransform.position);
        }
    }

    private void UpdateRotation()
    {
        if (_navAgent.velocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = _navAgent.velocity.normalized;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    private void CheckAttackRange()
    {
        if (_playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
        if (distanceToPlayer < _attackRange)
        {
            GameManager.Instance?.GameOver("Caught by zombie!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance?.GameOver("Caught by zombie!");
        }
    }

    void OnDestroy()
    {
        RuntimeNavMeshBaker.OnNavMeshReady -= HandleNavMeshReady;
        CancelInvoke();
    }

    public void SetMoveSpeed(float newSpeed)
    {
        _moveSpeed = newSpeed;
        _isSlowedByPoop = true;
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.speed = newSpeed;
        }
    }

    public float GetOriginalMoveSpeed()
    {
        return _originalMoveSpeed;
    }

    public void ResetMoveSpeed()
    {
        _isSlowedByPoop = false;
    }

    void OnDrawGizmosSelected()
    {
        if (_playerTransform != null)
        {
            Gizmos.color = _hasLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, _playerTransform.position);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (_navAgent != null && _navAgent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] corners = _navAgent.path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i - 1], corners[i]);
            }
        }

        if (_lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_lastKnownPlayerPosition, Vector3.one * 0.5f);
        }
    }
}