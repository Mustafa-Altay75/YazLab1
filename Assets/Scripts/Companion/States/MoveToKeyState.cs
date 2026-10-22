using UnityEngine;

/// <summary>
/// Masanın üstündeki anahtara yürür.
/// Rota kodda tarif edilmez; ajana sadece hedef nokta verilir, yolu
/// NavMesh üzerinde A* algoritması kendisi hesaplar.
/// </summary>
public class MoveToKeyState : ISkeletonState
{
    public string Name => "MoveToKey";

    public void Enter(SkeletonContext context)
    {
        if (context.KeyObject == null)
        {
            // Hedef hiç atanmamışsa yürünecek yer yok.
            return;
        }

        context.SetDestination(context.KeyObject.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.KeyObject == null)
        {
            return context.Blocked;
        }

        // Hiç yol kurulamadıysa boşuna bekleme.
        if (context.PathFailed())
        {
            return context.Blocked;
        }

        if (!context.HasArrived())
        {
            return this;
        }

        // Yolun sonuna gelindi. Yol kısmi çıkmışsa burası hedefin yanı
        // olmayabilir; gerçekten anahtarın yanında mıyız diye ayrıca bak.
        if (!context.IsNear(context.KeyObject.transform.position))
        {
            return context.Blocked;
        }

        return context.PickUpKey;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
