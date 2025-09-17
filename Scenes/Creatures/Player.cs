using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] private int Acceleration = 700;
	[Export] private float AirResistance = 0.0002f;
	[Export] private float MaxVelocity = 1000;

	private Sprite2D Sprite = null;

	public override void _Ready()
	{
		GD.Print("Ready");
		Sprite = GetNode<Sprite2D>("Sprite");
	}


	private void OrientByRotation()
	{

		var CurScaleY = Scale.Y;
		double rotationCos = Math.Cos(Rotation);
		if (rotationCos < 0 && CurScaleY > 0)
		{

			Scale = new Vector2(Scale.X, -Scale.Y);
		}
		else if (rotationCos > 0 && CurScaleY < 0)
		{

			Scale = new Vector2(Scale.X, Math.Abs(Scale.Y));
		}
	}

	public override void _Process(double delta)
	{

		bool InputAdded = false;
		if (Input.IsActionPressed("MoveUp"))
		{
			Velocity += Vector2.Up * Acceleration * (float)delta;
			InputAdded = true;
		}

		if (Input.IsActionPressed("MoveLeft"))
		{
			Velocity += Vector2.Left * Acceleration * (float)(delta);
			InputAdded = true;
		}

		if (Input.IsActionPressed("MoveDown"))
		{
			Velocity += Vector2.Down * Acceleration * (float)(delta);
			InputAdded = true;
		}

		if (Input.IsActionPressed("MoveRight"))
		{
			Velocity += Vector2.Right * Acceleration * (float)(delta);
			InputAdded = true;
		}

		if (Input.IsKeyPressed(Key.Space))
		{
			Position = Vector2.Zero;
		}

		bool isZeroApprox = Velocity.IsZeroApprox();

		if (!isZeroApprox && !InputAdded)
		{
			Velocity += -Velocity * AirResistance;
		}
		else if (isZeroApprox)
		{
			Velocity = Vector2.Zero;
		}

		Velocity = Velocity.Clamp(-MaxVelocity, MaxVelocity);

		LookAt(Velocity.Normalized() + Position);
		OrientByRotation();

		MoveAndSlide();
	}
}
