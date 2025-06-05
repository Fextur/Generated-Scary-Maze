using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private float _spawnDelay = 10f;

    private bool _hasSpawned = false;

    void Start()
    {
        StartCoroutine(SpawnZombieAfterDelay());
    }

    public void ResetSpawner()
    {
        DestroyExistingZombies();
        _hasSpawned = false;
        StopAllCoroutines();
        StartCoroutine(SpawnZombieAfterDelay());
    }

    private IEnumerator SpawnZombieAfterDelay()
    {
        yield return new WaitForSeconds(_spawnDelay);

        if (!_hasSpawned && _zombiePrefab != null)
        {
            SpawnZombie();
            _hasSpawned = true;
        }
    }

    private void SpawnZombie()
    {
        var mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        if (mazeGenerator == null)
        {
            return;
        }

        Vector2Int startCoords = mazeGenerator.GetStartPosition();
        Vector3 spawnPosition = new Vector3(startCoords.x, 0.5f, startCoords.y);

        GameObject zombie = Instantiate(_zombiePrefab, spawnPosition, Quaternion.identity);
        zombie.tag = "Zombie";

        if (zombie.GetComponent<NavMeshAgent>() != null)
        {
            Destroy(zombie.GetComponent<NavMeshAgent>());
        }
    }

    private void DestroyExistingZombies()
    {
        var existingZombies = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (var zombie in existingZombies)
        {
            Destroy(zombie);
        }
    }
}