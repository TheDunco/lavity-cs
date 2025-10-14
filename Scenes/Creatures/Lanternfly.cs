using Godot;

public partial class Lanternfly : Creature
{
	private Area2D PerceptionArea = null;
	private Player Player = null;
	private AnimatedSprite2D Sprite = null;
	private PackedScene ConsumableScene = null;
	private RandomNumberGenerator rng = null;
	private AudioStreamPlayer2D DeathSound = null;
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

		ConsumableScene = GD.Load<PackedScene>("res://Scenes/Environment/Plants/SeedConsumable.tscn");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
	}

	public void Kill()
	{
		int consumableCount = rng.RandiRange(1, 4);
		Node rootScene = GetTree().CurrentScene;
		do
		{
			Consumable consumable = ConsumableScene.Instantiate<Consumable>();

			consumable.Effect = new PlantEffect
			{

				Duration = 20,
				EnergyMod = 4,
				HealthMod = 2,
				Name = "Lanternfly Consumable",
				StomachSpace = 10,
				StomachTextureSprite = consumable.GetStomachTextureSprite()

			};
			consumable.GlobalPosition = GlobalPosition + new Vector2I(rng.RandiRange(-10, 10), rng.RandiRange(-10, 10));
			consumable.LinearVelocity = Velocity;
			rootScene.CallDeferred("AddNode", consumable);
			consumableCount -= 1;
		} while (consumableCount > 0);

		// TODO: Play particle animation
		// TODO: Play sound
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
