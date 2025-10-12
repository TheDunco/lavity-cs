using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Lanternfly : Creature
{
	private Area2D PerceptionArea = null;
	private Player Player = null;
	private AnimatedSprite2D Sprite = null;
	public override void _Ready()
	{
		BaseAcceleration = 4;
		base._Ready();
		PerceptionArea = GetNode<Area2D>("PerceptionArea");
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		PerceptionArea.BodyEntered += this.OnBodyEnteredPerceptionArea;
		PerceptionArea.BodyExited += this.OnBodyExitedPerceptionArea;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Player != null)
		{
			MoveToward(Player.GlobalPosition);
			Sprite.Play();

		}
		else
		{
			Sprite.Stop();
		}
		OrientByRotation();
		MoveAndSlide();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

	}


	private void OnBodyEnteredPerceptionArea(Node2D body)
	{
		if (body is Player seenPlayer)
		{
			Player = seenPlayer;
		}
	}

	private void OnBodyExitedPerceptionArea(Node2D body)
	{
		if (body is Player)
		{
			Player = null;
		}
	}

}
