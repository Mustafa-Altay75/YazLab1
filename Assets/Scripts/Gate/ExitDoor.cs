using UnityEngine;

/// <summary>
/// Zindanın çıkış kapısı. Yalnızca açılır; etkileşimi yoktur.
///
/// Mustafa'nın DoorInteractable'ı buraya uymuyordu: o bileşen oyuncuya
/// "Kapı dışarıdan kilitli! (İskelet açmalı)" yazısını gösteriyor ve
/// burada bu yanlış olurdu — bu kapıyı iskelet değil Paladin açıyor.
/// Yazı yüzünden onun dosyasını değiştirmek yerine bu kapının kendi
/// küçük parçası yazıldı.
///
/// Açma kararı <see cref="PaladinNegotiation"/>'da; burası sadece kanadı
/// döndürüyor.
/// </summary>
public class ExitDoor : MonoBehaviour
{
    [Tooltip("Döndürülecek kanat. Boş bırakılırsa bu objenin kendisi.")]
    [SerializeField] private Transform leaf;

    [SerializeField] private Vector3 openRotation = new Vector3(0f, 100f, 0f);
    [SerializeField] private float openSpeed = 1.8f;

    private Quaternion kapali, acikHedef;
    private bool acik;

    /// <summary>Kapı açıldı mı.</summary>
    public bool IsOpen { get { return acik; } }

    private void Awake()
    {
        if (leaf == null) leaf = transform;
        kapali = leaf.localRotation;
        acikHedef = kapali * Quaternion.Euler(openRotation);
    }

    private void Update()
    {
        if (!acik) return;
        leaf.localRotation = Quaternion.Slerp(leaf.localRotation, acikHedef, Time.deltaTime * openSpeed);
    }

    public void Open()
    {
        if (acik) return;

        acik = true;

        // Kapalıyken yolu kesen collider açılınca çekilsin.
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }
}
