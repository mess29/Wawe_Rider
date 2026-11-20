using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement; 

/// <summary>
/// GDD Madde 3, 8, 9 & 10: Skor, UI, Oyun Sonu ve CAN SİSTEMİ.
/// Tüm oyun döngüsünü ve sahneler arası geçişi yönetir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gerekli Referanslar")]
    public Transform playerTransform; 
    private Rigidbody2D playerRb;

    // --- DURUM YÖNETİMİ ---
    public bool IsGameOver { get; private set; } = false;

    // --- CAN SİSTEMİ ---
    [Header("Sağlık Ayarları")]
    public int maxLives = 3;
    private int currentLives;

    // --- SKOR VE İLERLEME ---
    private float currentDistance = 0f;
    private int currentGold = 0;
    private float startPlayerX; 

    [Header("Denizcilik Ayarları")]
    public float unitsPerNauticalMile = 100f; 

    [Header("HUD Referansları (Oyun İçi)")]
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI goldText;     
    public TextMeshProUGUI speedText;    
    
    [Tooltip("Can ikonlarını (Image veya GameObject) buraya sürükleyin.")]
    public GameObject[] heartIcons; 

    [Header("Game Over UI Referansları")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalGoldText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        Time.timeScale = 1f; // Zamanın aktığından emin ol
        IsGameOver = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (playerTransform == null)
        {
            Debug.LogError("GameManager: Player Transform atanmamış!");
            enabled = false; 
            return;
        }

        playerRb = playerTransform.GetComponent<Rigidbody2D>();
        startPlayerX = playerTransform.position.x;
        currentGold = 0;
        
        // Canları fulle
        currentLives = maxLives;
        
        UpdateUI(); 
        UpdateHeartsUI(); 
    }

    void Update()
    {
        if (IsGameOver || playerTransform == null) return;

        float rawDistance = playerTransform.position.x - startPlayerX;
        if (rawDistance > currentDistance)
        {
            currentDistance = rawDistance;
        }

        UpdateUI();
    }

    public void AddGold(int amount)
    {
        if (IsGameOver) return;
        currentGold += amount;
    }

    public void TakeDamage(int damageAmount)
    {
        if (IsGameOver) return;

        currentLives -= damageAmount;
        if (currentLives < 0) currentLives = 0;

        Debug.Log("Hasar Alındı! Kalan Can: " + currentLives);
        
        UpdateHeartsUI(); 

        if (currentLives <= 0)
        {
            TriggerGameOver("Teknen Parçalandı!");
        }
    }

    // --- OYUN SONU MANTIĞI ---

    public void TriggerGameOver(string reason)
    {
        if (IsGameOver) return; 
        Debug.Log("Oyun Bitti! Sebep: " + reason);
        IsGameOver = true;
        Time.timeScale = 0f; // Oyunu durdur

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
                finalScoreText.text = "Mesafe: " + GetDistanceInNM().ToString("F1") + " NM";
            
            if (finalGoldText != null)
                finalGoldText.text = "Toplam Altın: " + currentGold.ToString();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Ana Menü sahnesini yükler.
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Zamanı düzelt (Game Over'da durmuş olabilir)
        SceneManager.LoadScene("MainMenu"); 
    }

    // --- Yardımcılar ---

    public float GetDistanceInNM()
    {
        return currentDistance / unitsPerNauticalMile;
    }

    public float GetSpeedInKnots()
    {
        if (playerRb != null)
        {
            return playerRb.velocity.magnitude * 1.94f;
        }
        return 0f;
    }

    void UpdateUI()
    {
        if (distanceText != null)
            distanceText.text = "Mesafe: " + GetDistanceInNM().ToString("F1") + " NM";

        if (goldText != null)
            goldText.text = "Altın: " + currentGold.ToString();

        if (speedText != null)
            speedText.text = "Hız: " + GetSpeedInKnots().ToString("F0") + " kn";
    }

    void UpdateHeartsUI()
    {
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] == null) continue;

            if (i < currentLives)
            {
                heartIcons[i].SetActive(true);
            }
            else
            {
                heartIcons[i].SetActive(false);
            }
        }
    }
}