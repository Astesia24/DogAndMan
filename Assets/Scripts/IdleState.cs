using UnityEngine;

public class IdleState : State
{
    public IdleState(Player player, StateMachine machine) : base(player, machine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (player.rb != null) player.rb.velocity = Vector2.zero;
    }

    public override void FixedUpdate()
    {
        if (player.inputMagnitude >= Player.MoveThreshold)
        {
            machine.ChangeState(player.Move);
            return;
        }

        if (player.rb != null) player.rb.velocity = Vector2.zero;

        base.FixedUpdate();
    }
}
