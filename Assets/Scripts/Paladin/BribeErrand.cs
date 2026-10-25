using UnityEngine;

/// <summary>
/// Paladin rüşvet istediğinde dünyada neyin değişeceğini yürütür.
///
/// Karar motoru yalnızca "rüşvet istendi" diye haber veriyor; kapıyı
/// açmak, iskeletin görevini açmak gibi sahneye dokunan işler burada.
/// Ayrı durmasının sebebi, PaladinNegotiation'ın saf karar mantığı olarak
/// kalması: o sınıfın sahnede ne olduğundan haberi olmamalı.
///
/// Kapı burada <b>kalıcı olarak</b> açılıyor. Zorunlu: kapıyı plakada
/// duran iskelet açık tutuyordu, altınları toplamaya gidince plakadan
/// inecek ve oyuncu Paladin'in odasında kilitli kalacaktı.
/// </summary>
public class BribeErrand : MonoBehaviour
{
    [SerializeField] private PaladinNegotiation negotiation;
    [SerializeField] private SkeletonCompanion companion;
    [SerializeField] private PlateGate gate;

    [SerializeField] private bool logChanges = true;

    private void Awake()
    {
        if (negotiation == null) negotiation = FindAnyObjectByType<PaladinNegotiation>();
        if (companion == null) companion = FindAnyObjectByType<SkeletonCompanion>();
        if (gate == null) gate = FindAnyObjectByType<PlateGate>();

        if (negotiation != null) negotiation.BribeDemanded += RusvetIstendi;
    }

    private void OnDestroy()
    {
        if (negotiation != null) negotiation.BribeDemanded -= RusvetIstendi;
    }

    private void RusvetIstendi(int adet)
    {
        if (gate != null) gate.HoldOpen();
        if (companion != null) companion.UnlockCoinTask();

        if (logChanges)
            Debug.Log("Rüşvet istendi: kapı açık tutuluyor, iskeletin altın görevi açıldı (" + adet + ").", this);
    }
}
