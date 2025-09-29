using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	// Stats
	[Export] public double MaxEnergy = 100;
	[Export] public double MaxHealth = 100;

	public double Energy { get; private set; }
	public double Health { get; private set; }

	private List<PlantEffect> Stomach = [];

	private Tween statTween;
	private double pendingEnergy;
	private double pendingHealth;
	private bool IsInputAdded = false;

	// Movement
	private int Acceleration = 1000;
	private float AirResistance = 0.0002f;
	private float MaxVelocity = 1500;

	private AnimatedSprite2D Sprite = null;
	private AudioStreamPlayer WingFlapSounds = null;

	// Camera
	private Camera2D Camera = null;
	[Export] public float ZoomSpeed = 1f;     // How fast zoom target changes when holding
	[Export] public float MinZoom = 0.25f;       // Minimum zoom factor
	[Export] public float MaxZoom = 2.0f;       // Maximum zoom factor
	[Export] public float ZoomTweenTime = 0.01f; // Time for smooth interpolation
	private Vector2 targetZoom;
	private Tween zoomTween;

	// Light
	private PointLight2D PlayerLight = null;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<Camera2D>("Camera");
		targetZoom = Camera.Zoom;
		PlayerLight = GetNode<PointLight2D>("PlayerLight");

		Energy = MaxEnergy * 0.25;
		Health = MaxHealth;

		var statsManager = GetNode<StatsManager>("/root/StatsManager");
		statsManager.StatsTick += OnStatsTick;
	}
	public void EatPlant(PlantEffect effect)
	{
		Stomach.Add(effect);
	}

	private void OnStatsTick()
	{
		GD.Print("Stats Tick || ", "Health: ", Health, " Energy: ", Energy);
		// Start from current values
		double newEnergy = Energy;
		double newHealth = Health;

		// Passive drain if light is on
		if (PlayerLight.Enabled)
			newEnergy -= 0.5;

		if (IsInputAdded)
			newEnergy -= 1.0;

		if (targetZoom != Vector2.One)
		{
			double drainMultiplier = Mathf.Remap(
				Camera.Zoom.X,
				MinZoom, MaxZoom,
				1.0, 0.0
			);
			newEnergy -= drainMultiplier;
		}

		// Digest plants
		for (int i = Stomach.Count - 1; i >= 0; i--)
		{
			var effect = Stomach[i];
			newEnergy += effect.EnergyMod;
			newHealth += effect.HealthMod;

			effect.Duration -= 1.0;
			if (effect.Duration <= 0)
				Stomach.RemoveAt(i);
		}

		// Health-energy interactions
		if (newEnergy <= 1.0)
		{
			newHealth -= 1.0;
		}
		else if (newEnergy >= MaxEnergy * 0.5 && newHealth < MaxHealth)
		{
			newHealth += 0.5;
		}

		// Clamp values
		Energy = Mathf.Clamp(newEnergy, 0, MaxEnergy);
		Health = Mathf.Clamp(newHealth, 0, MaxHealth);
	}

	private void OrientByRotation()
	{

		var CurScaleY = Scale.Y;
		double rotationCos = Math.Cos(Rotation);
		if (rotationCos < 0 && CurScaleY > 0)
		{

			Scale = new Vector2(Scale.X, -Scale.Y);
		}
		else if (rotationCos > 0 && CurScaleY < 0)
		{

			Scale = new Vector2(Scale.X, Math.Abs(Scale.Y));
		}
	}

	public override void _Process(double delta)
	{

		IsInputAdded = false;
		if (Input.IsActionPressed("MoveUp"))
		{
			Velocity += Vector2.Up * Acceleration * (float)delta;
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveLeft"))
		{
			Velocity += Vector2.Left * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveDown"))
		{
			Velocity += Vector2.Down * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsActionPressed("MoveRight"))
		{
			Velocity += Vector2.Right * Acceleration * (float)(delta);
			IsInputAdded = true;
		}

		if (Input.IsKeyPressed(Key.Backspace))
		{
			Position = Vector2.Zero;
			Velocity = Vector2.Zero;
		}

		if (Input.IsActionPressed("ZoomIn"))
		{
			targetZoom += new Vector2(ZoomSpeed, ZoomSpeed) * (float)delta;

		}

		if (Input.IsActionPressed("ZoomOut"))
		{
			targetZoom -= new Vector2(ZoomSpeed, ZoomSpeed) * (float)delta;

		}
		// Clamp target zoom
		targetZoom.X = Mathf.Clamp(targetZoom.X, MinZoom, MaxZoom);
		targetZoom.Y = Mathf.Clamp(targetZoom.Y, MinZoom, MaxZoom);

		// Smoothly animate toward target zoom
		if (Camera.Zoom != targetZoom)
		{
			// Kill old tween if one is still running
			zoomTween?.Kill();

			zoomTween = CreateTween();
			zoomTween.TweenProperty(Camera, "zoom", targetZoom, ZoomTweenTime)
					 .SetTrans(Tween.TransitionType.Sine)
					 .SetEase(Tween.EaseType.InOut);
		}


		bool isZeroApprox = Velocity.IsZeroApprox();

		if (IsInputAdded)
		{
			Sprite.Play();
			if (!WingFlapSounds.Playing)
			{
				WingFlapSounds.Play();
			}
		}
		else
		{
			Sprite.Stop();
		}

		if (!isZeroApprox && !IsInputAdded)
		{
			Velocity += -Velocity * AirResistance;
		}
		else if (isZeroApprox)
		{
			Velocity = Vector2.Zero;
		}

		Velocity = Velocity.Clamp(-MaxVelocity, MaxVelocity);

		LookAt(Velocity.Normalized() + Position);
		OrientByRotation();

		Velocity += GetGravity();

		bool didCollide = MoveAndSlide();

		if (didCollide)
		{
			var collision = this.GetLastSlideCollision();
			if (collision.GetCollider() is Consumable consumableCollision)
			{
				EatPlant(consumableCollision.OnConsume());
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (@event.IsActionPressed("ToggleLight"))
		{
			PlayerLight.Enabled = !PlayerLight.Enabled;
		}
	}


}
