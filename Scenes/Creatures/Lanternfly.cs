using System;
using Godot;

public partial class Lanternfly : Creature
{
	private Player Player = null;
	private PackedScene ConsumableScene = null;
	private RandomNumberGenerator rng = null;
	private AudioStreamPlayer2D DeathSound = null;
	private int SeedsConsumed = 0;
	public override void _Ready()
	{
		Damage = 8;
		base._Ready();
		DeathSound = GetNode<AudioStreamPlayer2D>("DeathSound");

		ConsumableScene = GD.Load<PackedScene>("res://Scenes/Environment/Plants/SeedConsumable.tscn");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
	}

	public override void Kill()
	{
		int consumableCount = rng.RandiRange(Math.Max(1, SeedsConsumed), 1 + SeedsConsumed);
		Node rootScene = GetTree().CurrentScene;
		do
		{
			Consumable consumable = ConsumableScene.Instantiate<Consumable>();
			consumable.Modulate = Colors.Red;

			consumable.Effect = new PlantEffect
			{

				Duration = 20,
				EnergyMod = 5,
				HealthMod = 2,
				Name = "Lanternfly Kill Consumable",
				StomachSpace = 10,
				StomachTextureSprite = consumable.GetStomachTextureSprite()

			};
			consumable.GlobalPosition = GlobalPosition + new Vector2I(rng.RandiRange(-10, 10), rng.RandiRange(-10, 10));
			consumable.LinearVelocity = Velocity;
			rootScene.CallDeferred("AddNode", consumable);
			consumableCount -= 1;
		} while (consumableCount > 0);

		// TODO: Play particle animation
		DeathSound.Reparent(rootScene);
		DeathSound.Finished += () => DeathSound.QueueFree();
		DeathSound.Play();
		base.Kill();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		OrientByRotation();

	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (Player != null)
		{
			MoveToward(Player.GlobalPosition, delta);
			Sprite.Play();
		}
		else
		{
			Sprite.Stop();
		}
		bool didCollide = MoveAndSlide();
		if (didCollide)
		{
			var collider = GetLastSlideCollision().GetCollider();
			if (collider is Consumable consumable)
			{
				consumable.OnConsume();
				consumable.Reparent(this);
				SeedsConsumed += 1;
				if (consumable.Effect.Name == "Lanternfly Kill Consumable")
				{
					Kill();
				}
				Damage += 2;
				float UpscaleFactor = 1.15f;
				Vector2 Upscale = new(UpscaleFactor, UpscaleFactor);
				Scale *= Upscale;
				LavityLight.Scale *= Upscale;
				Acceleration *= UpscaleFactor;
			}
		}
	}


	internal override void OnBodyEnteredPerceptionArea(Node body)
	{
		if (body is Player seenPlayer)
		{
			Player = seenPlayer;
		}
	}

	internal override void OnBodyExitedPerceptionArea(Node body)
	{
		if (body is Player)
		{
			Player = null;
		}
	}

}
