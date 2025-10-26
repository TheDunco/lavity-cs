using Godot;
using System;

public abstract partial class Creature : CharacterBody2D
{
	[Export] internal int BaseAcceleration = 5;
	internal float Acceleration;
	public int Damage = 2;
	public override void _Ready()
	{
		base._Ready();
		Acceleration = BaseAcceleration;
	}

	internal void MoveToward(Vector2 pos)
	{
		LookAt(pos);
		Velocity += GlobalPosition.DirectionTo(pos) * (Acceleration + (GlobalPosition.DistanceTo(pos) * (1 / 100)));
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
}
