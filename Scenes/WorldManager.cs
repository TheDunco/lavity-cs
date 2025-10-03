using Godot;
using System;
using System.Collections.Generic;

public partial class WorldManager : Node2D
{
	[Export] public PackedScene WorldGeneratorScene;
	[Export] public int ChunkRadius = 2; // how many chunks out from center
	[Export] public Vector2 ChunkSize = new(4000, 4000);
	[Export] public int MaxIslandCount = 10;
	[Export] public float MinRadius = 150f;
	[Export] public float MaxRadius = 1500f;
	[Export] public float IslandPadding = 100f;

	private Node2D player;
	private Dictionary<Vector2I, WorldGenerator> activeChunks = new();
	private Vector2I currentCenterChunk;

	private RandomNumberGenerator rng;
	private Queue<(Vector2I coords, List<(Vector2, float)> islands)> generationQueue = new();


	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;

		player = GetNode<Node2D>("Player"); // adjust path as needed

		UpdateChunks(new(0, 0));

		// GD.Print($"Global islands: {islands.Count}");

	}

	public override void _Process(double delta)
	{
		if (generationQueue.Count > 0)
		{
			var (coords, islands) = generationQueue.Dequeue();

			var chunk = WorldGeneratorScene.Instantiate<WorldGenerator>();
			AddChild(chunk);

			Vector2 worldOrigin = new(coords.X * ChunkSize.X, coords.Y * ChunkSize.Y);
			chunk.Position = worldOrigin;

			chunk.RenderIslandsInArea(new Rect2(worldOrigin, ChunkSize), islands);
			activeChunks[coords] = chunk;
		}

		Vector2I playerChunk = WorldToChunkCoords(player.GlobalPosition);
		if (playerChunk != currentCenterChunk)
		{
			currentCenterChunk = playerChunk;
			UpdateChunks(playerChunk);
		}
	}

	private List<(Vector2 center, float radius)> GenerateIslandsForChunk(Vector2I coords)
	{
		var localIslands = new List<(Vector2 center, float radius)>();

		// Derive seed from global seed and chunk coords
		ulong chunkSeed = (ulong)(coords.X * 73856093 ^ coords.Y * 19349663) + rng.Seed;
		var localRng = new RandomNumberGenerator { Seed = chunkSeed };

		int islandCount = localRng.RandiRange(1, MaxIslandCount); // tweak for density

		for (int i = 0; i < islandCount; i++)
		{
			float radius = localRng.RandfRange(MinRadius, MaxRadius);

			Vector2 chunkOrigin = new(coords.X * ChunkSize.X, coords.Y * ChunkSize.Y);
			Vector2 center = chunkOrigin + new Vector2(
				localRng.RandfRange(0, ChunkSize.X),
				localRng.RandfRange(0, ChunkSize.Y)
			);

			// Optional: enforce padding to avoid overlap
			bool valid = true;
			foreach (var (c, r) in localIslands)
			{
				if (center.DistanceTo(c) < radius + r + IslandPadding)
				{
					valid = false;
					break;
				}
			}

			if (valid)
				localIslands.Add((center, radius));
		}
		GD.Print($"Chunk {coords}: generated {localIslands.Count} islands");


		return localIslands;
	}


	private void UpdateChunks(Vector2I playerChunk)
	{
		var needed = new HashSet<Vector2I>();

		GD.Print($"PlayerChunk={playerChunk}, Active={activeChunks.Count}, Needed={needed.Count}");


		for (int x = -ChunkRadius; x <= ChunkRadius; x++)
		{
			for (int y = -ChunkRadius; y <= ChunkRadius; y++)
			{
				Vector2I coords = new(currentCenterChunk.X + x, currentCenterChunk.Y + y);
				needed.Add(coords);

				if (!activeChunks.ContainsKey(coords))
					SpawnChunk(coords);
			}
		}

		var toRemove = new List<Vector2I>();
		foreach (var kvp in activeChunks)
		{
			if (!needed.Contains(kvp.Key))
			{
				kvp.Value.QueueFree();
				toRemove.Add(kvp.Key);
			}
		}
		foreach (var coords in toRemove)
			activeChunks.Remove(coords);
	}

	private void SpawnChunk(Vector2I coords)
	{
		var localIslands = GenerateIslandsForChunk(coords);
		generationQueue.Enqueue((coords, localIslands));
	}


	private Vector2I WorldToChunkCoords(Vector2 pos)
	{
		int cx = Mathf.FloorToInt(pos.X / ChunkSize.X);
		int cy = Mathf.FloorToInt(pos.Y / ChunkSize.Y);
		return new Vector2I(cx, cy);
	}
}
