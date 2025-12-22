using JetBrains.Annotations;
using VContainer.Unity;

[UsedImplicitly] //Registered in DI Container
public class EntryPoint : IStartable
{
    private readonly PlayerInputResolver _playerInputResolver;

    public EntryPoint(PlayerInputResolver playerInputResolver) => 
        _playerInputResolver = playerInputResolver;

    public void Start()
    {
        _playerInputResolver.Initialize();
    }
}