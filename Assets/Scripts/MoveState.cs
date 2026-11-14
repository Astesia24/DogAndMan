using UnityEngine;

public class MoveState : State
{
    public MoveState(Player player, StateMachine machine) : base(player, machine)
    {
    }

    public override void FixedUpdate()
    {
        if (player.inputMagnitude < Player.MoveThreshold)
        {
            machine.ChangeState(player.Idle);
            return;
        }

        // direction 已经被 ClampMagnitude 限制到长度 <= 1，直接乘速度即可
        if (player.rb != null) player.rb.velocity = player.direction * player.speed;

        base.FixedUpdate();
    }
}
