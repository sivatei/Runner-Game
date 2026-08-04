using Godot;
using System;
using System.Linq;

public partial class Main : Node
{
	private PackedScene _cactusScene;
	private PackedScene _bombScene;
	private PackedScene _doorScene;
	private TileMapLayer _tileMapLayer;
	private CharacterBody2D _player;

	public override void _Ready()
	{
		_cactusScene = GD.Load<PackedScene>("res://cactus.tscn");
		_bombScene = GD.Load<PackedScene>("res://bomb.tscn");
		_doorScene = GD.Load<PackedScene>("res://door.tscn");
		_tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
		_player = GetNode<CharacterBody2D>("CharacterBody2D");

		SetupLevel();
	}

	private void SetupLevel()
	{
		Godot.Collections.Array<Vector2I> usedCells = _tileMapLayer.GetUsedCells();
		if (usedCells.Count == 0) return;
		
		int minX = usedCells[0].X;
		int maxX = usedCells[0].X;

		// Group by X and find the top tile (minimum Y) for each column
		var topCells = new System.Collections.Generic.List<Vector2I>();
		var xGroups = usedCells.GroupBy(c => c.X);
		foreach (var group in xGroups)
		{
			int x = group.Key;
			if (x < minX) minX = x;
			if (x > maxX) maxX = x;
			int topY = group.Min(c => c.Y);
			topCells.Add(new Vector2I(x, topY));
		}
		
		// Generate robust collisions for all tiles so we don't have to rely on buggy .tscn manual edits
		foreach (Vector2I cell in usedCells)
		{
			StaticBody2D tileCollision = new StaticBody2D();
			CollisionShape2D shape = new CollisionShape2D();
			RectangleShape2D rect = new RectangleShape2D();
			rect.Size = new Vector2(64, 64);
			shape.Shape = rect;
			tileCollision.AddChild(shape);
			tileCollision.Position = _tileMapLayer.MapToLocal(cell);
			AddChild(tileCollision);
		}

		Random random = new Random();
		int lastHazardX = -999;
		
		// Sort by X so we can check spacing properly
		topCells = topCells.OrderBy(c => c.X).ToList();

		foreach (Vector2I cell in topCells)
		{
			// Check minimum distance (e.g., at least 3 tiles apart to be a bit easier)
			if (cell.X - lastHazardX < 3) continue;

			// Get the world position of this tile
			Vector2 localPos = _tileMapLayer.MapToLocal(cell);

			// ONLY spawn if the tile is well within the playable area (X > 600) so the player has safe starting space
			// Also avoid spawning an obstacle right on the door (maxX)
			if (localPos.X < 600 || cell.X >= maxX - 1) continue;

			// Random chance to spawn - reduced to 25% for balanced difficulty
			if (random.NextDouble() < 0.25)
			{
				PackedScene sceneToSpawn = random.NextDouble() < 0.5 ? _cactusScene : _bombScene;
				Area2D hazard = sceneToSpawn.Instantiate<Area2D>();
				
				hazard.BodyEntered += (Node2D body) => {
					if (body is CharacterBody2d player)
					{
						player.TakeDamage();
						hazard.QueueFree();
					}
				};

				hazard.Position = localPos + new Vector2(0, -64);
				AddChild(hazard);
				
				lastHazardX = cell.X;
			}
		}

		// Do NOT override the player's position, let them start exactly where they were placed in the editor!
		// This ensures they start in front of the boundaries you placed.

		// Immediately snap camera to prevent swoop and properly initialize the locked Y-axis
		Camera2D cam = _player.GetNode<Camera2D>("Camera2D");
		if (cam != null)
		{
			cam.GlobalPosition = _player.GlobalPosition;
			cam.ResetSmoothing();
		}

		// Place door at the end
		var maxCell = topCells.First(c => c.X == maxX);
		Area2D door = _doorScene.Instantiate<Area2D>();
		
		door.BodyEntered += (Node2D body) => {
			if (body is CharacterBody2d player)
			{
				player.WinGame();
			}
		};

		Vector2 doorPos = _tileMapLayer.MapToLocal(maxCell);
		door.Scale = new Vector2(2.0f, 2.0f); // Make the door a bit smaller
		// Slightly adjusted the offset so it sits perfectly flush with the visual top of the grass tile
		// Shifted X by -32 to move it left a little bit
		door.Position = doorPos + new Vector2(-32, -88); 
		AddChild(door);
	}
}
