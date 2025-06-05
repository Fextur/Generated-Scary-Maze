using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Screens")]
    [SerializeField] private GameObject _startScreen;
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _gameOverScreen;

    [Header("Game Components")]
    [SerializeField] private ZombieSpawner _zombieSpawner;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip _gameOverSound;
    [SerializeField] private AudioClip _winSound;
    [SerializeField] private float _soundVolume = 0.8f;

    private GameObject _player;
    private MazeGenerator _mazeGenerator;
    private bool _gameStarted = false;

    private AudioSource _soundEffectAudioSource;

    public enum GameState
    {
        StartScreen,
        Playing,
        Won,
        GameOver
    }

    private GameState _currentState = GameState.StartScreen;

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
        SetupSoundEffects();

        ShowStartScreen();
    }

    private void SetupSoundEffects()
    {
        _soundEffectAudioSource = gameObject.AddComponent<AudioSource>();
        _soundEffectAudioSource.volume = _soundVolume;
        _soundEffectAudioSource.playOnAwake = false;
        _soundEffectAudioSource.loop = false;

        _soundEffectAudioSource.spatialBlend = 0f;
    }

    public void SetPlayer(GameObject player)
    {
        _player = player;
    }

    public void SetMazeGenerator(MazeGenerator mazeGenerator)
    {
        _mazeGenerator = mazeGenerator;
    }

    public void ShowStartScreen()
    {
        SetGameState(GameState.StartScreen);
    }

    public void StartGame()
    {
        CleanupExistingZombies();

        PoopManager.CleanAllPoops();

        if (_mazeGenerator != null)
        {
            _mazeGenerator.GenerateNewMaze();
        }

        if (_zombieSpawner != null)
        {
            _zombieSpawner.ResetSpawner();
        }

        SetGameState(GameState.Playing);
    }

    public void WinGame()
    {
        PoopManager.CleanAllPoops();

        if (_mazeGenerator != null)
        {
            int currentSize = _mazeGenerator.GetMazeWidth();
            int nextSize = currentSize * 2;
            _mazeGenerator.SetMazeSize(nextSize);



        }
        if (_mazeGenerator.IsWinSize())
        {
            SetGameState(GameState.Won);
        }
        else
        {
            StartGame();
        }
    }

    public void GameOver(string reason)
    {
        PoopManager.CleanAllPoops();

        SetGameState(GameState.GameOver);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RestartGame()
    {
        PoopManager.CleanAllPoops();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool IsGameStarted()
    {
        return _gameStarted;
    }

    public GameState GetCurrentState()
    {
        return _currentState;
    }

    private void SetGameState(GameState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case GameState.StartScreen:
                SetUIState(startScreen: true, winScreen: false, gameOverScreen: false);
                SetPlayerState(active: false, controllerEnabled: true);
                SetCursorState(visible: true, locked: false);
                _gameStarted = false;
                break;

            case GameState.Playing:
                SetUIState(startScreen: false, winScreen: false, gameOverScreen: false);
                SetPlayerState(active: true, controllerEnabled: true);
                SetCursorState(visible: false, locked: true);
                _gameStarted = true;
                break;

            case GameState.Won:
                SetUIState(startScreen: false, winScreen: true, gameOverScreen: false);
                SetPlayerState(active: true, controllerEnabled: false);
                SetCursorState(visible: true, locked: false);
                _gameStarted = false;

                PlayWinSound();

                break;

            case GameState.GameOver:
                SetUIState(startScreen: false, winScreen: false, gameOverScreen: true);
                SetPlayerState(active: true, controllerEnabled: false);
                SetCursorState(visible: true, locked: false);
                _gameStarted = false;

                PlayGameOverSound();
                break;
        }
    }

    private void PlayGameOverSound()
    {
        if (_gameOverSound != null && _soundEffectAudioSource != null)
        {
            _soundEffectAudioSource.PlayOneShot(_gameOverSound);
        }
    }

    private void PlayWinSound()
    {
        if (_winSound != null && _soundEffectAudioSource != null)
        {
            _soundEffectAudioSource.PlayOneShot(_winSound);
        }
    }

    private void SetUIState(bool startScreen, bool winScreen, bool gameOverScreen)
    {
        if (_startScreen != null) _startScreen.SetActive(startScreen);
        if (_winScreen != null) _winScreen.SetActive(winScreen);
        if (_gameOverScreen != null) _gameOverScreen.SetActive(gameOverScreen);
    }

    private void SetPlayerState(bool active, bool controllerEnabled)
    {
        if (_player == null) return;

        _player.SetActive(active);

        if (active)
        {
            FPSController controller = _player.GetComponent<FPSController>();
            if (controller != null)
            {
                controller.enabled = controllerEnabled;
            }
        }
    }

    private void SetCursorState(bool visible, bool locked)
    {
        Cursor.visible = visible;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void CleanupExistingZombies()
    {
        GameObject[] existingZombies = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject zombie in existingZombies)
        {
            Destroy(zombie);
        }
    }
}