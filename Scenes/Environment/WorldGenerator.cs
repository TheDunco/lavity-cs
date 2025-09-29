using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class WorldGenerator : Node2D
{
	// === Island Settings ===
	[Export] public int IslandCount = 5;
	[Export] public float MinRadius = 80f;
	[Export] public float MaxRadius = 200f;
	[Export] public int RadialSegments = 64; // how smooth each island is
	[Export] public float NoiseFrequency = 1.0f;
	[Export] public float NoiseStrength = 0.3f;
	[Export] public Vector2 WorldSize = new(2000, 1200);

	// === Plant Settings ===
	[Export] public PackedScene[] PlantPrefabs;
	[Export] public Vector2I PlantDensity = new(8, 16); // how many plants per island
	[Export] public float PlantJitter = 12f;

	private Vector2 minExtent = Vector2.Inf;
	private Vector2 maxExtent = -Vector2.Inf;

	private RandomNumberGenerator rng = null;
	private FastNoiseLite noise;

	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		Generate();
	}

	public void Generate()
	{
		// Clear previous
		foreach (Node child in GetChildren())
			child.QueueFree();

		noise = new FastNoiseLite
		{
			Seed = (int)rng.Seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = NoiseFrequency
		};

		for (int i = 0; i < IslandCount; i++)
		{
			GenerateIsland();
		}
	}

	private void GenerateIsland()
	{
		float radius = rng.RandfRange(MinRadius, MaxRadius);
		Vector2 center = new(rng.RandfRange(-WorldSize.X, WorldSize.X), rng.RandfRange(-WorldSize.Y, WorldSize.Y));

		float baseRadiusX = radius * rng.RandfRange(0.7f, 1.3f); // random stretch on X
		float baseRadiusY = radius * rng.RandfRange(0.7f, 1.3f); // random stretch on Y


		var surfacePoints = new List<Vector2>();
		for (int j = 0; j < RadialSegments; j++)
		{
			float angle = j * Mathf.Tau / RadialSegments;

			// Elliptical base point
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			Vector2 ellipseBase = new Vector2(dir.X * baseRadiusX, dir.Y * baseRadiusY);

			// Noise deformation
			float deform = 1f;
			deform += noise.GetNoise2D(ellipseBase.X * NoiseFrequency, ellipseBase.Y * NoiseFrequency) * NoiseStrength;
			deform += noise.GetNoise2D(ellipseBase.X * (NoiseFrequency * 2f), ellipseBase.Y * (NoiseFrequency * 2f)) * (NoiseStrength * 0.5f);

			Vector2 point = center + ellipseBase * deform;
			surfacePoints.Add(point);
		}

		foreach (var pt in surfacePoints)
		{
			minExtent = new Vector2(Mathf.Min(minExtent.X, pt.X), Mathf.Min(minExtent.Y, pt.Y));
			maxExtent = new Vector2(Mathf.Max(maxExtent.X, pt.X), Mathf.Max(maxExtent.Y, pt.Y));
		}

		// Build polygon for island fill
		var poly = new List<Vector2>(surfacePoints);
		var islandPoly = new Polygon2D
		{
			Polygon = poly.ToArray(),
			Color = new Color(0.12f, 0.12f, 0.18f, 1f)
		};
		AddChild(islandPoly);

		// Add collision
		var body = new StaticBody2D();
		var collision = new CollisionPolygon2D
		{
			Polygon = poly.ToArray()
		};
		body.AddChild(collision);
		AddChild(body);

		// Add Light Occlusion 
		var occluder = new LightOccluder2D();
		var occPoly = new OccluderPolygon2D
		{
			Polygon = poly.ToArray()
		};
		occluder.Occluder = occPoly;
		AddChild(occluder);

		// === Place Plants ===
		if (PlantPrefabs.Length > 0)
		{
			for (int k = 0; k < rng.RandiRange(PlantDensity.X, PlantDensity.Y); k++)
			{
				// Pick a random surface point
				int idx = rng.RandiRange(0, surfacePoints.Count - 1);
				Vector2 pos = surfacePoints[idx];

				// Normal points outward from island center
				Vector2 normal = (pos - center).Normalized();

				// Apply jitter
				pos += normal * rng.RandfRange(-PlantJitter, PlantJitter);

				// Instantiate plant
				var prefab = PlantPrefabs[rng.RandiRange(0, PlantPrefabs.Length - 1)];
				var plant = prefab.Instantiate<Node2D>();
				plant.Position = pos;
				plant.Rotation = normal.Angle() + Mathf.Pi / 2f; // stem outward
				AddChild(plant);
			}
		}
	}
}
