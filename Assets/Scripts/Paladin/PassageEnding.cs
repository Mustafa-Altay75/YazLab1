using UnityEngine;

/// <summary>
/// Paladin ikna oldu: kapı açılır, oyuncu kendi ayağıyla çıkar.
/// </summary>
public class PassageEnding : NegotiationEnding
{
    [Header("Bu Sona Özel")]
    [Tooltip("Açılacak çıkış kapısı.")]
    [SerializeField] private ExitDoor exitDoor;

    private void Awake()
    {
        if (exitDoor == null) exitDoor = FindAnyObjectByType<ExitDoor>();
    }

    public override bool IsMet(PaladinNegotiation negotiation)
    {
        return negotiation.Trust >= negotiation.TrustToPass;
    }

    public override void ApplyToWorld()
    {
        if (exitDoor != null) exitDoor.Open();
    }
}
