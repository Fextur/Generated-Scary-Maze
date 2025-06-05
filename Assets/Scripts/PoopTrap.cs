using UnityEngine;
using System.Collections;

public class PoopTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float _slowDuration = 2f;
    [SerializeField] private float _slowFactor = 0.3f;
    [SerializeField] private float _creatorDelay = 1f;
    [SerializeField] private float _detectionRadius = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _playerStepSound;
    [SerializeField] private AudioClip _zombieStepSound;

    private AudioSource _audioSource;
    private bool _hasBeenTriggered = false;
    private GameObject _creator;
    private float _creationTime;

    void Awake()
    {
        gameObject.tag = "Poop";
        _creationTime = Time.time;
    }

    void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.volume = 0.7f;
        _audioSource.playOnAwake = false;

        Destroy(gameObject, 60f);
    }

    void Update()
    {
        if (_hasBeenTriggered) return;

        CheckForCollisions();
    }

    private void CheckForCollisions()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(player.transform.position.x, 0, player.transform.position.z)
            );

            if (distance <= _detectionRadius)
            {
                bool canAffectCreator = _creator != player || (Time.time - _creationTime) >= _creatorDelay;

                if (canAffectCreator)
                {
                    ApplySlowEffect(player, true);
                    return;
                }
            }
        }

        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject zombie in zombies)
        {
            if (zombie != null)
            {
                float distance = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(zombie.transform.position.x, 0, zombie.transform.position.z)
                );

                if (distance <= _detectionRadius)
                {
                    ApplySlowEffect(zombie, false);
                    return;
                }
            }
        }
    }

    private void ApplySlowEffect(GameObject target, bool isPlayer)
    {
        _hasBeenTriggered = true;

        AudioClip soundToPlay = isPlayer ? _playerStepSound : _zombieStepSound;
        if (soundToPlay != null && _audioSource != null)
        {
            AudioSource targetAudioSource = target.GetComponent<AudioSource>();
            if (targetAudioSource != null)
            {
                targetAudioSource.PlayOneShot(soundToPlay);
            }
            else
            {
                AudioSource tempAudio = target.AddComponent<AudioSource>();
                tempAudio.spatialBlend = 1f;
                tempAudio.volume = 0.7f;
                tempAudio.PlayOneShot(soundToPlay);
                Destroy(tempAudio, soundToPlay.length + 0.1f);
            }
        }
        else
        {
            CreateDefaultStepSoundOnTarget(target, isPlayer);
        }

        if (isPlayer)
        {
            FPSController playerController = target.GetComponent<FPSController>();
            if (playerController != null)
            {
                playerController.StartCoroutine(SlowPlayerEffect(playerController, _slowDuration, _slowFactor));
            }

            PoopScreenEffect screenEffect = target.GetComponent<PoopScreenEffect>();
            if (screenEffect != null)
            {
                screenEffect.TriggerEffect(_slowDuration);
            }
        }
        else
        {
            ZombieController zombieController = target.GetComponent<ZombieController>();
            if (zombieController != null)
            {
                zombieController.StartCoroutine(SlowZombieEffect(zombieController, _slowDuration, _slowFactor));
            }
        }

        Destroy(gameObject);
    }

    public static System.Collections.IEnumerator SlowPlayerEffect(FPSController playerController, float duration, float slowFactor)
    {
        if (playerController == null) yield break;

        float originalSpeed = playerController.GetOriginalMoveSpeed();
        float slowedSpeed = originalSpeed * slowFactor;

        playerController.SetMoveSpeed(slowedSpeed);

        yield return new WaitForSeconds(duration);

        if (playerController != null)
        {
            playerController.ResetMoveSpeed();
        }
    }

    public static System.Collections.IEnumerator SlowZombieEffect(ZombieController zombieController, float duration, float slowFactor)
    {
        if (zombieController == null) yield break;

        float originalSpeed = zombieController.GetOriginalMoveSpeed();
        float slowedSpeed = originalSpeed * slowFactor;

        zombieController.SetMoveSpeed(slowedSpeed);

        yield return new WaitForSeconds(duration);

        if (zombieController != null)
        {
            zombieController.ResetMoveSpeed();
        }
    }

    public void SetCreator(GameObject creator)
    {
        _creator = creator;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }


    private void CreateDefaultStepSoundOnTarget(GameObject target, bool isPlayer)
    {
        float frequency = isPlayer ? 300f : 200f;
        int sampleRate = 44100;
        float duration = 0.3f;
        int samples = Mathf.RoundToInt(sampleRate * duration);

        AudioClip clip = AudioClip.Create("PoopStep", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 3f);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.1f;
        }

        clip.SetData(data, 0);

        AudioSource targetAudioSource = target.GetComponent<AudioSource>();
        if (targetAudioSource != null)
        {
            targetAudioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource tempAudio = target.AddComponent<AudioSource>();
            tempAudio.spatialBlend = 1f;
            tempAudio.volume = 0.7f;
            tempAudio.PlayOneShot(clip);
            Destroy(tempAudio, duration + 0.1f);
        }
    }
}