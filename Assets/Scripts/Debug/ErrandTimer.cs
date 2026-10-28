using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// İskelete verilen her görevi süre tutarak ölçer.
///
/// Proje dokümanının 4.4 maddesi ajanın "farklı başlangıç
/// konumlarında/koşullarında tekrarlanabilir" biçimde test edilmesini ve
/// <i>başarı oranı</i> ile <i>ortalama tamamlama süresi</i> sayılarının
/// raporda sunulmasını istiyor. Takip modu sayesinde iskelet istenen
/// noktada serbest bırakılabiliyor, ama gözle izlemek sayı vermiyor;
/// bu bileşen veriyor.
///
/// Oyun mantığına dokunmuyor: yalnızca <see cref="SkeletonCompanion"/>
/// üzerindeki genel özellikleri okuyor, hiçbir şey yazmıyor. Kapalıyken
/// sahnede hiçbir etkisi yok — <see cref="TestShortcuts"/> ile aynı desen,
/// tek bir anahtar.
///
/// Çıktı: her görev bittiğinde konsola doğrudan rapora yapıştırılabilir bir
/// Markdown tablo satırı basılır. Ekranın sol üstünde de o an çalışan
/// kronometre görünür, böylece ölçümün koştuğu koşu sırasında bellidir.
/// </summary>
public class ErrandTimer : MonoBehaviour
{
    [Tooltip("ANA ANAHTAR. Kapalıyken bu bileşen hiçbir şey yapmaz. " +
             "Ölçüm alınacağı zaman açılır, teslimde kapalı olmalı.")]
    [SerializeField] private bool active;

    [Tooltip("Boş bırakılırsa sahneden aranır.")]
    [SerializeField] private SkeletonCompanion companion;

    [Tooltip("Kapalıyken kronometre ekranda görünmez; ölçüm yine de konsola yazılır.")]
    [SerializeField] private bool showOnScreen = true;

    [Tooltip("Süregelen koşuyu BAŞARISIZ kapatan tuş. Başarı oranının bir anlamı " +
             "olması için başarısızlığın da kaydedilebilmesi gerekiyor: yalnızca " +
             "biten koşuları sayan bir sayaç her zaman %100 yazar.")]
    [SerializeField] private Key failKey = Key.F4;

    [Tooltip("Bu kadar saniyede bitmeyen koşu kendiliğinden başarısız sayılır. " +
             "0 ise otomatik kapatma yok.")]
    [SerializeField] private float giveUpSeconds = 120f;

    /// <summary>Ölçülen bir görev: nereden başladı, ne zaman başladı.</summary>
    private class Olcum
    {
        public string Gorev;
        public Vector3 Baslangic;
        public float BaslangicZamani;
        public int BaslangicTakilma;
        public int BaslangicYenidenYol;
    }

    /// <summary>Biten bir koşunun rapora giren hâli.</summary>
    private class Sonuc
    {
        public string Satir;
        public string Gorev;
        public float Sure;
        public bool Basarili;
    }

    private Olcum suregelen;
    private readonly List<Sonuc> sonuclar = new List<Sonuc>();
    private string sonSatir = "";

    // OnGUI her karede çalışıyor; stili orada kurmak kare başına çöp
    // üretiyordu. Bir kez kurulup saklanıyor.
    private GUIStyle stil;

    private void Update()
    {
        if (!active) return;

        if (companion == null)
        {
            companion = FindAnyObjectByType<SkeletonCompanion>();
            if (companion == null) return;
        }

        if (suregelen == null) { BaslangicAra(); return; }

        if (Keyboard.current != null && Keyboard.current[failKey].wasPressedThisFrame)
        {
            Kapat(false);
            return;
        }

        if (giveUpSeconds > 0f && Time.time - suregelen.BaslangicZamani > giveUpSeconds)
        {
            Kapat(false);
            return;
        }

        BitisAra();
    }

    /// <summary>
    /// Verilmiş ama henüz ölçülmemiş bir emir var mı.
    ///
    /// Emir bayrakları tek yönlü: bir kez true olunca geri dönmüyorlar.
    /// O yüzden bayrağın kendisi değil, o görevin daha önce ölçülmüş olup
    /// olmaması başlangıcı belirliyor; biten görevin bayrağı açık kalsa da
    /// ölçüm yeniden başlamıyor.
    /// </summary>
    private void BaslangicAra()
    {
        if (companion.OrderGiven && !companion.KeyErrandDone && !Olculdu("anahtar"))
        {
            Basla("anahtar");
        }
        else if (companion.PlateOrderGiven && !Olculdu("plaka"))
        {
            Basla("plaka");
        }
        else if (companion.SwordOrderGiven && !companion.IsArmed && !Olculdu("kilic"))
        {
            Basla("kilic");
        }
    }

    /// <summary>Süregelen ölçümün bitiş koşulu sağlandı mı.</summary>
    private void BitisAra()
    {
        bool bitti;

        switch (suregelen.Gorev)
        {
            case "anahtar": bitti = companion.KeyErrandDone; break;
            case "plaka":   bitti = companion.CurrentStateName == "StandOnPlate"; break;
            case "kilic":   bitti = companion.IsArmed; break;
            default:        bitti = false; break;
        }

        if (bitti) Kapat(true);
    }

