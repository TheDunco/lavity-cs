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
	private PackedScene ConsumableScene = null;
	private RandomNumberGenerator rng = null;
	public override void _Ready()
	{
		base._Ready();
		StatsManager statsManager = GetNode<StatsManager>("/root/StatsManager");
		ConsumableScene = GD.Load<PackedScene>("res://Scenes/Environment/Plants/SeedConsumable.tscn");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		MaxSpeed = 1000;

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
		OrientByRotation();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (Player != null && Player.IsLightOn())
		{
			MoveToward(Player.GlobalPosition, delta);
			Sprite.Animation = "flying";
		}
		else if (Enemies.Count > 0 && ClosestEnemy != null)
		{
			if (IsInstanceValid(ClosestEnemy))
			{
				// Move away from the closest enemy
				MoveAway(ClosestEnemy.GlobalPosition, delta);
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
				MoveToward(ClosestKin.GlobalPosition, delta);
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
		bool didCollide = MoveAndSlide();
		if (didCollide)
		{
			var collider = GetLastSlideCollision().GetCollider();
			if (collider is Lanternfly)
			{
				Kill();
			}
		}
	}


	public override void Kill()
	{
		Consumable consumable = ConsumableScene.Instantiate<Consumable>();
		consumable.Modulate = LavityLight.GetColor();
		Node rootScene = GetTree().CurrentScene;

		consumable.Effect = new PlantEffect
		{

			Duration = 10,
			EnergyMod = 10,
			HealthMod = 3,
			Name = "Firefly Kill Consumable",
			StomachSpace = 20,
			StomachTextureSprite = consumable.GetStomachTextureSprite()
		};
		consumable.GlobalPosition = GlobalPosition + new Vector2I(rng.RandiRange(-10, 10), rng.RandiRange(-10, 10));
		consumable.LinearVelocity = Velocity;
		rootScene.CallDeferred("AddNode", consumable);

		base.Kill();
	}

	internal override void OnBodyEnteredPerceptionArea(Node body)
	{
		if (body is Player seenPlayer)
		{
			Player = seenPlayer;
		}
		else if (body is Firefly seenFirefly && seenFirefly != this)
		{
			Kin.Add(seenFirefly);
		}
		else if (body is Creature creature && creature != this)
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
