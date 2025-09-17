using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] private int Acceleration = 10;
	private double Speed = 0;

	public override void _Ready()
	{
		GD.Print("Ready");
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.W))
		{
			Speed += Acceleration * delta * 0.5;
			Position += Vector2.Right * (float)(Speed * delta);
			Speed += Acceleration * delta * 0.5;
		}
	}
}
