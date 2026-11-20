using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GDD v3.3: Fizik ve Kontrol Düzeltmesi.
/// Düzeltmeler:
/// 1. Motor artık "isGrounded" (Zemine değiyor) durumunda da çalışıyor.
/// 2. Boost gücü ve vektörü uçmayı engelleyecek şekilde kısıldı.
/// 3. Batma/Yüzme mantığı katı zemin (EdgeCollider) ile uyumlu hale getirildi.
/// </summary>
public class VehicleController : MonoBehaviour
{
    [Header("Motor ve Hareket")]
    public float motorGucu = 8000f;    // Kütle 10 iken ideal itiş
    public float frenGucu = 4000f;     // Geri itiş
    public float boostGucu = 40f;      // 100 -> 40 (Uçmayı engellemek için düşürüldü)
    
    [Header("Kaldırma Kuvveti (Buoyancy)")]
    // Not: Katı zemin olduğu için bu güç sadece tekne dalganın içine gömülürse (bug vb.) çalışır.
    public float suKaldirmaGucu = 5f; 
    
    [Tooltip("Suda/Zeminde giderken sürtünme (Kayma hissi için düşük tutun)")]
    public float suLinearDrag = 1f; // 3 -> 1 (Daha rahat kaysın)
    public float suAngularDrag = 2f;
    
    [Tooltip("Havadaki sürtünme")]
    public float havaLinearDrag = 0.05f;
    public float havaAngularDrag = 0.5f;

    [Header("Hava Kontrolü")]
    public float havaDonusGucu = 300f; 

    [Header("Yakıt Ayarları")]
    public float maksimumYakit = 100f;
    public float yakitTuketimSuda = 5f; 
    public float yakitTuketimHavada = 2.5f; 
    public float yakitTuketimRolanti = 0.5f; 
    public float boostYakitBedeli = 10f; 

    [Header("UI & Görsel")]
    public Slider yakitSlider; 
    public GameObject splashEffectPrefab; 
    public ParticleSystem foamParticles; 

    // --- Durum Değişkenleri ---
    [HideInInspector] public float mevcutYakit;
    private Rigidbody2D rb;
    private float hareketInput;
    private bool isGrounded = false; 
    private bool isUnderwater = false; 
    
    // Yüzme noktaları
    private Vector2[] floatPoints; 
    
    // Ölümcül Durumlar
    private float suAltindaKalmaSuresi = 0f;
    private float tersDonmeSuresi = 0f;
    private bool isDead = false;
    private float lastVelocityY;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        
        mevcutYakit = maksimumYakit;
        rb.freezeRotation = false; 
        
        UpdateFuelUI();
        if (foamParticles != null) foamParticles.Stop();
        
        // Yüzme noktalarını belirle
        float width = col.bounds.size.x;
        float height = col.bounds.size.y;
        float yOffset = -height / 2f; 
        float xOffset = width / 2f;

