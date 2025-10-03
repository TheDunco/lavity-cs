using Godot;
using System;
using System.Collections.Generic;

public partial class WorldManager : Node2D
{
	[Export] public int WorldWidth = 100000;
	[Export] public int WorldHeight = 100000;
	[Export] public int IslandCount = 5000;
	[Export] public float MinRadius = 50f;
	[Export] public float MaxRadius = 1000f;
	[Export] public float RenderDistance = 4000f;

	[Export] public PackedScene[] PlantPrefabs;

	private List<Island> islands = new();
	private Node2D player;
	private FastNoiseLite noise;
	private RandomNumberGenerator rng;

	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		player = GetNode<Node2D>("Player");

		// Setup noise
		noise = new FastNoiseLite
		{
			Seed = (int)rng.Seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 0.05f
		};

		GenerateWorldMetadata();
	}

	private void GenerateWorldMetadata()
	{
		islands.Clear();
		for (int i = 0; i < IslandCount; i++)
		{
			Vector2 pos = new(
				rng.RandfRange(-WorldWidth / 2, WorldWidth / 2),
				rng.RandfRange(-WorldHeight / 2, WorldHeight / 2)
			);
			float radius = rng.RandfRange(MinRadius, MaxRadius);
			ulong seed = rng.Randi();

			islands.Add(new Island(pos, radius, seed));
		}

		GD.Print($"Generated {islands.Count} islands for the world.");
	}

	public override void _Process(double delta)
	{
		Vector2 playerPos = player.GlobalPosition;

		foreach (var island in islands)
		{
			float dist = playerPos.DistanceTo(island.Center);

			if (dist < RenderDistance)
			{
				island.Render(this, PlantPrefabs, noise, rng);
			}
			else
			{
				island.Unrender();
			}
		}
	}
}
