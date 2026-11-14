using UnityEngine;

public class StateMachine
{
    public State currentState;

    public void Initialize(State oriState)
    {
        if (oriState == null)
        {
            Debug.LogError("[StateMachine] Initialize 传入了 null");
            return;
        }

        currentState = oriState;
        currentState.Enter();
    }

    public void ChangeState(State newState)
    {
        // 同一个状态重复切换会导致 Exit/Enter 各跑一遍，直接把状态弄乱
        if (newState == null || newState == currentState) return;

        if (currentState != null) currentState.Exit();

        currentState = newState;
        currentState.Enter();
    }

    public void FixedUpdate()
    {
        if (currentState == null) return;   // 还没 Initialize 就跳过，而不是 NRE

        currentState.FixedUpdate();
    }
}
