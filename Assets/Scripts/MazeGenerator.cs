using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField]
    private MazeCell _mazeCellPrefab;

    [Header("Progression")]
    [SerializeField] private int _winMazeSize = 20;
    private int _currentMazeSize = 5;

    private MazeCell[,] _mazeGrid;

    [SerializeField]
    private GameObject _startMarker;

    [SerializeField]
    private GameObject _endMarker;

    [SerializeField, Tooltip("Message that will appear on the sign at the start")]
    private string _instructionMessage = "Find your way to the exit while avoiding the zombie!";

    private Vector2Int _startPosition = new Vector2Int(0, 0);
    private Vector2Int _endPosition;

    [SerializeField]
    private GameObject _playerPrefab;

    private GameObject _currentPlayer;

    private RuntimeNavMeshBaker _navMeshBaker;
    private JumpscareManager _jumpscareManager;

    [SerializeField] private GameObject _instructionSignPrefab;

    private const float CELL_SPACING = 1.2f;

    public int GetMazeWidth()
    {
        return _currentMazeSize;
    }

    public int GetMazeDepth()
    {
        return _currentMazeSize;
    }

    public Vector2Int GetStartPosition()
    {
        return _startPosition;
    }

    public Vector2Int GetEndPosition()
    {
        return _endPosition;
    }

    public void SetMazeSize(int size)
    {
        _currentMazeSize = size;
    }

    public bool IsWinSize()
    {
        return _currentMazeSize > _winMazeSize;
    }

    void Start()
    {
        GameManager.Instance.SetMazeGenerator(this);

        _navMeshBaker = FindFirstObjectByType<RuntimeNavMeshBaker>();
        if (_navMeshBaker == null)
        {
            GameObject navMeshBakerObj = new GameObject("RuntimeNavMeshBaker");
            _navMeshBaker = navMeshBakerObj.AddComponent<RuntimeNavMeshBaker>();
        }

        _jumpscareManager = FindFirstObjectByType<JumpscareManager>();
        if (_jumpscareManager == null)
        {
            GameObject jumpscareObj = new GameObject("JumpscareManager");
            _jumpscareManager = jumpscareObj.AddComponent<JumpscareManager>();
        }
    }

    public void GenerateNewMaze()
    {
        StartCoroutine(GenerateMazeCoroutine());
    }

    private IEnumerator GenerateMazeCoroutine()
    {
        ClearMaze();

        yield return StartCoroutine(GenerateMazeStructure());

        PlaceGameObjects();

        _jumpscareManager.GenerateJumpscarePositions();
        _jumpscareManager.PlaceJumpscareTriggers();

        yield return StartCoroutine(BakeNavMeshForMaze());

        yield return StartCoroutine(SpawnZombieAfterDelay());
    }

    private IEnumerator GenerateMazeStructure()
    {
        _mazeGrid = new MazeCell[_currentMazeSize, _currentMazeSize];

        CreateGroundPlane();

        for (int x = 0; x < _currentMazeSize; x++)
        {
            for (int z = 0; z < _currentMazeSize; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x * CELL_SPACING, 0, z * CELL_SPACING), Quaternion.identity);
                _mazeGrid[x, z].transform.parent = transform;
            }

            if (x % 5 == 0)
            {
                yield return null;
            }
        }

        GenerateMaze(null, _mazeGrid[0, 0]);

        yield return null;
    }

    private void CreateGroundPlane()
    {
        GameObject ground = GameObject.FindGameObjectWithTag("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.tag = "Ground";
            ground.name = "MazeGround";
        }

        float mazeCenter = (_currentMazeSize - 1) * CELL_SPACING * 0.5f;
        ground.transform.position = new Vector3(mazeCenter, -0.05f, mazeCenter);

        float scaleSize = ((_currentMazeSize * CELL_SPACING) + 2f) / 10f;
        ground.transform.localScale = new Vector3(scaleSize, 1, scaleSize);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null && groundRenderer.material != null)
        {
            groundRenderer.material.color = new Color(0.3f, 0.5f, 0.3f);
        }
    }

    private void PlaceGameObjects()
    {
        _startPosition = new Vector2Int(0, 0);
        SetEndPosition();
        PlaceStartAndEndMarkers();
    }

    private IEnumerator BakeNavMeshForMaze()
    {
        if (_navMeshBaker != null)
        {
            _navMeshBaker.BakeNavMeshForMaze(this);

            while (!_navMeshBaker.IsNavMeshReady)
            {
                yield return null;
            }
        }
    }

    private void PlaceInstructionSign()
    {
        if (_instructionSignPrefab == null)
        {
            return;
        }

        MazeCell startCell = _mazeGrid[_startPosition.x, _startPosition.y];
        Vector3 signPosition = startCell.transform.position + new Vector3(0.22f, -0.02f, 0.22f);

        GameObject sign = Instantiate(_instructionSignPrefab, signPosition, Quaternion.Euler(0, 225, 0));
        sign.tag = "Sign";

        SignTextSetup signText = sign.GetComponent<SignTextSetup>();
        if (signText != null && !string.IsNullOrEmpty(_instructionMessage))
        {
            signText.SetMessage(_instructionMessage);
        }
    }

    private IEnumerator SpawnZombieAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }
    }

    private void ClearMaze()
    {
        if (_navMeshBaker != null)
        {
            _navMeshBaker.ClearNavMesh();
        }

        if (_jumpscareManager != null)
        {
            _jumpscareManager.ClearAllJumpscares();
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (_currentPlayer != null)
        {
            Destroy(_currentPlayer);
        }

        GameObject[] existingZombies = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject zombie in existingZombies)
        {
            Destroy(zombie);
        }

        GameObject[] existingSigns = GameObject.FindGameObjectsWithTag("Sign");
        foreach (GameObject sign in existingSigns)
        {
            Destroy(sign);
        }

        GameObject[] endMarkers = GameObject.FindObjectsByType<WinTrigger>(FindObjectsSortMode.InstanceID).Select(wt => wt.gameObject).ToArray();
        foreach (GameObject marker in endMarkers)
        {
            Destroy(marker);
        }

        GameObject[] startMarkers = GameObject.FindGameObjectsWithTag("StartMarker");
        foreach (GameObject marker in startMarkers)
        {
            Destroy(marker);
        }

        JumpscareTrigger[] jumpscareMarkers = FindObjectsByType<JumpscareTrigger>(FindObjectsSortMode.None);
        foreach (JumpscareTrigger trigger in jumpscareMarkers)
        {
            if (trigger != null && trigger.gameObject != null)
            {
                Destroy(trigger.gameObject);
            }
        }
    }

    private void SetEndPosition()
    {
        _endPosition = new Vector2Int(_currentMazeSize - 1, _currentMazeSize - 1);
    }

    private void PlaceStartAndEndMarkers()
    {
        Vector3 startPos = new Vector3(_startPosition.x * CELL_SPACING, 0, _startPosition.y * CELL_SPACING);

        if (_startMarker != null)
        {
            Instantiate(_startMarker, startPos + new Vector3(0, 0.3f, 0), Quaternion.identity);
        }

        PlaceInstructionSign();

        if (_playerPrefab != null)
        {
            Vector3 playerSpawnPos = new Vector3(startPos.x, 15f, startPos.z);
            _currentPlayer = Instantiate(_playerPrefab, playerSpawnPos, Quaternion.identity);

            GameManager.Instance.SetPlayer(_currentPlayer);
        }

        if (_endMarker != null)
        {
            Vector3 endPos = new Vector3(_endPosition.x * CELL_SPACING, 0, _endPosition.y * CELL_SPACING);
            GameObject endObj = Instantiate(_endMarker, endPos + new Vector3(0, 0.3f, 0), Quaternion.identity);

            SphereCollider trigger = endObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.4f;

            endObj.AddComponent<WinTrigger>();
        }
    }

    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();
        ClearWalls(previousCell, currentCell);

        MazeCell nextCell;
        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = Mathf.RoundToInt(currentCell.transform.position.x / CELL_SPACING);
        int z = Mathf.RoundToInt(currentCell.transform.position.z / CELL_SPACING);

        if (x + 1 < _currentMazeSize)
        {
            var cellToRight = _mazeGrid[x + 1, z];

            if (cellToRight.IsVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];

            if (cellToLeft.IsVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < _currentMazeSize)
        {
            var cellToFront = _mazeGrid[x, z + 1];

            if (cellToFront.IsVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];

            if (cellToBack.IsVisited == false)
            {
                yield return cellToBack;
            }
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        Vector3 prevPos = previousCell.transform.position;
        Vector3 currPos = currentCell.transform.position;

        if (prevPos.x < currPos.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
        }
        else if (prevPos.x > currPos.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
        }
        else if (prevPos.z < currPos.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
        }
        else if (prevPos.z > currPos.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
        }
    }
}