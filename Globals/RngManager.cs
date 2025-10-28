using System;
using Godot;

public partial class RngManager : Node
{
	public RandomNumberGenerator Rng = new();
	// 111 used for testing
	public ulong Seed = 111;

	public override void _Ready()
	{
		base._Ready();
		SetSeed((ulong)new RandomNumberGenerator().RandfRange(1, Mathf.Inf));
	}

	public void SetSeed(ulong seed)
	{
		Rng.Seed = seed;
	}

}
