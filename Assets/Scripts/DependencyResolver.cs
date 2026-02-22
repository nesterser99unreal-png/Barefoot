using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DependencyResolver : LifetimeScope
{
    [SerializeField] private LinesRunnerPlayerMovement _playerMovement;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<EntryPoint>();
        
        builder.RegisterComponent(_playerMovement);
        
        builder.Register<PlayerInputResolver>(Lifetime.Singleton);
        builder.Register<LinesRunnerMovementHandler>(Lifetime.Singleton);
    }
}