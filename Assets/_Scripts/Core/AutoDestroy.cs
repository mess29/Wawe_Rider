using UnityEngine;

/// <summary>
/// Eklendiği nesneyi belirli bir süre sonra veya Particle System bitince yok eder.
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [Tooltip("Otomatik yok olma süresi (Eğer Particle System bulunamazsa bu süre kullanılır)")]
    public float defaultDelay = 2f;

    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        
        if (ps != null)
        {
            // Particle System'in ana modülünü al
            var main = ps.main;
            
            // Efektin süresi + parçacıkların yaşam ömrü kadar bekle
            // Böylece son parçacık da kaybolduktan sonra nesne silinir.
            float totalDuration = main.duration + main.startLifetime.constantMax;
            
            Destroy(gameObject, totalDuration);
        }
        else
        {
            // Particle System yoksa varsayılan süreyi kullan
            Destroy(gameObject, defaultDelay);
        }
    }
}