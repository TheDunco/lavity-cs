using Godot;

public partial class Plant : Node2D
{
	public override void _Ready()
	{
		base._Ready();

		SpawnManager spawnManager = GetNode<SpawnManager>("/root/SpawnManager");
		spawnManager.SpawnTick += OnSpawnTick;

	}

	public virtual void OnSpawnTick()
	{
	}
}
