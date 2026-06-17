using Godot;

public partial class SlashEffect : AnimatedSprite2D
{
    public override void _Ready()
    {
        Connect(AnimationPlayer.SignalName.AnimationFinished, Callable.From(() => QueueFree()));
        Play();
    }

    public void SetFacingRight(bool facingRight)
    {
        Scale = new Vector2(facingRight ? 1 : -1, 1);
    }
}
