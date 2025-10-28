using Godot;
using System;
using System.Collections.Generic;

public partial class Island : Node2D
{
	public Vector2 Center { get; private set; }
	public float Radius { get; private set; }
	public ulong Seed { get; private set; }
	public bool IsRendered { get; private set; } = false;
	public bool HasRendered { get; private set; } = false;
	public Vector2I PlantSpawnChanceRange { get; set; } = new(5, 13);

	private Node2D renderInstance;
	private List<Vector2> SurfacePoints = [];

	public Island(Vector2 center, float radius, ulong seed)
	{
		Center = center;
		Radius = radius;
		Seed = seed;
	}

	public void Render(
		Node parent,
		PackedScene[] plantPrefabs,
		FastNoiseLite noise,
		RandomNumberGenerator rng
	)
	{
		if (IsRendered) return;

		try
		{

			renderInstance = new Node2D();
			parent.AddChild(renderInstance);

			int radialSegments = 256;
			float baseRadiusX = Radius * rng.RandfRange(0.7f, 1.3f);
			float baseRadiusY = Radius * rng.RandfRange(0.7f, 1.3f);

			List<Vector2> surfacePoints = [];
			for (int j = 0; j < radialSegments; j++)
			{
				float angle = j * Mathf.Tau / radialSegments;
				Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
				Vector2 ellipseBase = new(dir.X * baseRadiusX, dir.Y * baseRadiusY);

				float deform = 1f;
				deform += noise.GetNoise2D(ellipseBase.X, ellipseBase.Y) * 0.3f;

				Vector2 point = Center + ellipseBase * deform;
				surfacePoints.Add(point);
			}

			// --- Polygon fill ---
			var poly = new Polygon2D
			{
				Polygon = surfacePoints.ToArray(),
				Color = Colors.SandyBrown * new Color(rng.RandfRange(0, 0.7f), rng.RandfRange(0, 0.7f), rng.RandfRange(0, 0.7f), 1f),
				ClipChildren = ClipChildrenMode.AndDraw
			};
			poly.ZIndex = 10;
			renderInstance.AddChild(poly);

			// --- Collision ---
			var body = new StaticBody2D();
			var collision = new CollisionPolygon2D
			{
				Polygon = surfacePoints.ToArray()
			};
			body.AddChild(collision);
			renderInstance.AddChild(body);

			// --- Light Occlusion ---
			var occluder = new LightOccluder2D();
			var occPoly = new OccluderPolygon2D
			{
				Polygon = [.. surfacePoints]
			};
			occluder.Occluder = occPoly;
			renderInstance.AddChild(occluder);

			SurfacePoints = surfacePoints;

			// Spawn plants
			if (plantPrefabs != null && plantPrefabs.Length > 0)
			{
				SpawnPrefabs(plantPrefabs, PlantSpawnChanceRange, rng);
			}

			IsRendered = true;
			HasRendered = true;

		}
		catch (Exception e)
		{
			if (e.Message.Contains("Convex decomposing failed!"))
			{
				GD.PrintErr("Failed to render island");
				return;
			}
		}
	}

	private void SpawnPrefabs(PackedScene[] prefabs, Vector2I chance, RandomNumberGenerator rng, int surfaceOffest = -10)
	{
		int plantCount = rng.RandiRange(chance.X, chance.Y); // density can be adjusted
		for (int k = 0; k < plantCount; k++)
		{
			int idx = rng.RandiRange(0, SurfacePoints.Count - 1);
			Vector2 pos = SurfacePoints[idx];
			Vector2 normal = (pos - Center).Normalized();

			var prefab = prefabs[rng.RandiRange(0, prefabs.Length - 1)];
			var instance = prefab.Instantiate<Node2D>();
			if (surfaceOffest > 0)
			{
				instance.Position = pos + normal * surfaceOffest;
			}
			else
			{

				instance.Position = pos;
			}
			instance.Rotation = normal.Angle() + Mathf.Pi / 2f;
			renderInstance.AddChild(instance);
		}
	}

	public Vector2 GetRandomSurfacePoint(RandomNumberGenerator rng)
	{
		if (SurfacePoints == null || SurfacePoints.Count == 0)
			return Center; // fallback if not rendered yet

		int idx = rng.RandiRange(0, SurfacePoints.Count - 1);
		return SurfacePoints[idx];
	}


	public void Unrender()
	{
		if (!IsRendered) return;
		renderInstance.QueueFree();
		renderInstance = null;
		IsRendered = false;
	}
}
