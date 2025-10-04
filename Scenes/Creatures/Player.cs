using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class Player : CharacterBody2D
{
	// Stats
	[Export] public double MaxEnergy = 100;
	[Export] public double MaxHealth = 100;
	[Export] public double MaxStomachSpace = 100;
	private int MaxPlantsDigestedPerCycle = 2;
	private AudioStreamPlayer OnConsumeSound = null;

	public double Energy { get; private set; }
	public double Health { get; private set; }
	public double Fullness { get; private set; }

	private List<PlantEffect> Stomach = [];

	private Tween statTween;
	private double pendingEnergy;
	private double pendingHealth;
	private bool IsInputAdded = false;

	// Movement
	private readonly static int BaseAcceleration = 1000;
	private int Acceleration = BaseAcceleration;
	private float AirResistance = 0.0002f;
	private float MaxVelocity = 1500;

	private AnimatedSprite2D Sprite = null;
	private AudioStreamPlayer WingFlapSounds = null;

	// Camera
	private Camera2D Camera = null;
	[Export] public float ZoomSpeed = 1f;     // How fast zoom target changes when holding
	[Export] public float MinZoom = 0.5f;       // Minimum zoom factor
	[Export] public float MaxZoom = 2.0f;       // Maximum zoom factor
	[Export] public float ZoomTweenTime = 0.01f; // Time for smooth interpolation
	private Vector2 targetZoom;
	private Tween zoomTween;

	// Light
	private LavityLight PlayerLight = null;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<Camera2D>("Camera");
		targetZoom = Camera.Zoom;
		PlayerLight = GetNode<LavityLight>("LavityLight");
		OnConsumeSound = GetNode<AudioStreamPlayer>("OnConsumeSound");

		Energy = MaxEnergy * 0.75;
		Health = MaxHealth;

		var statsManager = GetNode<StatsManager>("/root/StatsManager");
		statsManager.StatsTick += OnStatsTick;
	}
	public double GetCurrentFullness()
	{
		double fullness = 0;
		foreach (var effect in Stomach)
		{
			fullness += effect.StomachSpace;
		}
		return fullness;
	}
	public void EatPlant(PlantEffect effect)
	{
		double Fullness = GetCurrentFullness();
		if (Fullness < MaxStomachSpace)
		{

			Stomach.Add(effect);
		}
		else
		{
			Health -= Fullness - MaxStomachSpace;
		}
	}

	private void OnStatsTick()
	{
		Fullness = GetCurrentFullness();
		// Start from current values
		double newEnergy = Energy;
		double newHealth = Health;

		// Passive drain if light is on
		if (PlayerLight.IsEnabled())
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
		var plantsDigestedThisTick = 0;
		for (int i = Stomach.Count - 1; i >= 0; i--)
		{
			var effect = Stomach[i];
			newEnergy += effect.EnergyMod;
			newHealth += effect.HealthMod;

			effect.Duration -= 1.0;
			effect.StomachSpace -= effect.StomachSpace / effect.Duration;
			plantsDigestedThisTick += 1;
			if (effect.Duration <= 0 || effect.StomachSpace <= 0)
				Stomach.RemoveAt(i);

			if (plantsDigestedThisTick >= MaxPlantsDigestedPerCycle)
			{
				break;
			}
		}

		if (newEnergy <= 0.0)
		{
			newHealth -= Math.Abs(newEnergy);
		}
		// slow passive health regen
		else if (newEnergy >= 99 && Health < MaxHealth)
		{
			newHealth += 0.1;
		}

		// Clamp values
		Energy = Mathf.Clamp(newEnergy, 0, MaxEnergy);
		Health = Mathf.Clamp(newHealth, 0, MaxHealth);

		if (Health == 0)
		{
			GetTree().Quit();
		}
		Acceleration = (int)Mathf.Remap(Energy, 0f, 100f, 0.5 * BaseAcceleration, 1.25 * BaseAcceleration);
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
			if (collision.GetCollider() is Consumable consumableCollision && !PlayerLight.IsEnabled())
			{
				EatPlant(consumableCollision.OnConsume());
				OnConsumeSound?.Play();
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (@event.IsActionPressed("ToggleLight"))
		{
			PlayerLight.Toggle();
		}
	}


}
