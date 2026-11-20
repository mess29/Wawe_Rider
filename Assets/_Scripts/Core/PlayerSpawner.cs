using System.Collections;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Gerekli Bileşenler")]
    [Tooltip("Hierarchy'deki Player nesnesi")]
    public GameObject playerObject; 
    
    [Header("Başlangıç Ayarları")]
    [Tooltip("Oyuncunun oyuna başlayacağı X konumu")]
    public float startXPosition = 0f;
    [Tooltip("Oyuncuyu dalganın ne kadar üstüne yerleştireceği (çarpışma hatasını önler)")]
    public float spawnHeightOffset = 0.5f;

    void Start()
    {
        if (playerObject == null)
        {
            Debug.LogError("Player nesnesi atanmamış!");
            return;
        }

        // Oyuncuyu başlangıçta gizle
        playerObject.SetActive(false);

        // Oyuncuyu yerleştirme işlemini başlat
        StartCoroutine(PositionPlayerAfterWaves());
    }

    private IEnumerator PositionPlayerAfterWaves()
    {
        // GÜVENLİK ADIMI:
        // WaveGenerator'ın Start() fonksiyonunun çalışıp ilk dalga collider'larını
        // oluşturduğundan %100 emin olmak için çok kısa bir an bekle.
        yield return new WaitForSeconds(0.2f); 

        // 1. Işını (Raycast) göndereceğimiz yeri belirle:
        // Oyuncunun başlayacağı X konumunda, gökyüzünde bir nokta.
        Vector2 rayStartPoint = new Vector2(startXPosition, 100f);

        // 2. Işını aşağı doğru gönder (200 birim uzağa)
        RaycastHit2D hit = Physics2D.Raycast(rayStartPoint, Vector2.down, 200f);

        // 3. Işın bir şeye çarptı mı? (Dalga collider'ına)
        if (hit.collider != null)
        {
            // Evet, çarptı. Şimdi oyuncuyu tam o noktaya yerleştir.
            Vector3 spawnPosition = new Vector3(
                hit.point.x, // Işının çarptığı X (startXPosition ile aynı olmalı)
                hit.point.y + spawnHeightOffset, // Çarptığı Y + güvenlik payı
                0 
            );

            playerObject.transform.position = spawnPosition;
            playerObject.SetActive(true); // Oyuncuyu görünür/aktif yap
        }
        else
        {
            // Hata durumu: Dalga bulunamadı!
            Debug.LogError("Güvenli Başlangıç Yeri Bulunamadı! Dalgaların Collider'ı var mı?");
            // Acil durum için oyuncuyu (0,0)'da başlat
            playerObject.transform.position = new Vector3(startXPosition, 0, 0);
            playerObject.SetActive(true);
        }
    }
}
