using Godot;
using System;

public abstract partial class Creature : CharacterBody2D
{
	public int Damage = 2;
	internal Area2D PerceptionArea = null;
	internal AnimatedSprite2D Sprite = null;
	internal LavityLight LavityLight = null;
	[Export] internal int BaseAcceleration = 1000;
	internal float Acceleration;
	internal float AirResistance = 100;
	internal float MaxSpeed = 2000;
	public override void _Ready()
	{
		base._Ready();
		PerceptionArea = GetNode<Area2D>("PerceptionArea");
		LavityLight = GetNode<LavityLight>("LavityLight");
		Sprite = GetNode<AnimatedSprite2D>("Sprite");

		Sprite.Play();

		if (PerceptionArea != null)
		{
			PerceptionArea.BodyEntered += OnBodyEnteredPerceptionArea;
			PerceptionArea.BodyExited += OnBodyExitedPerceptionArea;
		}

		Acceleration = BaseAcceleration;
	}

	internal void MoveToward(Vector2 pos, double delta)
	{
		LookAt(pos);
		Velocity = Velocity.MoveToward(GlobalPosition.DirectionTo(pos) * MaxSpeed, Acceleration * (float)delta + (GlobalPosition.DistanceTo(pos) * (1 / 100)));
	}

	internal void MoveAway(Vector2 pos, double delta)
	{
		Vector2 awayDir = (GlobalPosition - pos).Normalized();
		LookAt(GlobalPosition + awayDir);
		Velocity = Velocity.MoveToward(
			awayDir * MaxSpeed,
			Acceleration * (float)delta + (GlobalPosition.DistanceTo(pos) * 0.01f)
		);
	}

	public void Reparent(Node node)
	{
		base.Reparent(node);
	}

	internal void OrientByRotation()
	{
		var CurScaleY = Scale.Y;
		double rotationCos = Math.Cos(GlobalRotation);
		if (rotationCos < 0 && CurScaleY > 0)
		{

			Scale = new Vector2(Scale.X, -Scale.Y);
		}
		else if (rotationCos > 0 && CurScaleY < 0)
		{

			Scale = new Vector2(Scale.X, Math.Abs(Scale.Y));
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Velocity = Velocity.Clamp(-MaxSpeed, MaxSpeed);
		Velocity += GetGravity() * (float)delta * 1000;
	}


	internal virtual void OnBodyEnteredPerceptionArea(Node body)
	{
	}

	internal virtual void OnBodyExitedPerceptionArea(Node body)
	{
	}

	public virtual void Kill()
	{
		QueueFree();
	}
}
