using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;

public class RuntimeNavMeshBaker : MonoBehaviour
{
    [Header("NavMesh Settings")]
    [SerializeField] private float _agentRadius = 0.2f;
    [SerializeField] private float _agentHeight = 1.5f;
    [SerializeField] private float _maxSlope = 45f;
    [SerializeField] private float _stepHeight = 0.4f;
    [SerializeField] private float _dropHeight = 4f;
    [SerializeField] private float _jumpDistance = 0f;
    [SerializeField] private float _minRegionArea = 2f;

    [Header("Baking Bounds")]
    [SerializeField] private float _extraBounds = 2f;

    private NavMeshData _navMeshData;
    private NavMeshDataInstance _navMeshInstance;
    private bool _isNavMeshReady = false;

    public bool IsNavMeshReady => _isNavMeshReady;
    public static RuntimeNavMeshBaker Instance { get; private set; }

    public static event Action OnNavMeshReady;

    void Awake()
    {
        Instance = this;
    }

    public void BakeNavMeshForMaze(MazeGenerator mazeGenerator)
    {
        StartCoroutine(BakeNavMeshCoroutine(mazeGenerator));
    }

    private IEnumerator BakeNavMeshCoroutine(MazeGenerator mazeGenerator)
    {
        _isNavMeshReady = false;

        ClearExistingNavMesh();

        yield return null;

        Bounds bakingBounds = CalculateMazeBounds(mazeGenerator);
        NavMeshBuildSettings buildSettings = CreateBuildSettings();
        List<NavMeshBuildSource> buildSources = CollectBuildSources(bakingBounds);

        _navMeshData = NavMeshBuilder.BuildNavMeshData(
            buildSettings,
            buildSources,
            bakingBounds,
            Vector3.zero,
            Quaternion.identity
        );

        if (_navMeshData != null)
        {
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            _isNavMeshReady = true;

            OnNavMeshReady?.Invoke();
        }
    }

    private void ClearExistingNavMesh()
    {
        if (_navMeshInstance.valid)
        {
            NavMesh.RemoveNavMeshData(_navMeshInstance);
        }
        _isNavMeshReady = false;
    }

    private Bounds CalculateMazeBounds(MazeGenerator mazeGenerator)
    {
        int mazeWidth = mazeGenerator.GetMazeWidth();
        int mazeDepth = mazeGenerator.GetMazeDepth();

        Vector3 center = new Vector3(
            (mazeWidth - 1) * 0.5f,
            1f,
            (mazeDepth - 1) * 0.5f
        );

        Vector3 size = new Vector3(
            mazeWidth + _extraBounds * 2,
            4f,
            mazeDepth + _extraBounds * 2
        );

        return new Bounds(center, size);
    }

    private NavMeshBuildSettings CreateBuildSettings()
    {
        return new NavMeshBuildSettings
        {
            agentTypeID = 0,
            agentRadius = _agentRadius,
            agentHeight = _agentHeight,
            agentSlope = _maxSlope,
            agentClimb = _stepHeight,
            ledgeDropHeight = _dropHeight,
            maxJumpAcrossDistance = _jumpDistance,
            minRegionArea = _minRegionArea,
            overrideVoxelSize = false,
            voxelSize = 0.16666667f,
            overrideTileSize = false,
            tileSize = 256
        };
    }

    private List<NavMeshBuildSource> CollectBuildSources(Bounds bounds)
    {
        var buildSources = new List<NavMeshBuildSource>();

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (IsWithinBounds(renderer.bounds, bounds) && ShouldIncludeInNavMesh(renderer.gameObject))
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null)
                {
                    buildSources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = mesh,
                        transform = renderer.transform.localToWorldMatrix,
                        area = 0
                    });
                }
            }
        }

        foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (IsWithinBounds(collider.bounds, bounds) &&
                ShouldIncludeInNavMesh(collider.gameObject) &&
                collider.GetComponent<Renderer>() == null)
            {
                buildSources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    sourceObject = collider,
                    transform = collider.transform.localToWorldMatrix,
                    area = 0
                });
            }
        }

        return buildSources;
    }

    private bool IsWithinBounds(Bounds objectBounds, Bounds bakingBounds)
    {
        return bakingBounds.Contains(objectBounds.center) || bakingBounds.Intersects(objectBounds);
    }

    private bool ShouldIncludeInNavMesh(GameObject obj)
    {
        if (obj.CompareTag("Player") || obj.CompareTag("Zombie") || obj.name.Contains("Sign"))
            return false;

        if (obj.name.Contains("Wall") || obj.name.Contains("Ground") || obj.name.Contains("Floor"))
            return true;

        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.name.Contains("MazeCell") || parent.name.Contains("MazeGenerator"))
                return true;
            parent = parent.parent;
        }

        var collider = obj.GetComponent<Collider>();
        return collider != null && !collider.isTrigger;
    }

    public void ClearNavMesh()
    {
        ClearExistingNavMesh();
    }

    void OnDestroy()
    {
        ClearNavMesh();
    }
}