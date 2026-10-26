using UnityEngine;

/// <summary>
/// Konuşmanın bir sonu: koşulu, dünyaya etkisi ve ekran metni.
///
/// <b>Yeni bir son eklemek motoru değiştirmeyi gerektirmez.</b> Bu
/// sınıftan türeyen küçük bir bileşen yazmak, onu sahnede bir objeye
/// koymak ve <see cref="PaladinNegotiation"/>'ın listesine eklemek
/// yeterli. Karar motoru hangi sonların var olduğunu bilmiyor; sadece
/// sırayla "koşulun sağlandı mı" diye soruyor.
///
/// Önce rüşvet sonu da burada duruyordu; kaldırıldığında motordan tek
/// satır silmek yetti. Yapının amacı buydu.
/// </summary>
public abstract class NegotiationEnding : MonoBehaviour
{
    [Header("Son Ekranı")]
    [SerializeField] private string title = "SON";

    [TextArea(2, 5)]
    [SerializeField] private string body = "";

    [Header("Davranış")]
    [Tooltip("Açıkken son ekranı oyuncu çıkış kapısından geçince gelir. " +
             "Kapalıyken koşul sağlanır sağlanmaz gelir.")]
    [SerializeField] private bool waitForExit = true;

    [Tooltip("Açıkken tek seçenek yeniden başlamaktır; 'devam et' gösterilmez.")]
    [SerializeField] private bool restartOnly;

    [Header("Sona Varılınca")]
    [Tooltip("Sohbete düşülecek sistem satırı. Konuşma bu sonla bittiğinde " +
             "oyuncunun ekranında zaten duran tek yer sohbet; oraya hiçbir şey " +
             "yazılmayınca çubuğun dolması dışında bir ibare kalmıyordu.")]
    [TextArea(1, 3)]
    [SerializeField] private string chatLine = "";

    [Tooltip("Panel kapandıktan sonra ekranın ortasında beliren duyuru başlığı. " +
             "Boşsa duyuru hiç gösterilmez.")]
    [SerializeField] private string cueTitle = "";

    [TextArea(1, 3)]
    [SerializeField] private string cueBody = "";

    [Tooltip("Duyuru geçtikten sonra üstte asılı kalan görev hatırlatması. " +
             "waitForExit açıkken anlamlı: oyuncu çıkışa varana kadar durur.")]
    [SerializeField] private string objectiveText = "";

    /// <summary>Son ekranındaki başlık.</summary>
    public string Title { get { return title; } }

    /// <summary>Son ekranındaki metin.</summary>
    public string Body { get { return body; } }

    /// <summary>Ekran, oyuncu çıkıştan geçtikten sonra mı gelecek.</summary>
    public bool WaitForExit { get { return waitForExit; } }

    /// <summary>Oyun kaybedildi mi; tek seçenek yeniden başlamak mı.</summary>
    public bool RestartOnly { get { return restartOnly; } }

    /// <summary>Sona varılınca sohbete düşülecek satır.</summary>
    public string ChatLine { get { return chatLine; } }

    /// <summary>Ekran ortasındaki duyurunun başlığı.</summary>
    public string CueTitle { get { return cueTitle; } }

    /// <summary>Ekran ortasındaki duyurunun gövdesi.</summary>
    public string CueBody { get { return cueBody; } }

    /// <summary>Duyurudan sonra üstte asılı kalan görev hatırlatması.</summary>
    public string ObjectiveText { get { return objectiveText; } }

    /// <summary>
    /// Bu sonun koşulu sağlandı mı. Her turun sonunda sorulur.
    /// </summary>
    public abstract bool IsMet(PaladinNegotiation negotiation);

    /// <summary>
    /// Sona varıldığında dünyada ne değişecek: kapı açılır, bir şey
    /// kaybolur, bir animasyon oynar. Varsayılanı hiçbir şey yapmamak.
    /// </summary>
    public virtual void ApplyToWorld() { }
}
