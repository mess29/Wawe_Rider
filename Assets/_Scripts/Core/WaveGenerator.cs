using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D; 

public class WaveGenerator : MonoBehaviour
{
    [Header("Gerekli Referanslar")]
    public Transform playerTransform;
    public Transform invisibleWall; 

    [Header("Dalga Ayarları")]
    public GameObject waveChunkPrefab;
    public float chunkLength = 50f;
    public int chunksOnScreen = 5;
    public int pointsPerChunk = 20;

    [Header("Toplanabilir Nesneler")]
    public GameObject fuelCanPrefab;
    [Range(0, 1)] public float fuelCanSpawnChance = 0.5f; 
    public float fuelCanHeightOffset = 1f;
    
    public GameObject goldCoinPrefab;
    [Range(0, 1)] public float goldCoinSpawnChance = 0.7f; 
    public float goldCoinHeightOffset = 1f;

    public GameObject obstaclePrefab; 
    [Range(0, 1)] public float obstacleSpawnChance = 0.4f; 
    public float obstacleHeightOffset = -0.2f; // Suya hafif gömük

    [Header("Zorluk Ayarları")]
    public float startWaveHeight = 2f;
    public float maxWaveHeight = 10f;
    public float startWaveFrequency = 0.1f;
    public float maxWaveFrequency = 0.5f;
    public float difficultyIncreaseRate = 0.001f;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float lastChunkEndX = 0f;
    
    // GAP DÜZELTMESİ İÇİN YENİ DEĞİŞKEN:
    // Bir önceki chunk'ın bittiği Y noktasını saklar, böylece yeni chunk oradan başlar.
    private float lastChunkEndY = 0f; 

    private float perlinNoiseOffsetX = 0f; 
    private float startPlayerX; 

