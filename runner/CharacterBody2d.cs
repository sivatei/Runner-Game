using Godot;
using System;

public partial class CharacterBody2d : CharacterBody2D
{
	public const float Speed = 400.0f;
	public const float JumpVelocity = -570.0f;

	private AnimatedSprite2D _animatedSprite;
	private int _health = 3;
	private Camera2D _camera;
	
	private Texture2D _heartFull;
	private Texture2D _heartEmpty;
	private HBoxContainer _heartContainer;
	
	private Label _deathLabel;
	private string[] _deathTaunts = new string[] { "NOOB😒", "Bro got absolutely wiped 💀🙏", "🫵🤣" };

	private bool _isDead = false;
	private double _deathTimer = 0;
	
	private static int _lastTauntIndex = -1;
	
	private bool _hasWon = false;
	private Control _winBg;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_camera = GetNode<Camera2D>("Camera2D");
		_camera.TopLevel = true;
		
		// Setup HUD safely via code
		_heartFull = GD.Load<Texture2D>("res://kenney_new-platformer-pack-1.1/Sprites/Tiles/Default/hud_heart.png");
		_heartEmpty = GD.Load<Texture2D>("res://kenney_new-platformer-pack-1.1/Sprites/Tiles/Default/hud_heart_empty.png");
		
		CanvasLayer canvas = new CanvasLayer();
		AddChild(canvas);
		
		MarginContainer margin = new MarginContainer();
		margin.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		margin.AddThemeConstantOverride("margin_top", 20);
		canvas.AddChild(margin);
		
		_heartContainer = new HBoxContainer();
		_heartContainer.Alignment = BoxContainer.AlignmentMode.Center;
		_heartContainer.AddThemeConstantOverride("separation", 2);
		margin.AddChild(_heartContainer);
		
		CenterContainer centerContainer = new CenterContainer();
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		canvas.AddChild(centerContainer);

