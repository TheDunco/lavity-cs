using System;
using Godot;

public partial class Lanternfly : Creature
{
	private Area2D PerceptionArea = null;
	private Player Player = null;
	private AnimatedSprite2D Sprite = null;
	private PackedScene ConsumableScene = null;
	private RandomNumberGenerator rng = null;
	private AudioStreamPlayer2D DeathSound = null;
	private LavityLight lavityLight = null;
	private int SeedsConsumed = 0;
	public override void _Ready()
	{
		BaseAcceleration = 4;
		Damage = 8;
		base._Ready();
		PerceptionArea = GetNode<Area2D>("PerceptionArea");
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		PerceptionArea.BodyEntered += OnBodyEnteredPerceptionArea;
		PerceptionArea.BodyExited += OnBodyExitedPerceptionArea;
		DeathSound = GetNode<AudioStreamPlayer2D>("DeathSound");
		lavityLight = GetNode<LavityLight>("LavityLight");

		ConsumableScene = GD.Load<PackedScene>("res://Scenes/Environment/Plants/SeedConsumable.tscn");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
	}

	public void Kill()
	{
		int consumableCount = rng.RandiRange(Math.Max(1, SeedsConsumed), 4 + SeedsConsumed);
		Node rootScene = GetTree().CurrentScene;
		do
		{
			Consumable consumable = ConsumableScene.Instantiate<Consumable>();

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
		DeathSound.Reparent(GetTree().CurrentScene);
		DeathSound.Finished += () => DeathSound.QueueFree();
		DeathSound.Play();
		QueueFree();
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
		bool didCollide = MoveAndSlide();
		if (didCollide)
		{
			var collision = this.GetLastSlideCollision();
			var collider = collision.GetCollider();
			if (collider is Consumable consumable)
			{
				consumable.OnConsume();
				consumable.Reparent(this);
				Damage += 2;
				Vector2 Upscale = new(1.2f, 1.2f);
				Scale *= Upscale;
				lavityLight.Scale *= Upscale;
				Acceleration *= 1.2f;
			}
		}
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
