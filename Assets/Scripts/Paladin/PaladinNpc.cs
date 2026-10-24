using UnityEngine;

/// <summary>
/// Çıkış kapısını tutan Paladin. Mustafa'nın IInteractable arayüzünü
/// uyguluyor, böylece not ve sandık gibi sıradan bir etkileşim nesnesi
/// oluyor ve PlayerInteraction'a hiç dokunmadan onun sistemine bağlanıyor.
///
/// Bu bileşen diyaloğu BAŞLATMAZ, sadece istendiğini duyurur. Konuşmanın
/// kendisi yerel dil modelinden gelecek; burada hazır cevap tutmak proje
/// gereksinimlerine aykırı olurdu (sahte/hardcoded LLM yanıtı sıfır puan).
/// Diyalog arayüzü eklendiğinde DialogueRequested olayına abone olacak ve
/// konuşma bitince CloseDialogue çağıracak.
/// </summary>
public class PaladinNpc : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[E] Konuş";

    [Tooltip("Konuşurken oyuncuya dönsün mü.")]
    [SerializeField] private bool faceInteractor = true;

    /// <summary>Oyuncu konuşmak istediğinde tetiklenir. Dinleyicisi diyalog arayüzü olacak.</summary>
    public event System.Action<PaladinNpc, GameObject> DialogueRequested;

    /// <summary>Diyalog paneli açıkken true; bu sırada etkileşim yazısı gizlenir.</summary>
    public bool DialogueOpen { get; private set; }

    private bool uyariVerildi;

    public string GetInteractPrompt()
    {
        // Panel açıkken aynı tuş tekrar konuşma başlatmasın.
        if (DialogueOpen) return string.Empty;

        return promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (DialogueOpen) return;

        if (faceInteractor && interactor != null) OyuncuyaDon(interactor.transform.position);

        DialogueOpen = true;

        if (DialogueRequested != null)
        {
            DialogueRequested(this, interactor);
            return;
        }

        // Arayüz henüz bağlanmadıysa kilitli kalmayalım; sessizce yutmak yerine
        // bir kere uyar, çünkü bu durum eksik kurulumun işaretidir.
        DialogueOpen = false;
        if (!uyariVerildi)
        {
            uyariVerildi = true;
            Debug.LogWarning("PaladinNpc: diyalog arayüzü bağlı değil, konuşma başlatılamadı.", this);
        }
    }

    /// <summary>Diyalog arayüzü konuşmayı kapatınca çağırır.</summary>
    public void CloseDialogue()
    {
        DialogueOpen = false;
    }

    private void OyuncuyaDon(Vector3 hedef)
    {
        Vector3 yon = hedef - transform.position;
        yon.y = 0f;
        if (yon.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(yon);
    }
}
