/// <summary>
/// Görev bitti. İskelet kapının yanında bekler, başka bir şey yapmaz.
/// Bu durumdan çıkış yoktur.
/// </summary>
public class CompletedState : ISkeletonState
{
    public string Name => "Completed";

    public void Enter(SkeletonContext context)
    {
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        return this;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
