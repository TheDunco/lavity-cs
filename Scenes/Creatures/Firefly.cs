using Godot;
using System;
using System.Collections.Generic;

public partial class Firefly : Creature
{
	private Player Player = null;
	private readonly List<Firefly> Kin = [];
	private readonly List<Creature> Enemies = [];
	private Firefly ClosestKin = null;
	private Creature ClosestEnemy = null;
	public override void _Ready()
	{
		base._Ready();
		StatsManager statsManager = GetNode<StatsManager>("/root/StatsManager");

		// Calculate the closest on StatsTick for performance
		statsManager.StatsTick += () =>
		{
			if (ClosestKin == null && Kin.Count > 0 && IsInstanceValid(Kin[0]))
			{
				ClosestKin = Kin[0];
			}
			else if (Kin.Count == 0 || !IsInstanceValid(ClosestKin))
			{
				return;
			}
			foreach (Firefly firefly in Kin)
			{
				if (!IsInstanceValid(firefly))
				{
					Kin.Remove(firefly);
					continue;
				}
				var distanceToClosestKin = GlobalPosition.DistanceTo(ClosestKin.GlobalPosition);
				if (GlobalPosition.DistanceTo(firefly.GlobalPosition) < distanceToClosestKin)
				{
					ClosestKin = firefly;
				}
			}
		};

		statsManager.StatsTick += () =>
		{
			if (ClosestEnemy == null && Enemies.Count > 0 && IsInstanceValid(Enemies[0]))
			{
				ClosestEnemy = Enemies[0];
			}
			else if (Enemies.Count == 0 || !IsInstanceValid(ClosestEnemy))
			{
				return;
			}
			foreach (Creature creature in Enemies)
			{
				if (!IsInstanceValid(creature))
				{
					Enemies.Remove(creature);
					continue;
				}
				var distanceToClosestEnemy = GlobalPosition.DistanceTo(ClosestEnemy.GlobalPosition);
				if (GlobalPosition.DistanceTo(creature.GlobalPosition) < distanceToClosestEnemy)
				{
					ClosestEnemy = creature;
				}
			}
		};
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Player != null && Player.IsLightOn())
		{
			MoveToward(Player.GlobalPosition);
			Sprite.Animation = "flying";
		}
		else if (Enemies.Count > 0 && ClosestEnemy != null)
		{
			if (IsInstanceValid(ClosestEnemy))
			{
				// Move away from the closest enemy
				MoveToward(-ClosestEnemy.GlobalPosition);
				Sprite.Animation = "flying";

			}
			else
			{
				ClosestEnemy = null;
			}
		}
		else if (Kin.Count > 0 && ClosestKin != null)
		{
			if (IsInstanceValid(ClosestKin))
			{
				MoveToward(ClosestKin.GlobalPosition);
				Sprite.Animation = "flying";

			}
			else
			{
				ClosestKin = null;
			}
		}
		else
		{
			Sprite.Animation = "idle";
		}
		OrientByRotation();
		MoveAndSlide();
	}


	internal override void OnBodyEnteredPerceptionArea(Node body)
	{
		if (body is Player seenPlayer)
		{
			Player = seenPlayer;
		}
		else if (body is Firefly seenFirefly)
		{
			Kin.Add(seenFirefly);
		}
		else if (body is Creature creature)
		{
			Enemies.Add(creature);
		}
	}

	internal override void OnBodyExitedPerceptionArea(Node body)
	{
		if (body is Player)
		{
			Player = null;
		}
		else if (body is Firefly firefly)
		{
			Kin.Remove(firefly);
		}
		else if (body is Creature creature)
		{
			Enemies.Remove(creature);
		}
	}


}
