using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pazarlığın gidişatını gösteren çubuk.
///
/// Güven ve sabır şimdiye kadar yalnızca konsolda görünüyordu; oyuncunun
/// ikna mı ettiğini yoksa batırdığını mı anlaması imkânsızdı. Her tur
/// yirmi saniye süren bir konuşmada bu çok pahalı bir belirsizlik.
///
/// Çubuk ortadan iki yöne dolar: sağa doğru yeşil (GEÇİŞ), sola doğru
/// kırmızı (HÜKÜM). Uçlar <see cref="PaladinNegotiation"/>'daki eşikler;
/// burada sabit sayı yok, eşik değişirse çubuk da değişir.
///
/// <b>Çubuk sohbet panelinin parçası değil.</b> Önce panelin gövdesinin
/// içinde duruyordu ve iki ayrı şey — konuşmanın kendisi ile konuşmanın
/// skoru — tek bir kutuya sıkışmıştı. Artık ekranın üstünde kendi başına
/// duruyor; panel gizlenince de görünmeye devam ediyor, çünkü oyuncu
/// oyuna dönüp geri geldiğinde nerede kaldığını hatırlaması gerekiyor.
/// </summary>
public class NegotiationBar : MonoBehaviour
{
    [Header("Arayüz")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Ortadan sağa dolan parça. Pivotu solda olmalı.")]
    [SerializeField] private RectTransform fillPositive;

    [Tooltip("Ortadan sola dolan parça. Pivotu sağda olmalı.")]
    [SerializeField] private RectTransform fillNegative;

    [SerializeField] private TextMeshProUGUI valueText;

    [Header("Bağlantılar")]
    [SerializeField] private PaladinNegotiation negotiation;

    [Header("Görünüm")]
    [SerializeField] private Color positiveColor = new Color(0.45f, 0.72f, 0.36f, 1f);
    [SerializeField] private Color negativeColor = new Color(0.78f, 0.28f, 0.24f, 1f);

    [Tooltip("Çubuğun yarı genişliği (piksel). Tam genişlik bunun iki katı.")]
    [SerializeField] private float halfWidth = 150f;

    [Tooltip("Çubuk hedefe bu hızda kayar; ani zıplamasın.")]
    [SerializeField] private float lerpSpeed = 6f;

    private float gosterilen;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();

        if (fillPositive != null) fillPositive.GetComponent<Image>().color = positiveColor;
        if (fillNegative != null) fillNegative.GetComponent<Image>().color = negativeColor;

        gosterilen = 0f;
        Goster(false);
    }

    private void Update()
    {
        if (negotiation == null) { Goster(false); return; }

        // Konuşma başlayınca görünür, bir sona varılınca çekilir. Arada
        // panel gizlense de kalır: pazarlık hâlâ sürüyor.
        bool acik = negotiation.HasStarted && !negotiation.IsOver;
        Goster(acik);
        if (!acik) return;

        gosterilen = Mathf.Lerp(gosterilen, negotiation.Trust, Time.deltaTime * lerpSpeed);

        float gecis = Mathf.Max(1, negotiation.TrustToPass);
        float hukum = Mathf.Max(1, -negotiation.TrustToFail);

        float artiOran = Mathf.Clamp01(Mathf.Max(0f, gosterilen) / gecis);
        float eksiOran = Mathf.Clamp01(Mathf.Max(0f, -gosterilen) / hukum);

        if (fillPositive != null) fillPositive.sizeDelta = new Vector2(artiOran * halfWidth, fillPositive.sizeDelta.y);
        if (fillNegative != null) fillNegative.sizeDelta = new Vector2(eksiOran * halfWidth, fillNegative.sizeDelta.y);

        if (valueText != null)
        {
            valueText.text = negotiation.Trust >= 0
                ? "İkna  " + negotiation.Trust + " / " + negotiation.TrustToPass
                : "Öfke  " + (-negotiation.Trust) + " / " + (-negotiation.TrustToFail);
        }

    }

    private void Goster(bool acik)
    {
        if (barRoot != null && barRoot.activeSelf != acik) barRoot.SetActive(acik);
    }
}
