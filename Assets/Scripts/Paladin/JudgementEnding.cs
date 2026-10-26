using UnityEngine;

/// <summary>
/// Paladin öfkelendi: kılıcını çeker, çıkılacak kapı yoktur, oyun
/// yeniden başlar.
/// </summary>
public class JudgementEnding : NegotiationEnding
{
    [Header("Bu Sona Özel")]
    [Tooltip("Varsa saldırı animasyonunu tetikleyecek animatör.")]
    [SerializeField] private Animator paladinAnimator;

    [SerializeField] private string attackTrigger = "Attack";

    private void Awake()
    {
        if (paladinAnimator == null)
        {
            var npc = FindAnyObjectByType<PaladinNpc>();
            if (npc != null) paladinAnimator = npc.GetComponent<Animator>();
        }
    }

    public override bool IsMet(PaladinNegotiation negotiation)
    {
        return negotiation.Trust <= negotiation.TrustToFail;
    }

    public override void ApplyToWorld()
    {
        if (paladinAnimator == null || string.IsNullOrEmpty(attackTrigger)) return;

        // Parametre yoksa Unity uyarı basıyor; sessizce geç.
        foreach (var p in paladinAnimator.parameters)
            if (p.name == attackTrigger) { paladinAnimator.SetTrigger(attackTrigger); return; }
    }
}
