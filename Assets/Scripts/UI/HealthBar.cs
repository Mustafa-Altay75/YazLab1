using UnityEngine;

/// <summary>
/// Muhafızın başının üstünde duran can çubuğu.
///
/// Çarpışmayı okunur kılan şey bu: vuruşun bir işe yarayıp yaramadığı
/// yoksa hiçbir yerden anlaşılmıyordu. Rakamı ekranın köşesine yazmak
/// yerine hedefin üstüne koymak, hangi muhafıza vurduğunu da gösteriyor.
///
/// Çarpışma başlamadan görünmüyor. Bu bir süs tercihi değil: o aşamada
/// muhafızlar zaten dokunulmaz, dolayısıyla dolu bir çubuk göstermek
/// oyuncuya "vurabilirsin" diye yanlış söz vermek olurdu.
///
/// Dünya uzayında bir Canvas; her karede kameraya dönüyor, yoksa yandan
/// bakınca kayboluyor.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Tooltip("İzlenecek can. Boş bırakılırsa üst objelerde aranır.")]
    [SerializeField] private Health health;

    [Tooltip("Doluluğu gösteren parça. Pivotu solda olmalı; ölçeklenerek kısalıyor.")]
    [SerializeField] private RectTransform fill;

    [Tooltip("Boş bırakılırsa sahnedeki GuardEncounter aranır. Çubuk çarpışma başlayınca görünür.")]
    [SerializeField] private GuardEncounter encounter;

    [Tooltip("Takip edilen objeye göre konum (birim).")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.75f, 0f);

    [Tooltip("Ölü muhafızın çubuğu bu kadar sonra kaybolur (saniye).")]
    [SerializeField] private float fadeAfterDeath = 1.2f;

    private Transform takip;
    private Camera kamera;
    private Canvas tuval;
    private float olumZamani = -1f;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (encounter == null) encounter = FindAnyObjectByType<GuardEncounter>();

        takip = health != null ? health.transform : transform.parent;
        tuval = GetComponent<Canvas>();
        Goster(false);
    }

    private void LateUpdate()
    {
        if (health == null || fill == null) return;

        if (health.IsDead && olumZamani < 0f) olumZamani = Time.time;

        bool carpismaVar = encounter == null || encounter.FightStarted;
        bool olumGecti = olumZamani >= 0f && Time.time - olumZamani > fadeAfterDeath;

        if (!carpismaVar || olumGecti) { Goster(false); return; }

        Goster(true);

        if (takip != null) transform.position = takip.position + worldOffset;

        // Kamerayı her karede yeniden bul: sahne kamerası değişirse de çalışsın.
        if (kamera == null) kamera = Camera.main;
        if (kamera != null) transform.rotation = kamera.transform.rotation;

        float oran = health.Max > 0 ? (float)health.Current / health.Max : 0f;
        fill.localScale = new Vector3(Mathf.Clamp01(oran), 1f, 1f);
    }

    /// <summary>
    /// Görünürlüğü Canvas'ı kapatarak ayarlıyor, objeyi kapatarak değil.
    /// Obje kapatılsaydı LateUpdate de durur ve çubuk bir daha hiç
    /// geri gelmezdi.
    /// </summary>
    private void Goster(bool acik)
    {
        if (tuval != null) tuval.enabled = acik;
    }
}