        floatPoints = new Vector2[3];
        floatPoints[0] = new Vector2(-xOffset, yOffset); 
        floatPoints[1] = new Vector2(0, yOffset);        
        floatPoints[2] = new Vector2(xOffset, yOffset);  
    }

    void Update()
    {
        if (isDead || GameManager.Instance.IsGameOver) return;

        hareketInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && mevcutYakit >= boostYakitBedeli)
        {
            ApplyBoost();
        }

        CalculateFuelConsumption();
        HandleVFX();
        CheckLifeThreats();
        
        lastVelocityY = rb.velocity.y;
    }

    void FixedUpdate()
    {
        if (isDead || GameManager.Instance.IsGameOver) return;

        // --- 1. ORTAM KONTROLÜ ---
        ApplyBuoyancy(); // isUnderwater ve Drag değerlerini ayarlar

        // --- 2. HAREKET MANTIĞI ---
        // DÜZELTME: Sadece suyun içinde değil, zemine değiyorsa da (isGrounded) motor çalışsın.
        if (isUnderwater || isGrounded)
        {
            // Yüzeyde gidiyoruz (Motor Gücü)
            if (hareketInput > 0)
            {
                // Yere paralel kuvvet uygulamak daha iyidir ama basit right vektörü de iş görür
                rb.AddForce(transform.right * hareketInput * motorGucu * Time.fixedDeltaTime);
            }
            else if (hareketInput < 0)
            {
                rb.AddForce(transform.right * hareketInput * frenGucu * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Havadayız (Hava Kontrolü - Tork)
            if (Mathf.Abs(hareketInput) > 0.1f)
            {
                rb.AddTorque(hareketInput * -havaDonusGucu * Time.fixedDeltaTime);
            }
        }
    }

    void ApplyBuoyancy()
    {
        isUnderwater = false;
        
        // Raycast ile su seviyesini kontrol et
        foreach (Vector2 point in floatPoints)
        {
            Vector3 worldPoint = transform.TransformPoint(point);
            // Işını biraz daha yukarıdan atalım ki yüzeye yakınsak algılasın
            Vector2 rayOrigin = new Vector2(worldPoint.x, worldPoint.y + 2f);
            
            // Sadece "Ground" katmanına/tag'ine çarpacak şekilde filtreleyelim
            // (LayerMask kullanmak daha performanslıdır ama şimdilik Tag kontrolü)
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 5f);

            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                if (worldPoint.y < hit.point.y)
                {
                    isUnderwater = true;
                    // Kaldırma kuvveti (Sadece batarsak çalışır)
                    float depth = hit.point.y - worldPoint.y;
                    Vector2 liftForce = Vector2.up * depth * suKaldirmaGucu;
                    liftForce += Vector2.up * (Mathf.Abs(Physics2D.gravity.y) * rb.mass) / floatPoints.Length;
                    rb.AddForceAtPosition(liftForce, worldPoint);
                }
            }
        }

        // --- SÜRTÜNME AYARLARI ---
        // Zemine değiyorsak VEYA suyun içindeysek su sürtünmesini kullan
        if (isUnderwater || isGrounded)
        {
            rb.drag = suLinearDrag;
            rb.angularDrag = suAngularDrag;
        }
        else
        {
            rb.drag = havaLinearDrag;
            rb.angularDrag = havaAngularDrag;
        }
    }

    void ApplyBoost()
    {
        ConsumeFuel(boostYakitBedeli);
        // DÜZELTME: Y bileşeni (yukarı fırlatma) düşürüldü. 0.5 -> 0.2
        Vector2 boostVector = (transform.right * 1.0f + transform.up * 0.2f).normalized;
        
        // ForceMode2D.Impulse kütleye ani hız verir. Değeri düşürdük.
        rb.AddForce(boostVector * boostGucu, ForceMode2D.Impulse);
    }

    void CalculateFuelConsumption()
    {
        float anlikTuketim = 0f;
        if (Mathf.Abs(hareketInput) > 0.1f) 
        {
            // Suda veya Zeminde
            if (isUnderwater || isGrounded) anlikTuketim = yakitTuketimSuda;
            else anlikTuketim = yakitTuketimHavada; 
        }
        else 
        {
            if (!isUnderwater && !isGrounded) anlikTuketim = yakitTuketimRolanti; 
        }
        ConsumeFuel(anlikTuketim * Time.deltaTime);
    }

    void HandleVFX()
    {
        if (foamParticles != null)
        {
            // Hem suda/zeminde olmalı hem de hızı olmalı
            if ((isUnderwater || isGrounded) && Mathf.Abs(rb.velocity.x) > 1f)
            {
                if (!foamParticles.isPlaying) foamParticles.Play();
            }
            else
            {
                if (foamParticles.isPlaying) foamParticles.Stop();
            }
        }
    }

    void CheckLifeThreats()
    {
        // 1. ALABORA (Ters Dönme) - Sadece zemine/suya değiyorken
        if ((isGrounded || isUnderwater) && transform.up.y < -0.5f) 
        {
            tersDonmeSuresi += Time.deltaTime;
            if (tersDonmeSuresi > 2.0f) Die("Alabora Oldun!");
        }
        else tersDonmeSuresi = 0f;

        // 2. BOĞULMA
        if (transform.position.y < -15f) 
        {
            suAltindaKalmaSuresi += Time.deltaTime;
            if (suAltindaKalmaSuresi > 3.0f) Die("Sulara Gömüldün!");
        }
        else suAltindaKalmaSuresi = 0f;
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        GameManager.Instance.TriggerGameOver(reason);
    }
    
    void ConsumeFuel(float amount)
    {
        mevcutYakit -= amount;
        if (mevcutYakit <= 0)
        {
            mevcutYakit = 0;
            Die("Yakıt Tükendi");
        }
        UpdateFuelUI();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Sert düşüş efekti
            if (lastVelocityY < -8f && splashEffectPrefab != null)
            {
                Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y - 0.5f, 0);
                Instantiate(splashEffectPrefab, spawnPos, Quaternion.identity);
            }
            isGrounded = true;
        }
        else if (collision.gameObject.CompareTag("ObstacleTag"))
        {
            GameManager.Instance.TakeDamage(1);
            Vector2 bounceDir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(bounceDir * 10f, ForceMode2D.Impulse); 
        }
        else if (collision.gameObject.CompareTag("DeadZone"))
        {
            GameManager.Instance.TakeDamage(1); 
            rb.AddForce(Vector2.right * 50f, ForceMode2D.Impulse); 
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("FuelCanTag"))
        {
            mevcutYakit = maksimumYakit;
            UpdateFuelUI();
            Destroy(other.gameObject); 
        }
        else if (other.CompareTag("GoldCoinTag"))
        {
            GameManager.Instance?.AddGold(1); 
            Destroy(other.gameObject); 
        }
    }

    void UpdateFuelUI()
    {
        if (yakitSlider != null) yakitSlider.value = mevcutYakit / maksimumYakit;
    }
}