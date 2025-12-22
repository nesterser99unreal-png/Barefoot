using System;
using JetBrains.Annotations;

[UsedImplicitly] //Registered in DI Container
public class PlayerInputResolver : IDisposable
{
    private readonly LinesRunnerPlayerMovement _linesRunnerPlayerMovement;
    
    private PlayerInputMap _inputMap;

    public PlayerInputResolver(LinesRunnerPlayerMovement linesRunnerPlayerMovement) => 
        _linesRunnerPlayerMovement = linesRunnerPlayerMovement;

    public void Initialize()
    {
        if (_inputMap != null)
            return;
        
        _inputMap = new PlayerInputMap();
        
        _inputMap.LinesRunner.LeftMove.performed += _linesRunnerPlayerMovement.TurnLeft;
        _inputMap.LinesRunner.RightMove.performed += _linesRunnerPlayerMovement.TurnRight;
        _inputMap.LinesRunner.Jump.performed += _linesRunnerPlayerMovement.Jump;
        
        _inputMap.Enable();
    }

    public void Dispose()
    {
        _inputMap.LinesRunner.LeftMove.performed -= _linesRunnerPlayerMovement.TurnLeft;
        _inputMap.LinesRunner.RightMove.performed -= _linesRunnerPlayerMovement.TurnRight;
        _inputMap.LinesRunner.Jump.performed -= _linesRunnerPlayerMovement.Jump;
        
        _inputMap?.Dispose();
    }
}