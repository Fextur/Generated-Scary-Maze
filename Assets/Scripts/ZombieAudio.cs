using UnityEngine;

public class ZombieAudio : MonoBehaviour
{
    [Header("Zombie Sound")]
    [SerializeField] private AudioClip _zombieSound;
    [SerializeField] private float _volume = 0.7f;
    [SerializeField] private float _maxDistance = 15f;
    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.clip = _zombieSound;
        _audioSource.loop = true;
        _audioSource.volume = _volume;
        _audioSource.playOnAwake = false;

        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = _maxDistance;
        _audioSource.minDistance = 1f;

        if (_zombieSound != null)
        {
            _audioSource.Play();
        }
    }

}