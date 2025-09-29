using Godot;

public partial class Plant : Node2D
{

	internal int ConsumableSpawnChance = 1;
	internal int MaxConsumables = 3;
	internal RandomNumberGenerator rng = null;
	public override void _Ready()
	{
		base._Ready();

		rng = GetNode<RngManager>("/root/RngManager").Rng;

		SpawnManager spawnManager = GetNode<SpawnManager>("/root/SpawnManager");
		spawnManager.SpawnTick += OnSpawnTick;

	}

	public virtual void OnSpawnTick()
	{
	}
}
