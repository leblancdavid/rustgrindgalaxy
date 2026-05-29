using Godot;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 120.0f;
    [Export] public float JumpVelocity = -260.0f;
    [Export] public float GravityScale = 1.0f;

    public PlayerLoadout? Loadout { get; private set; }

    public void SetLoadout(PlayerLoadout loadout)
    {
        Loadout = loadout;
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale;

        if (!IsOnFloor())
        {
            velocity.Y += gravity * (float)delta;
        }

        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        var inputDirection = Input.GetAxis("ui_left", "ui_right");
        velocity.X = inputDirection * MoveSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }
}
