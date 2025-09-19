using Godot;
using System;

// TODO: Somehow make the tilemap occlude gravity the same way it's occluding light -- may not be possible by default

// https://gameidea.org/2025/01/20/procedural-cave-generation-in-godot-2d/#Project_Setup
public partial class CaveTilemap : TileMapLayer
{
	// Configuration parameters
	[Export] public int FillPercent { get; set; } = 45; // Initial random fill percentage
	[Export] public int SmoothingIterations { get; set; } = 4; // How many times to smooth the cave
	[Export] public int MinCaveSize { get; set; } = 50; // Minimum size of cave regions to keep
	[Export] public int RoomRadius { get; set; } = 4; // Radius of open spaces around start/end

	// Map settings
	[Export] public int MapWidth { get; set; } = 100;
	[Export] public int MapHeight { get; set; } = 100;
	[Export] public int MaxGenerationAttempts { get; set; } = 16; // Maximum attempts to generate a valid map

	[Export] public bool ShouldBeClosed { get; set; } = true;

	// Tile IDs
	public const int WallTile = 0;
	public const int Empty = -1;

	private int[,] map; // 2D array to store our map data
	private Vector2I startPoint;
	private Vector2I endPoint;

	public override void _Ready()
	{
		base._Ready();
		startPoint = new(0, 0);
		endPoint = new(100, 100);
		GenerateLevel(startPoint, endPoint);
	}

	public void GenerateLevel(Vector2I start, Vector2I end)
	{
		startPoint = start;
		endPoint = end;

		int attempt = 0;
		bool validMap = false;

		while (!validMap && attempt < MaxGenerationAttempts)
		{
			attempt++;

			// Clear existing tiles
			Clear();
			map = new int[MapWidth, MapHeight];

			// Generate a new map
			InitializeMap(attempt); // Pass attempt as seed

			// Smooth the map multiple times using cellular automata
			for (int i = 0; i < SmoothingIterations; i++)
			{
				SmoothMap();
			}

			// Create rooms at start and end points
			CreateRoom(startPoint, RoomRadius);
			CreateRoom(endPoint, RoomRadius);

			// Validate path existence
			validMap = CheckPathExists();
		}

		if (!validMap)
		{
			GD.PushError($"Failed to generate a valid map after {MaxGenerationAttempts} attempts");
		}

		// Force walls on the map edges
		if (ShouldBeClosed)
		{
			ForceWalls();
		}

		// Apply the final map to tilemap
		ApplyToTilemap();
	}

	private void InitializeMap(int seedValue)
	{
		RandomNumberGenerator rng = new()

		{
			Seed = (ulong)seedValue
		};

		for (int x = 0; x < MapWidth; x++)
		{
			for (int y = 0; y < MapHeight; y++)
			{
				// Fill edges with walls
				if (x == 0 || x == MapWidth - 1 || y == 0 || y == MapHeight - 1)
				{
					map[x, y] = WallTile;
				}
				else
				{
					map[x, y] = rng.RandiRange(0, 99) < FillPercent ? WallTile : Empty;
				}
			}
		}
	}

	private void SmoothMap()
	{
		int[,] newMap = new int[MapWidth, MapHeight];

		for (int x = 0; x < MapWidth; x++)
		{
			for (int y = 0; y < MapHeight; y++)
			{
				int wallCount = GetSurroundingWallCount(x, y);

				if (wallCount > 4)
					newMap[x, y] = WallTile;
				else if (wallCount < 4)
					newMap[x, y] = Empty;
				else
					newMap[x, y] = map[x, y];
			}
		}

		map = newMap;
	}

	private int GetSurroundingWallCount(int cx, int cy)
	{
		int wallCount = 0;
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				int nx = cx + x;
				int ny = cy + y;
				if (x == 0 && y == 0)
					continue;

				if (nx < 0 || nx >= MapWidth || ny < 0 || ny >= MapHeight)
					wallCount++;
				else if (map[nx, ny] == WallTile)
					wallCount++;
			}
		}
		return wallCount;
	}

	private void CreateRoom(Vector2I center, int radius)
	{
		for (int x = -radius; x <= radius; x++)
		{
			for (int y = -radius; y <= radius; y++)
			{
				int nx = center.X + x;
				int ny = center.Y + y;
				if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight)
				{
					if (Math.Sqrt(x * x + y * y) <= radius)
					{
						map[nx, ny] = Empty;
					}
				}
			}
		}
	}

	private bool CheckPathExists()
	{
		var astar = new AStar2D();

		// Add all empty points to AStar
		for (int x = 0; x < MapWidth; x++)
		{
			for (int y = 0; y < MapHeight; y++)
			{
				if (map[x, y] == Empty)
				{
					int pointId = GetPointId(x, y);
					astar.AddPoint(pointId, new Vector2(x, y));
				}
			}
		}

		// Connect neighboring points
		for (int x = 0; x < MapWidth; x++)
		{
			for (int y = 0; y < MapHeight; y++)
			{
				if (map[x, y] == Empty)
				{
					int pointId = GetPointId(x, y);
					foreach (var offset in new Vector2I[] {
						new(1, 0), new(-1, 0),
						new(0, 1), new(0, -1)
					})
					{
						int nx = x + offset.X;
						int ny = y + offset.Y;
						if (IsValidEmptyPos(nx, ny))
						{
							int nextId = GetPointId(nx, ny);
							if (!astar.ArePointsConnected(pointId, nextId))
								astar.ConnectPoints(pointId, nextId);
						}
					}
				}
			}
		}

		int startId = GetPointId(startPoint.X, startPoint.Y);
		int endId = GetPointId(endPoint.X, endPoint.Y);

		return astar.HasPoint(startId) && astar.HasPoint(endId) &&
			astar.GetPointPath(startId, endId).Length > 0;
	}

	private bool IsValidEmptyPos(int x, int y)
	{
		return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight && map[x, y] == Empty;
	}

	private int GetPointId(int x, int y)
	{
		return x + y * MapWidth;
	}

	private void ForceWalls()
	{
		if (!ShouldBeClosed)
			return;

		// Top and bottom edges
		for (int x = 0; x < MapWidth; x++)
		{
			map[x, 0] = WallTile;
			map[x, MapHeight - 1] = WallTile;
		}
		// Left and right edges
		for (int y = 0; y < MapHeight; y++)
		{
			map[0, y] = WallTile;
			map[MapWidth - 1, y] = WallTile;
		}
	}

	private void ApplyToTilemap()
	{
		for (int x = 0; x < MapWidth; x++)
		{
			for (int y = 0; y < MapHeight; y++)
			{
				Vector2I pos = new(x, y);
				if (map[x, y] == WallTile)
				{
					SetCell(pos, 0, new Vector2I(0, (int)(GD.Randi() % 2)), 0);
				}
				else
				{
					SetCell(pos, 0, new Vector2I(0, 0), -1);
				}
			}
		}
	}
}
