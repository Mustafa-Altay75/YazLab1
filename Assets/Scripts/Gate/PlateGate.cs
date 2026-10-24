using UnityEngine;

/// <summary>
/// Paladin'in odasına açılan çift kanatlı kapı. İki kanat zıt yönlere,
/// araba kapısı gibi dışa doğru açılır.
///
/// Kapıyı asıl açacak olan oyuncu değil, basınç plakasında bekleyen
/// iskelettir; plaka bağlanınca OpenGate/CloseGate dışarıdan çağrılacak.
/// allowPlayerInteraction şimdilik açık, böylece kapı plakasız da
/// denenebiliyor. Plaka bağlandığında kapatılacak.
/// </summary>
public class PlateGate : MonoBehaviour, IInteractable
{
    [Header("Kanatlar")]
    [SerializeField] private Transform leafWest;
    [SerializeField] private Transform leafEast;

    [Header("Açılma")]
    [Tooltip("Kanatların kapalı konumdan sapma açısı. Batı kanadı eksi, doğu kanadı artı yönde döner.")]
    [SerializeField] private float openAngle = 105f;
    [SerializeField] private float openSpeed = 2.5f;

    [Header("Test")]
    [Tooltip("Plaka bağlanana kadar oyuncunun E ile açabilmesi için açık bırakıldı.")]
    [SerializeField] private bool allowPlayerInteraction = true;
    [SerializeField] private string promptText = "[E] Kapıyı aç";

    private Quaternion kapaliBati, kapaliDogu;
    private Quaternion acikBati, acikDogu;
    private bool acik;

    /// <summary>Kapı tamamen veya kısmen açık mı.</summary>
    public bool IsOpen { get { return acik; } }

    private void Awake()
    {
        if (leafWest != null)
        {
            kapaliBati = leafWest.localRotation;
            acikBati = kapaliBati * Quaternion.Euler(0f, -openAngle, 0f);
        }
        if (leafEast != null)
        {
            kapaliDogu = leafEast.localRotation;
            acikDogu = kapaliDogu * Quaternion.Euler(0f, openAngle, 0f);
        }
    }

    private void Update()
    {
        float hiz = Time.deltaTime * openSpeed;
        if (leafWest != null)
            leafWest.localRotation = Quaternion.Slerp(leafWest.localRotation, acik ? acikBati : kapaliBati, hiz);
        if (leafEast != null)
            leafEast.localRotation = Quaternion.Slerp(leafEast.localRotation, acik ? acikDogu : kapaliDogu, hiz);
    }

    public string GetInteractPrompt()
    {
        if (!allowPlayerInteraction) return string.Empty;
        return acik ? string.Empty : promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (!allowPlayerInteraction || acik) return;
        OpenGate();
    }

    /// <summary>Plaka basılınca çağrılır.</summary>
    public void OpenGate()
    {
        acik = true;
        KanatCarpismasi(false);
    }

    /// <summary>Plakadan inilince çağrılır.</summary>
    public void CloseGate()
    {
        acik = false;
        KanatCarpismasi(true);
    }

    /// <summary>
    /// Kanat collider'ları yalnızca kapalıyken yolu kesmeli. Açılırken
    /// kapatılıyor, yoksa dönen kanat oyuncuyu kenara itiyor.
    /// </summary>
    private void KanatCarpismasi(bool aktif)
    {
        if (leafWest != null)
        {
            var c = leafWest.GetComponent<Collider>();
            if (c != null) c.enabled = aktif;
        }
        if (leafEast != null)
        {
            var c = leafEast.GetComponent<Collider>();
            if (c != null) c.enabled = aktif;
        }
    }
}
