using Godot;
using System;
using System.Collections.Generic;

public partial class WorldGenerator : Node2D
{
	[Export] public PackedScene[] PlantPrefabs;
	[Export] public Vector2I PlantDensity = new(5, 15);
	[Export] public float PlantJitter = 12f;
	[Export] public float NoiseFrequency = 0.05f;
	[Export] public float NoiseStrength = 0.3f;
	[Export] public int RadialSegments = 256;

	private RandomNumberGenerator rng;
	private FastNoiseLite noise;

	public override void _Ready()
	{
		rng = GetNode<RngManager>("/root/RngManager").Rng;

		noise = new FastNoiseLite
		{
			Seed = (int)rng.Seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = NoiseFrequency
		};
	}

	public void RenderIslandsInArea(Rect2 area, List<(Vector2 center, float radius)> islands)
	{
		GD.Print($"[WorldGenerator] Rendering {islands.Count} islands in {area}");
		foreach (var (center, radius) in islands)
			GenerateIsland(center, radius);
	}

	private void GenerateIsland(Vector2 center, float radius)
	{
		float baseRadiusX = radius * rng.RandfRange(0.7f, 1.3f);
		float baseRadiusY = radius * rng.RandfRange(0.7f, 1.3f);

		var surfacePoints = new List<Vector2>();
		for (int j = 0; j < RadialSegments; j++)
		{
			float angle = j * Mathf.Tau / RadialSegments;
			Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
			Vector2 ellipseBase = new(dir.X * baseRadiusX, dir.Y * baseRadiusY);

			float deform = 1f;
			deform += noise.GetNoise2D(ellipseBase.X * NoiseFrequency, ellipseBase.Y * NoiseFrequency) * NoiseStrength;
			deform += noise.GetNoise2D(ellipseBase.X * (NoiseFrequency * 2f), ellipseBase.Y * (NoiseFrequency * 2f)) * (NoiseStrength * 0.5f);

			Vector2 point = center + ellipseBase * deform;
			surfacePoints.Add(point);
		}

		var poly = new Polygon2D
		{
			Polygon = [.. surfacePoints],
			Color = new Color(0.12f, 0.12f, 0.18f, 1f)
		};
		AddChild(poly);

		var body = new StaticBody2D();
		var collision = new CollisionPolygon2D
		{
			Polygon = [.. surfacePoints]
		};
		body.AddChild(collision);
		AddChild(body);

		var occluder = new LightOccluder2D();
		var occPoly = new OccluderPolygon2D
		{
			Polygon = [.. surfacePoints]
		};
		occluder.Occluder = occPoly;
		AddChild(occluder);

		if (PlantPrefabs.Length > 0)
		{
			for (int k = 0; k < rng.RandiRange(PlantDensity.X, PlantDensity.Y); k++)
			{
				int idx = rng.RandiRange(0, surfacePoints.Count - 1);
				Vector2 pos = surfacePoints[idx];
				Vector2 normal = (pos - center).Normalized();
				pos += normal * rng.RandfRange(-PlantJitter, PlantJitter);

				var prefab = PlantPrefabs[rng.RandiRange(0, PlantPrefabs.Length - 1)];
				var plant = prefab.Instantiate<Node2D>();
				plant.Position = pos;
				plant.Rotation = normal.Angle() + Mathf.Pi / 2f;
				AddChild(plant);
			}
		}
	}
}