		_deathLabel = new Label();
		_deathLabel.AddThemeFontSizeOverride("font_size", 64);
		_deathLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1)); // White text
		_deathLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
		_deathLabel.AddThemeConstantOverride("outline_size", 8);
		_deathLabel.Hide();
		centerContainer.AddChild(_deathLabel);
		
		// Setup Win Screen using the user's custom scene!
		PackedScene winScreenScene = GD.Load<PackedScene>("res://control.tscn");
		_winBg = winScreenScene.Instantiate<Control>();
		_winBg.Hide();
		canvas.AddChild(_winBg);
		
		// Setup Mobile Controls Container
		Control mobileControls = new Control();
		mobileControls.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		// Automatically hide on-screen buttons if playing on a PC!
		if (!OS.HasFeature("mobile"))
		{
			mobileControls.Hide();
		}
		canvas.AddChild(mobileControls);

		// Left Arrow
		Label lblLeft = new Label();
		lblLeft.Text = "<";
		lblLeft.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		lblLeft.Position = new Vector2(40, -120);
		lblLeft.AddThemeFontSizeOverride("font_size", 64);
		lblLeft.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.5f));
		mobileControls.AddChild(lblLeft);
		
		TouchScreenButton btnLeft = new TouchScreenButton();
		RectangleShape2D rectLeft = new RectangleShape2D();
		rectLeft.Size = new Vector2(150, 150);
		btnLeft.Shape = rectLeft;
		btnLeft.Position = new Vector2(20, 30);
		btnLeft.Action = "ui_left";
		lblLeft.AddChild(btnLeft);

		// Right Arrow
		Label lblRight = new Label();
		lblRight.Text = ">";
		lblRight.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		lblRight.Position = new Vector2(160, -120);
		lblRight.AddThemeFontSizeOverride("font_size", 64);
		lblRight.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.5f));
		mobileControls.AddChild(lblRight);
		
		TouchScreenButton btnRight = new TouchScreenButton();
		RectangleShape2D rectRight = new RectangleShape2D();
		rectRight.Size = new Vector2(150, 150);
		btnRight.Shape = rectRight;
		btnRight.Position = new Vector2(20, 30);
		btnRight.Action = "ui_right";
		lblRight.AddChild(btnRight);

		// Jump Button
		Label lblJump = new Label();
		lblJump.Text = "[ JUMP ]";
		lblJump.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		lblJump.Position = new Vector2(-220, -120);
		lblJump.AddThemeFontSizeOverride("font_size", 48);
		lblJump.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.5f));
		mobileControls.AddChild(lblJump);
		
		TouchScreenButton btnJump = new TouchScreenButton();
		RectangleShape2D rectJump = new RectangleShape2D();
		rectJump.Size = new Vector2(250, 150);
		btnJump.Shape = rectJump;
		btnJump.Position = new Vector2(100, 30);
		btnJump.Action = "ui_up";
		lblJump.AddChild(btnJump);

		UpdateHearts();
	}

	private void UpdateHearts()
	{
		foreach (Node child in _heartContainer.GetChildren())
		{
			child.QueueFree();
		}
		
		for (int i = 0; i < 3; i++)
		{
			TextureRect heart = new TextureRect();
			heart.Texture = (i < _health) ? _heartFull : _heartEmpty;
			heart.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			heart.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			heart.CustomMinimumSize = new Vector2(40, 40);
			_heartContainer.AddChild(heart);
		}
	}

	public void TakeDamage()
	{
		if (_isDead) return;
		
		_health--;
		UpdateHearts();
		if (_health <= 0)
		{
			Die();
		}
	}
	
	private void Die()
	{
		if (_isDead) return;
		_isDead = true;
		_deathTimer = 2.0; // Wait 2 seconds
		
		// Show random death taunt (guaranteed not to repeat the last one)
		Random random = new Random();
		int nextIndex = random.Next(_deathTaunts.Length);
		while (nextIndex == _lastTauntIndex)
		{
			nextIndex = random.Next(_deathTaunts.Length);
		}
		_lastTauntIndex = nextIndex;
		_deathLabel.Text = _deathTaunts[nextIndex];
		_deathLabel.Show();
		
		// Little bounce effect when dying
		Vector2 vel = Velocity;
		vel.Y = -400;
		Velocity = vel;
		
		// Disable collision so they fall through the floor
		GetNode<CollisionPolygon2D>("CollisionPolygon2D").SetDeferred("disabled", true);
	}

	public void WinGame()
	{
		if (_isDead || _hasWon) return;
		_hasWon = true;
		Velocity = Vector2.Zero; // Stop the player completely
		_winBg.Show();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hasWon)
		{
			if (Input.IsPhysicalKeyPressed(Key.Space) || Input.IsMouseButtonPressed(MouseButton.Left))
			{
				GetTree().ReloadCurrentScene();
			}
			return; // Do not process movement while on win screen
		}

		if (_isDead)
		{
			_deathTimer -= delta;
			if (_deathTimer <= 0)
			{
				GetTree().ReloadCurrentScene();
				return;
			}
			
			// Still apply gravity and let camera follow
			Vector2 deadVel = Velocity;
			deadVel += GetGravity() * (float)delta;
			Velocity = deadVel;
			MoveAndSlide();
			
			UpdateCamera();
			return;
		}
		
		// Fall off the map detection
		if (GlobalPosition.Y > 1200)
		{
			Die();
		}

		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump with Space or Mobile UI.
		if ((Input.IsPhysicalKeyPressed(Key.Space) || Input.IsActionPressed("ui_up")) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction with A and D keys or Mobile UI.
		float direction = 0;
		if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsActionPressed("ui_left")) direction -= 1;
		if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsActionPressed("ui_right")) direction += 1;
		
		if (direction != 0)
		{
			velocity.X = direction * Speed;
			// Flip the sprite depending on direction (D is right, A is left)
			_animatedSprite.FlipH = direction < 0;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		UpdateCamera();

		// Handle Animations
		if (!IsOnFloor())
		{
			_animatedSprite.Play("jump");
		}
		else
		{
			if (direction != 0)
			{
				_animatedSprite.Play("walk");
			}
			else
			{
				_animatedSprite.Play("idle");
			}
		}
	}

	private void UpdateCamera()
	{
		if (_camera != null)
		{
			Vector2 camPos = _camera.GlobalPosition;
			
			// 1. Move camera right when moving forward
			if (GlobalPosition.X > camPos.X)
			{
				camPos.X = GlobalPosition.X;
			}
			
			// 2. Move camera left if the player walks back to the left edge of the screen
			// We dynamically calculate the true left edge using the viewport and camera offset
			// to ensure the player NEVER goes off-screen regardless of window size.
			float halfWidth = GetViewportRect().Size.X / 2.0f;
			float leftBoundary = camPos.X + _camera.Offset.X - halfWidth + 100; // 100 pixels safe margin from left edge
			
			if (GlobalPosition.X < leftBoundary)
			{
				// Pull the camera back so the player stays at the boundary
				camPos.X = GlobalPosition.X - _camera.Offset.X + halfWidth - 300;
			}

			// 3. Keep Y locked so the ground stays in the same place when jumping!
			// We ONLY follow the player's Y if they are dead and falling into the abyss.
			if (_isDead)
			{
				camPos.Y = GlobalPosition.Y;
			}

			_camera.GlobalPosition = camPos;
		}
	}
}