    private void Basla(string gorev)
    {
        suregelen = new Olcum
        {
            Gorev = gorev,
            Baslangic = companion.transform.position,
            BaslangicZamani = Time.time,
            BaslangicTakilma = companion.StuckCount,
            BaslangicYenidenYol = companion.RepathCount
        };

        Debug.Log("[OLCUM] " + gorev + " başladı. İskeletin bırakıldığı nokta: "
                  + Nokta(suregelen.Baslangic), this);
    }

    /// <summary>
    /// Süregelen koşuyu kapatır. <paramref name="basarili"/> false ise koşu
    /// başarısız sayılır: iskelet hedefe varamadı, takıldı ya da oyuncu
    /// vazgeçti. Başarısız koşu da tabloya giriyor, çünkü başarı oranı
    /// ancak başarısızlık kaydedilebiliyorsa bir ölçüdür.
    /// </summary>
    private void Kapat(bool basarili)
    {
        float sure = Time.time - suregelen.BaslangicZamani;

        // Kuş uçuşu mesafe: iskeletin kat ettiği yol değil, bırakıldığı
        // noktayla varış noktası arasındaki düz çizgi. Raporda asıl işi,
        // başlangıç noktalarının gerçekten birbirinden farklı olduğunu
        // belgelemek.
        float mesafe = Vector3.Distance(suregelen.Baslangic, companion.transform.position);

        // Sayılar nokta ile yazılıyor. Türkçe kültürde virgül basılıyordu ve
        // satır doğrudan rapora yapıştırıldığında "28,7 m" hem okunmuyor hem
        // de tablo hücresini bölme riski taşıyordu. Ölçümde görüldü.
        string satir = string.Format(
            CultureInfo.InvariantCulture,
            "| {0} | {1} | {2} | {3:0.0} m | {4} | {5:0.0} sn | {6} | {7} |",
            sonuclar.Count + 1,
            suregelen.Gorev,
            Nokta(suregelen.Baslangic),
            mesafe,
            basarili ? "başarılı" : "BAŞARISIZ",
            sure,
            companion.RepathCount - suregelen.BaslangicYenidenYol,
            companion.StuckCount - suregelen.BaslangicTakilma);

        sonuclar.Add(new Sonuc
        {
            Satir = satir,
            Gorev = suregelen.Gorev,
            Sure = sure,
            Basarili = basarili
        });

        sonSatir = satir;
        suregelen = null;

        Debug.Log("[OLCUM] " + satir, this);
        Debug.Log("[OLCUM] " + Ozet(), this);
    }

    /// <summary>
    /// Şartnamenin istediği iki sayı: başarı oranı ve ortalama tamamlama
    /// süresi. Her koşudan sonra yeniden yazılıyor, böylece son satır
    /// doğrudan raporun özet satırı oluyor ve elle toplama yapılmıyor.
    ///
    /// Ortalama yalnızca başarılı koşulardan alınıyor; yarıda kesilen bir
    /// koşunun süresi "tamamlama süresi" değildir.
    /// </summary>
    private string Ozet()
    {
        int basarili = 0;
        float toplam = 0f;

        foreach (Sonuc s in sonuclar)
        {
            if (!s.Basarili) continue;
            basarili++;
            toplam += s.Sure;
        }

        float oran = sonuclar.Count > 0 ? 100f * basarili / sonuclar.Count : 0f;
        float ortalama = basarili > 0 ? toplam / basarili : 0f;

        return string.Format(CultureInfo.InvariantCulture,
            "ÖZET: {0} koşu, {1} başarılı (%{2:0.#}), ortalama {3:0.0} sn",
            sonuclar.Count, basarili, oran, ortalama);
    }

    /// <summary>
    /// Bu görev daha önce ölçüldü mü. Emir bayrakları tek yönlü olduğu için
    /// gereken denetim bu: biten görevin bayrağı açık kalsa da ölçüm
    /// yeniden başlamıyor.
    /// </summary>
    private bool Olculdu(string gorev)
    {
        foreach (Sonuc s in sonuclar)
        {
            if (s.Gorev == gorev) return true;
        }
        return false;
    }

    private static string Nokta(Vector3 p)
    {
        return string.Format(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0})", p.x, p.z);
    }

    private void OnGUI()
    {
        if (!active || !showOnScreen || companion == null) return;

        if (stil == null)
        {
            stil = new GUIStyle(GUI.skin.label);
            stil.fontSize = 16;
            stil.normal.textColor = Color.yellow;
        }

        string metin = suregelen != null
            ? string.Format(CultureInfo.InvariantCulture, "[OLCUM] {0}: {1:0.0} sn",
                            suregelen.Gorev, Time.time - suregelen.BaslangicZamani)
            : "[OLCUM] emir bekleniyor";

        GUI.Label(new Rect(10f, 10f, 600f, 24f), metin, stil);

        if (sonSatir != "") GUI.Label(new Rect(10f, 34f, 900f, 24f), sonSatir, stil);
    }
}
