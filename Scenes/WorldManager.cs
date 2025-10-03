using Godot;
using System;
using System.Collections.Generic;

public partial class WorldManager : Node2D
{
	[Export] public int WorldWidth = 100000;
	[Export] public int WorldHeight = 100000;
	[Export] public int IslandCount = 6000;
	[Export] public float MinRadius = 150;
	[Export] public float MaxRadius = 1000;
	[Export] public float RenderDistance = 7000f;
	[Export] public float IslandPadding = 200f;
	[Export] public PackedScene[] PlantPrefabs;

	private List<Island> Islands = [];
	private Node2D player;
	[Export] public FastNoiseLite noise;
	private RandomNumberGenerator rng;

	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		player = GetNode<Node2D>("Player");

		GenerateIslands();

		Island spawnIsland = Islands[0]; // or pick largest/random

		// TODO: Move the PlantPrefabs to be a per-island concept
		spawnIsland.Render(this, PlantPrefabs, noise, rng);
		Vector2 edge = spawnIsland.GetRandomSurfacePoint(rng);
		Vector2 normal = (edge - spawnIsland.Center).Normalized();
		Vector2 spawnPoint = edge + normal * 100f; // offset away from island
		player.Position = spawnPoint;
	}

	public void GenerateIslands()
	{
		int attempts = 0;
		while (Islands.Count < IslandCount && attempts < IslandCount * 20) // prevent infinite loop
		{
			attempts++;

			Vector2 center = new(
				rng.RandfRange(-WorldWidth, WorldWidth),
				rng.RandfRange(-WorldHeight, WorldHeight)
			);

			float radius = rng.RandfRange(MinRadius, MaxRadius); // tweak min/max radius

			// check against existing islands
			bool overlaps = false;
			foreach (var other in Islands)
			{
				float minDist = radius + other.Radius + IslandPadding;
				if (center.DistanceTo(other.Center) < minDist)
				{
					overlaps = true;
					break;
				}
			}

			if (overlaps) continue;

			Islands.Add(new Island(center, radius, (ulong)rng.Randi()));
		}

		GD.Print($"Placed {Islands.Count} islands after {attempts} attempts.");
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
