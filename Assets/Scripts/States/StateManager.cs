using System.Collections.Generic;
using System.Linq;

public class StateManager
{
    private static readonly List<IState> states = new List<IState>();
    private static IState currentState;
    
    public static void GoToState<T>() where T : IState
    {
        currentState?.Exit();
        var newState = states.FirstOrDefault(x => x is T);
        currentState = newState;
        currentState?.Enter();
    }

    public static void AddState<T>(T state) where T : IState
    {
        states.Add(state);
    }

    // Discards bookkeeping from a prior bootstrap so a fresh Init.Awake() (e.g. after a
    // scene reload) never retains a currentState/states entry from the destroyed scene.
    // Without this, GoToState()'s currentState?.Exit() call touches a state wrapping a
    // destroyed GameObject from the previous scene instance.
    public static void Reset()
    {
        currentState = null;
        states.Clear();
    }
}