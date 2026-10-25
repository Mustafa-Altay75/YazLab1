using UnityEngine;
using TMPro;

/// <summary>
/// Çarpışma sırasındaki can göstergesi.
///
/// Sadece çarpışma sürerken görünüyor; oyunun geri kalanında ekranda can
/// barı taşımanın anlamı yok, çünkü başka hiçbir yerde hasar alınmıyor.
///
/// Oyuncunun ölmesi mümkün değil (Health'te canDie kapalı). O hâlde canı
/// neden gösteriyoruz: muhafızların vuruşu görünmezse çarpışma tek taraflı
/// bir animasyona dönüşür. Sayının düşmesi, karşı tarafın da bir şey
/// yaptığının tek işareti.
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] private GuardEncounter encounter;
    [SerializeField] private Health playerHealth;
    [SerializeField] private TextMeshProUGUI label;

    [Tooltip("Gösterge, muhafızlar düştükten sonra bu kadar daha ekranda kalır (saniye).")]
    [SerializeField] private float lingerSeconds = 2.5f;

    private float gizlenmeZamani = -1f;

    private void Awake()
    {
        if (encounter == null) encounter = FindAnyObjectByType<GuardEncounter>();

        if (playerHealth == null)
        {
            var inv = FindAnyObjectByType<PlayerInventory>();
            if (inv != null) playerHealth = inv.GetComponent<Health>();
        }

        Goster(false);
    }

    private void Update()
    {
        if (encounter == null || playerHealth == null || label == null) return;

        bool aktif = encounter.FightStarted && !encounter.Defeated;

        if (encounter.Defeated && gizlenmeZamani < 0f && encounter.FightStarted)
        {
            gizlenmeZamani = Time.time + lingerSeconds;
        }

        if (!aktif && gizlenmeZamani > 0f && Time.time < gizlenmeZamani) aktif = true;

        Goster(aktif);
        if (!aktif) return;

        label.text = "Can  " + playerHealth.Current + " / " + playerHealth.Max;
    }

    private void Goster(bool acik)
    {
        if (label == null) return;
        if (label.transform.parent != null) label.transform.parent.gameObject.SetActive(acik);
        else label.gameObject.SetActive(acik);
    }
}
