using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private float MAX_SPEED = 200f;
	private float ACCELERATION = 500f;
	private float DECELERATION = 100f;
	
	private Vector2 velocity = Vector2.Zero;
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Vector2.Zero;

		if (Input.IsActionPressed("ui_right"))
			direction.X += 1;
		if (Input.IsActionPressed("ui_left"))
			direction.X -= 1;
		if (Input.IsActionPressed("ui_up"))
			direction.Y -= 1;
		if (Input.IsActionPressed("ui_down"))
			direction.Y += 1;

		// Normalize direction to avoid diagonal speed boost
		if (direction.Length() > 0)
			direction = direction.Normalized();

		// Calculate desired velocity
		Vector2 desiredVelocity = direction * MAX_SPEED;

		// Calculate velocity change
		Vector2 velocityChange = desiredVelocity - velocity;

		// Apply acceleration
		if (direction.Length() > 0)
			velocityChange = velocityChange.Clamp(-1.0f * ACCELERATION * (float)delta, ACCELERATION * (float)delta);
		else
			velocityChange = velocityChange.Clamp(-1.0f * DECELERATION * (float)delta, DECELERATION * (float)delta);

		// Update velocity
		velocity += velocityChange;

		Velocity = velocity;
		// Move the character
		MoveAndSlide();
	}
}
