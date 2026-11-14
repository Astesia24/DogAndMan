using UnityEngine;

public class State
{
    protected Player player;
    protected StateMachine machine;

    public State(Player player, StateMachine machine)
    {
        this.player = player;
        this.machine = machine;

        if (player == null || machine == null)
        {
            Debug.LogError("[State] 构造时 player 或 machine 为 null");
        }
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void FixedUpdate()
    {
    }
}
