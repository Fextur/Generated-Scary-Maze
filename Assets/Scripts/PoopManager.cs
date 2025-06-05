using UnityEngine;
using System.Collections;

public class PoopManager : MonoBehaviour
{
    [Header("Poop Settings")]
    [SerializeField] private GameObject _poopPrefab;
    [SerializeField] private float _poopCooldown = 3f;
    [SerializeField] private KeyCode _poopKey = KeyCode.P;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _poopSound;

    [Header("Spawn Settings")]
    [SerializeField] private float _spawnHeight = 0.1f;
    private float _lastPoopTime = -999f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() != GameManager.GameState.Playing)
            return;

        if (Input.GetKeyDown(_poopKey))
        {
            TryToPoop();
        }
    }

    private void TryToPoop()
    {
        float timeSinceLastPoop = Time.time - _lastPoopTime;

        if (timeSinceLastPoop < _poopCooldown)
        {
            float timeRemaining = _poopCooldown - timeSinceLastPoop;
            return;
        }

        if (_poopPrefab == null)
        {
            return;
        }

        SpawnPoop();
    }

    private void SpawnPoop()
    {
        Vector3 spawnPosition = new Vector3(transform.position.x, _spawnHeight, transform.position.z);

        GameObject newPoop = Instantiate(_poopPrefab, spawnPosition, Quaternion.identity);

        PoopTrap poopTrap = newPoop.GetComponent<PoopTrap>();
        if (poopTrap != null)
        {
            poopTrap.SetCreator(gameObject);
        }

        if (_poopSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_poopSound);
        }

        _lastPoopTime = Time.time;

    }

    public void ResetCooldown()
    {
        _lastPoopTime = -999f;
    }

    public float GetRemainingCooldown()
    {
        float timeSinceLastPoop = Time.time - _lastPoopTime;
        return Mathf.Max(0, _poopCooldown - timeSinceLastPoop);
    }

    public static void CleanAllPoops()
    {
        GameObject[] existingPoops = GameObject.FindGameObjectsWithTag("Poop");
        foreach (GameObject poop in existingPoops)
        {
            Destroy(poop);
        }
    }
}