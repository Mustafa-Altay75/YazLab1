using UnityEngine;

/// <summary>
/// Kapıyı açtıktan sonra birkaç adım geri çekilir.
///
/// Olmadığında iskelet kapı kanadının tam önünde duruyor ve kapı açılırken
/// kanadın içine girmiş gibi görünüyordu. Geri çekilince hem görüntü
/// düzeliyor hem de oyuncu kapıdan geçerken önünü kapatmıyor.
///
/// Geri çekilme yönü sabit değil: kapıya hangi yönden geldiyse o yönün
/// tersine gidiyor. Böylece kapı veya iskelet taşınsa da doğru kalıyor.
/// </summary>
public class StepBackState : ISkeletonState
{
    public string Name => "StepBack";

    private float enteredAt;
    private Vector3 target;
    private bool hasTarget;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        hasTarget = false;

        if (context.Agent == null) return;

        // Sahnede bir dinlenme noktası verilmişse oraya git. Hesaplanan yön
        // kapının hemen dibinde bırakıyordu; belirlenmiş bir nokta hem
        // görünür oluyor hem de Scene view'dan sürüklenerek ayarlanabiliyor.
        if (context.RestPoint != null)
        {
            target = context.RestPoint.position;
            hasTarget = context.SetDestination(target);
            return;
        }

        if (context.Door == null) return;

        Vector3 kapi = context.Door.transform.position;
        Vector3 iskelet = context.Agent.transform.position;

        Vector3 yon = iskelet - kapi;
        yon.y = 0f;

        // Kapının tam üstündeyse geri çekilecek yön belirsiz kalır;
        // o durumda iskeletin baktığı yönün tersi kullanılır.
        if (yon.sqrMagnitude < 0.01f) yon = -context.Agent.transform.forward;

        target = kapi + yon.normalized * context.StepBackDistance;
        hasTarget = context.SetDestination(target);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        // Hedef kurulamadıysa oyalanma, görev zaten bitti.
        if (!hasTarget) return context.Completed;

        // Geri çekilme kritik değil; takılırsa bekletmeden bitir.
        if (Time.time - enteredAt > context.StepBackTimeout) return context.Completed;

        if (!context.HasArrived()) return this;

        return context.Completed;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
