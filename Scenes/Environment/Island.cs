using Godot;
using System;
using System.Collections.Generic;

public class Island
{
	public Vector2 Center { get; private set; }
	public float Radius { get; private set; }
	public ulong Seed { get; private set; }
	public bool IsRendered { get; private set; } = false;

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
				Color = new Color(0.12f, 0.12f, 0.18f, 1f),
				ClipChildren = CanvasItem.ClipChildrenMode.AndDraw
			};
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

			// --- Plants ---
			if (plantPrefabs != null && plantPrefabs.Length > 0)
			{
				int plantCount = rng.RandiRange(5, 15); // density can be adjusted
				for (int k = 0; k < plantCount; k++)
				{
					int idx = rng.RandiRange(0, surfacePoints.Count - 1);
					Vector2 pos = surfacePoints[idx];
					Vector2 normal = (pos - Center).Normalized();

					var prefab = plantPrefabs[rng.RandiRange(0, plantPrefabs.Length - 1)];
					var plant = prefab.Instantiate<Node2D>();
					if (plant is Lanternfly lanternfly)
					{
						lanternfly.Position = pos + normal * 50;
					}
					else
					{

						plant.Position = pos;
					}
					plant.Rotation = normal.Angle() + Mathf.Pi / 2f;
					renderInstance.AddChild(plant);
				}
			}

			IsRendered = true;
			SurfacePoints = surfacePoints;

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
