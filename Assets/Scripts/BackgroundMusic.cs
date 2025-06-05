using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioClip _backgroundTrack;
    [SerializeField] private float _volume = 0.5f;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.clip = _backgroundTrack;
        _audioSource.loop = true;
        _audioSource.volume = _volume;
        _audioSource.playOnAwake = false;

        if (_backgroundTrack != null)
        {
            _audioSource.Play();
        }
    }

    public void SetVolume(float newVolume)
    {
        _volume = Mathf.Clamp01(newVolume);
        if (_audioSource != null)
        {
            _audioSource.volume = _volume;
        }
    }
}