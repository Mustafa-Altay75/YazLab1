using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun gerçekten neyi taşıdığını bilen tek yer.
///
/// Yalan yakalama bu sınıfa dayanıyor: model oyuncunun <b>iddiasını</b>
/// raporluyor, doğruluğuna burası karar veriyor. Modelden "bu doğru mu"
/// diye sormanın anlamı yok — oyun durumunu bilmiyor, bilmesinin yolu da
/// yok.
///
/// Mustafa'nın PlayerInventory'si asa ve kitabı zaten tutuyor; ona
/// dokunmak yerine sarmalanıyor. Altın burada tutuluyor çünkü rüşvet
/// görevi bizim tarafımızda.
/// </summary>
public class PlayerPossessions : MonoBehaviour
{
    /// <summary>JSON şemasındaki claims değerleri. Burada ve şemada aynı olmalı.</summary>
    public const string Asa = "asa";
    public const string BuyuKitabi = "buyu_kitabi";
    public const string HucreAnahtari = "hucre_anahtari";
    public const string Hirsizlar = "hirsizlar";
    public const string Yok = "yok";

    [Tooltip("Boş bırakılırsa aynı objedeki PlayerInventory kullanılır.")]
    [SerializeField] private PlayerInventory inventory;

    [Tooltip("Hırsızlar alt edildi mi diye bakılacak yer. Boşsa sahneden aranır.")]
    [SerializeField] private GuardEncounter encounter;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        if (encounter == null) encounter = FindAnyObjectByType<GuardEncounter>();
    }

    /// <summary>
    /// Şemadaki bir iddianın karşılığı gerçekten üzerinde mi.
    ///
    /// <b>hucre_anahtari her zaman false.</b> Hücre anahtarını iskelet
    /// alıp kapıyı açıyor, oyuncunun eline hiç geçmiyor. Yani bu iddia
    /// kurgunun içine yerleştirilmiş bir yalan tuzağı; oyuncu "hücre
    /// anahtarı bende" derse Paladin'in bunu bilmesi gerekir.
    /// </summary>
    public bool Has(string claim)
    {
        switch (claim)
        {
            case Asa:
            case BuyuKitabi:
                return inventory != null && inventory.IsArmed;

            case HucreAnahtari:
                return false;

            case Hirsizlar:
                // Taşınan bir eşya değil, yapılmış bir iş: ganimet odasına
                // sızan iki hırsız alt edildi mi. Oyuncunun Paladin'e
                // söyleyebileceği tek doğrulanabilir başarı bu.
                return encounter != null && encounter.Defeated;
        }

        return false;
    }

    /// <summary>Modele verilecek "şu an taşıdıkları" listesi.</summary>
    public string TasidiklariMetni()
    {
        var liste = new List<string>();
        if (Has(Asa)) liste.Add(Asa);
        if (Has(BuyuKitabi)) liste.Add(BuyuKitabi);

        return liste.Count > 0 ? string.Join(" ve ", liste.ToArray()) : "hicbir sey";
    }

    /// <summary>Modele verilecek "taşımadıkları" listesi. Yalanı o anlasın diye.</summary>
    public string TasimadiklariMetni()
    {
        var liste = new List<string>();
        if (!Has(Asa)) liste.Add(Asa);
        if (!Has(BuyuKitabi)) liste.Add(BuyuKitabi);
        liste.Add(HucreAnahtari);

        return liste.Count > 0 ? string.Join(" ve ", liste.ToArray()) : "hicbir sey";
    }
}
