using Godot;

public partial class RngManager : Node
{
	public RandomNumberGenerator Rng = new();
	public ulong Seed = 111;

	public override void _Ready()
	{
		base._Ready();
		Rng.Seed = Seed;
	}

	public void SetSeed(ulong seed)
	{
		Rng.Seed = seed;
	}

}
