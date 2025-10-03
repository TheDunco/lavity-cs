using Godot;
using System;
using System.Collections.Generic;

public partial class WorldManager : Node2D
{
	[Export] public int WorldWidth = 100000;
	[Export] public int WorldHeight = 100000;
	[Export] public int IslandCount = 5000;
	[Export] public float MinRadius = 100;
	[Export] public float MaxRadius = 500;
	[Export] public float RenderDistance = 7000f;
	[Export] public float IslandPadding = 100f;
	[Export] public PackedScene[] PlantPrefabs;

	private List<Island> Islands = new();
	private Node2D player;
	[Export] public FastNoiseLite noise;
	private RandomNumberGenerator rng;

	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		player = GetNode<Node2D>("Player");

		// Setup noise
		noise.Seed = (int)rng.Seed;

		Islands = GenerateIslands();
	}

	public List<Island> GenerateIslands()
	{
		var islands = new List<Island>();

		int attempts = 0;
		while (islands.Count < IslandCount && attempts < IslandCount * 20) // prevent infinite loop
		{
			attempts++;

			Vector2 center = new Vector2(
				rng.RandfRange(-WorldWidth, WorldWidth),
				rng.RandfRange(-WorldHeight, WorldHeight)
			);

			float radius = rng.RandfRange(MinRadius, MaxRadius); // tweak min/max radius

			// check against existing islands
			bool overlaps = false;
			foreach (var other in islands)
			{
				float minDist = radius + other.Radius + IslandPadding;
				if (center.DistanceTo(other.Center) < minDist)
				{
					overlaps = true;
					break;
				}
			}

			if (overlaps) continue;

			islands.Add(new Island(center, radius, (ulong)rng.Randi()));
		}

		GD.Print($"Placed {islands.Count} islands after {attempts} attempts.");
		return islands;
	}

	public override void _Process(double delta)
	{
		Vector2 playerPos = player.GlobalPosition;

		foreach (var island in Islands)
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
