using Godot;
using System;
using System.Reflection;

public partial class Player : CharacterBody2D
{
	private int Acceleration = 1000;
	private float AirResistance = 0.0002f;
	private float MaxVelocity = 1500;

	private AnimatedSprite2D Sprite = null;
	private AudioStreamPlayer WingFlapSounds = null;
	private Camera2D Camera = null;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<Camera2D>("Camera");
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

		bool IsInputAdded = false;
		if (Input.IsActionPressed("MoveUp"))
		{
			Velocity += Vector2.Up * Acceleration * (float)delta;
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveLeft"))
		{
			Velocity += Vector2.Left * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveDown"))
		{
			Velocity += Vector2.Down * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveRight"))
		{
			Velocity += Vector2.Right * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsKeyPressed(Key.Space))
		{
			Position = Vector2.Zero;
			Velocity = Vector2.Zero;
		}


		bool isZeroApprox = Velocity.IsZeroApprox();

		if (IsInputAdded)
		{
			Sprite.Play();
			if (!WingFlapSounds.Playing)
			{
				WingFlapSounds.Play();
			}
		}
		else
		{
			Sprite.Stop();
		}

		if (!isZeroApprox && !IsInputAdded)
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

		Velocity += GetGravity();

		MoveAndSlide();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		base._UnhandledKeyInput(@event);

		if (@event.IsActionPressed("ZoomIn"))
		{
			Camera.Zoom += new Vector2(0.1f, 0.1f);
		}

		if (@event.IsActionPressed("ZoomOut"))
		{
			Camera.Zoom -= new Vector2(0.1f, 0.1f);
		}
	}

}
