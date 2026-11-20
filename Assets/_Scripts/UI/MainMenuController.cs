using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli

/// <summary>
/// Ana Menü işlevlerini yönetir.
/// Play, Quit, Linkler vb.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Sahne İsimleri")]
    [Tooltip("Oyun sahnesinin tam adı (Örn: Game)")]
    public string gameSceneName = "Game";

    // --- BUTON FONKSİYONLARI ---

    public void PlayGame()
    {
        // Oyunu başlat
        Debug.Log("Oyun Yükleniyor...");
        SceneManager.LoadScene(gameSceneName);
        
        // Zaman akışını düzelttiğimizden emin olalım (Game Over'da durdurmuştuk)
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        // Oyundan çık
        Debug.Log("Oyundan Çıkılıyor...");
        Application.Quit();
    }

    public void OpenGarage()
    {
        // İleride buraya Garaj sahnesi eklenecek
        Debug.Log("Garaj henüz yapım aşamasında!");
    }

    public void OpenSettings()
    {
        // İleride buraya Ayarlar paneli eklenecek
        Debug.Log("Ayarlar henüz yapım aşamasında!");
    }
}