    void Start()
    {
        if (playerTransform == null) return;

        startPlayerX = playerTransform.position.x;
        lastChunkEndX = playerTransform.position.x - (chunkLength * (chunksOnScreen / 2f));
        perlinNoiseOffsetX = Random.Range(0f, 100f); 

        // İlk başlangıç Y'si (0 olsun)
        lastChunkEndY = 0f;

        if (invisibleWall != null)
        {
            float wallStartX = (lastChunkEndX + chunkLength) + (chunkLength / 4f);
            invisibleWall.position = new Vector3(wallStartX, playerTransform.position.y, 0);
        }

        for (int i = 0; i < chunksOnScreen; i++)
        {
            SpawnChunk(i == 0); // İlk chunk mı?
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float triggerX = lastChunkEndX - (chunkLength * 1.5f);
        if (playerTransform.position.x > triggerX)
        {
            SpawnChunk(false);
            DestroyOldestChunk();
        }
    }

    void SpawnChunk(bool isFirst)
    {
        Vector3 spawnPosition = new Vector3(lastChunkEndX, 0, 0);
        GameObject newChunk = Instantiate(waveChunkPrefab, spawnPosition, Quaternion.identity, transform);
        activeChunks.Add(newChunk);
        
        // Tag ayarı (VehicleController Ground check için)
        newChunk.tag = "Ground"; 

        GenerateWaveShape(newChunk, isFirst);

        lastChunkEndX += chunkLength;
    }

    void GenerateWaveShape(GameObject chunk, bool isFirst)
    {
        SpriteShapeController ssc = chunk.GetComponent<SpriteShapeController>();
        Spline spline = ssc.spline;
        spline.Clear(); 

        List<Vector2> edgePoints = new List<Vector2>(); 

        float travelledDistance = Mathf.Max(0, playerTransform.position.x - startPlayerX);
        float difficultyLerp = Mathf.Clamp01(travelledDistance * difficultyIncreaseRate);
        
        float currentWaveHeight = Mathf.Lerp(startWaveHeight, maxWaveHeight, difficultyLerp);
        float currentWaveFrequency = Mathf.Lerp(startWaveFrequency, maxWaveFrequency, difficultyLerp);

        // --- DALGA ÇİZİMİ ---
        for (int i = 0; i <= pointsPerChunk; i++)
        {
            float x = (float)i / pointsPerChunk * chunkLength;
            float y;

            // GAP DÜZELTMESİ (Kritik Nokta)
            if (i == 0 && !isFirst)
            {
                // Eğer ilk nokta ise ve bu oyunun en başı değilse,
                // Y değerini, bir önceki chunk'ın bittiği Y'ye eşitle.
                y = lastChunkEndY;
            }
            else
            {
                // Normal Perlin Noise hesaplaması
                float noiseX = (x + perlinNoiseOffsetX) * currentWaveFrequency;
                y = Mathf.PerlinNoise(noiseX, 0f) * currentWaveHeight;
            }
            
            // Son nokta ise bu Y değerini sakla (sonraki chunk için)
            if (i == pointsPerChunk)
            {
                lastChunkEndY = y;
            }

            spline.InsertPointAt(i, new Vector3(x, y, 0));
            edgePoints.Add(new Vector2(x, y)); 

            if (i > 0 && i < pointsPerChunk)
            {
                spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                spline.SetLeftTangent(i, new Vector3(-0.5f, 0, 0));
                spline.SetRightTangent(i, new Vector3(0.5f, 0, 0));
            }
        }
        
        // NESNE YERLEŞTİRME (Raycast ile düzeltildi)
        SpawnObjectsWithRaycast(chunk, fuelCanPrefab, fuelCanSpawnChance, fuelCanHeightOffset);
        SpawnObjectsWithRaycast(chunk, goldCoinPrefab, goldCoinSpawnChance, goldCoinHeightOffset);
        SpawnObjectsWithRaycast(chunk, obstaclePrefab, obstacleSpawnChance, obstacleHeightOffset);

        // Alt kapatma
        spline.InsertPointAt(pointsPerChunk + 1, new Vector3(chunkLength, -50f, 0)); 
        spline.InsertPointAt(pointsPerChunk + 2, new Vector3(0, -50f, 0));           
        
        perlinNoiseOffsetX += chunkLength;

        EdgeCollider2D collider = chunk.GetComponent<EdgeCollider2D>();
        if (collider != null)
        {
            collider.SetPoints(edgePoints);
        }
    }

    // YENİ: Raycast ile güvenli nesne yerleştirme
    void SpawnObjectsWithRaycast(GameObject chunk, GameObject prefab, float chance, float offset)
    {
        if (prefab == null || Random.value > chance) return;

        // Chunk içinde rastgele bir X (local)
        float randomX = Random.Range(chunkLength * 0.1f, chunkLength * 0.9f);
        
        // Bu noktadaki spline yüksekliğini bulmak zor olabilir,
        // bu yüzden basit bir Raycast mantığı kullanalım veya yaklaşık hesaplayalım.
        // Chunk daha yeni oluşturulduğu için collider henüz bake edilmemiş olabilir.
        // Bu yüzden en sağlıklısı spline noktaları arasından seçim yapmaktır.
        
        // Basitleştirilmiş Yaklaşım: En yakın spline noktasını bul
        int closestPointIndex = Mathf.RoundToInt((randomX / chunkLength) * pointsPerChunk);
        SpriteShapeController ssc = chunk.GetComponent<SpriteShapeController>();
        Vector3 pointPos = ssc.spline.GetPosition(closestPointIndex);

        Vector3 spawnLocalPos = new Vector3(pointPos.x, pointPos.y + offset, 0);
        
        Instantiate(prefab, chunk.transform.TransformPoint(spawnLocalPos), Quaternion.identity, chunk.transform);
    }

    void DestroyOldestChunk()
    {
        if (activeChunks.Count > chunksOnScreen)
        {
            Destroy(activeChunks[0]);
            activeChunks.RemoveAt(0);

            if (invisibleWall != null)
            {
                invisibleWall.Translate(chunkLength, 0, 0);
            }
        }
    }
}