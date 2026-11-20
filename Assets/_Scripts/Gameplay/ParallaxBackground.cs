using UnityEngine;

/// <summary>
/// GDD Madde 5.1: Görsel Dil ve Efektler.
/// Sonsuz koşu hissini güçlendirmek için Gelişmiş Parallax sistemi.
/// Özellikler: Sonsuz döngü, Y ekseni kontrolü ve Otomatik Kaydırma (Rüzgar efekti).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Header("Temel Ayarlar")]
        [Tooltip("Hareket edecek arka plan görseli (Sprite Renderer içermeli)")]
        public Transform layerTransform;
        
        [Tooltip("X Ekseninde takip oranı.\n1 = Gökyüzü (Sabit/Uzak), 0.5 = Orta Mesafe, 0 = Yakın")]
        [Range(0f, 1f)] 
        public float parallaxFactorX;

        [Tooltip("Y Ekseninde takip oranı.\n1 = Tam takip, 0 = Y ekseninde sabit kalır (Önerilen: Gökyüzü için 0 veya 0.1)")]
        [Range(0f, 1f)]
        public float parallaxFactorY;

        [Header("Ekstra Efektler")]
        [Tooltip("Kamera dursa bile görselin hareket hızı (Örn: Rüzgarlı bulutlar için 0.5)")]
        public float autoMoveSpeedX;

        // Dahili Takip Değişkenleri
        [HideInInspector] public float startPosX;
        [HideInInspector] public float startPosY;
        [HideInInspector] public float length; // Görselin genişliği
        [HideInInspector] public bool isLooping; // Sonsuz döngüye uygun mu?
    }

    [Header("Genel Ayarlar")]
    [Tooltip("Takip edilecek kamera (Genellikle Main Camera)")]
    public Transform cameraTransform;
    public ParallaxLayer[] layers;

    void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
            {
                Debug.LogError("ParallaxBackground: Main Camera bulunamadı!");
                enabled = false;
                return;
            }
        }

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;

            // Başlangıç pozisyonlarını kaydet
            layer.startPosX = layer.layerTransform.position.x;
            layer.startPosY = layer.layerTransform.position.y;
            
            // Görselin genişliğini hesapla (Sonsuz döngü için şart)
            SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Sprite'ın dünya üzerindeki gerçek genişliğini al
                layer.length = sr.bounds.size.x;
                layer.isLooping = true;
            }
            else
            {
                Debug.LogWarning($"ParallaxLayer: {layer.layerTransform.name} nesnesinde SpriteRenderer yok, döngü çalışmayacak.");
                layer.isLooping = false; 
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;

            // --- 1. X EKSENİ HESAPLAMALARI ---
            
            // a) Kamera hareketine göre ne kadar kaymalı? (Parallax Etkisi)
            float distFromCamera = (cameraTransform.position.x * layer.parallaxFactorX);
            
            // b) Otomatik hareket (Rüzgar) var mı?
            // Zamanla artan bir ofset ekliyoruz.
            float autoScrollOffset = (layer.autoMoveSpeedX * Time.time);

            // c) Döngü kontrolü için "kalan" mesafe (Temp)
            // Bu değer, kameranın görselin başlangıcına göre nerede olduğunu söyler.
            float temp = (cameraTransform.position.x * (1 - layer.parallaxFactorX));

            // Yeni X Pozisyonu: Başlangıç + Kamera Etkisi + Rüzgar Etkisi
            float newX = layer.startPosX + distFromCamera + autoScrollOffset;

            // --- 2. Y EKSENİ HESAPLAMALARI ---
            
            // Y ekseni genellikle daha basittir. 
            // Eğer FactorY 0 ise, görsel Y ekseninde hiç kıpırdamaz (StartPosY'de kalır).
            // Eğer FactorY 1 ise, kamera ile birebir aynı oranda hareket eder.
            float camDeltaY = cameraTransform.position.y - layer.startPosY; // Başlangıca göre değişim
            float newY = layer.startPosY + (camDeltaY * layer.parallaxFactorY);

            // --- 3. POZİSYONU UYGULA ---
            layer.layerTransform.position = new Vector3(newX, newY, layer.layerTransform.position.z);

            // --- 4. SONSUZ DÖNGÜ (RESETLEME) ---
            if (layer.isLooping)
            {
                // Eğer kamera (temp), görselin sınırını aştıysa görseli ileri/geri ışınla.
                // Rüzgar etkisi (autoScrollOffset) döngü mantığını bozmasın diye onu hesaptan düşüyoruz.
                float tempWithWind = temp - autoScrollOffset;

                if (tempWithWind > layer.startPosX + layer.length)
                {
                    layer.startPosX += layer.length;
                }
                else if (tempWithWind < layer.startPosX - layer.length)
                {
                    layer.startPosX -= layer.length;
                }
            }
        }
    }
}