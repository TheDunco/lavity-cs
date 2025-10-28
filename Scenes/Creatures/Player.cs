using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Creature
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
	private StatsDisplay statsDisplay = null;

	private Tween statTween;
	private double pendingEnergy;
	private double pendingHealth;
	private bool IsInputAdded = false;

	private AudioStreamPlayer WingFlapSounds = null;

	// Camera
	private CameraController Camera = null;
	[Export] public float ZoomSpeed = 1f;     // How fast zoom target changes when holding
	[Export] public float MinZoom = 0.5f;       // Minimum zoom factor
	[Export] public float MaxZoom = 2.0f;       // Maximum zoom factor
	[Export] public float ZoomTweenTime = 0.01f; // Time for smooth interpolation
	private Vector2 targetZoom;
	private Tween zoomTween;

	private bool DisableCollisionDamage = false;

	private Area2D RepulseArea = null;
	private AnimationPlayer RepulseAnimation = null;

	// Light
	private RandomNumberGenerator rng = null;
	private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes/Common/Projectile.tscn");
	private AudioStreamPlayer ProjectileSound = null;
	public override void _Ready()
	{
		base._Ready();
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<CameraController>("../Player/Camera");
		targetZoom = Camera.Zoom;
		OnConsumeSound = GetNode<AudioStreamPlayer>("OnConsumeSound");
		statsDisplay = GetNode<StatsDisplay>("../StatsDisplay");
		RepulseArea = GetNode<Area2D>("RepulseArea");
		RepulseAnimation = GetNode<AnimationPlayer>("RepulseAnimation");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		ProjectileSound = GetNode<AudioStreamPlayer>("ProjectileSound");

		Energy = MaxEnergy * 0.75;
		Health = MaxHealth;

		var statsManager = GetNode<StatsManager>("/root/StatsManager");
		statsManager.StatsTick += OnStatsTick;
	}
	public List<Consumable> GetStomachConsumables()
	{
		List<Consumable> ret = [];
		var children = GetChildren();
		foreach (Node n in children)
		{
			if (n is Consumable c)
			{
				ret.Add(c);
			}
		}
		return ret;
	}

	public bool IsLightOn()
	{
		return LavityLight.IsEnabled();
	}

	public double GetCurrentFullness()
	{
		double fullness = 0;
		foreach (var consumable in GetStomachConsumables())
		{
			fullness += consumable.Effect.StomachSpace;
		}
		return fullness;
	}
	public void EatConsumable(Consumable consumable)
	{
		double Fullness = GetCurrentFullness();
		if (Fullness < MaxStomachSpace)
		{
			consumable.Reparent(this);
		}
		else
		{
			Health -= Fullness - MaxStomachSpace;
		}
	}

	private void OnStatsTick()
	{
		DisableCollisionDamage = false;
		Fullness = GetCurrentFullness();
		// Start from current values
		double newEnergy = Energy;
		double newHealth = Health;

		// Passive drain if light is on
		if (LavityLight.IsEnabled())
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
		List<Consumable> stomachConsumables = GetStomachConsumables();
		for (int i = GetStomachConsumables().Count - 1; i >= 0; i--)
		{
			var stomachConsumable = stomachConsumables[i];
			var effect = stomachConsumables[i].Effect;
			newEnergy += effect.EnergyMod;
			newHealth += effect.HealthMod;

			effect.Duration -= 1.0;
			effect.StomachSpace -= effect.StomachSpace / effect.Duration;
			plantsDigestedThisTick += 1;
			if (effect.Duration <= 0 || effect.StomachSpace <= 0)
			{
				RemoveChild(stomachConsumable);
			}

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

		LavityLight.SetEnergy(newEnergy);

		if (Health == 0)
		{
			GetTree().Quit();
		}
		else if (Health < MaxHealth * 0.25)
		{
			LavityLight.SetColor(Colors.Red);
		}
		else
		{
			LavityLight.SetColor(Colors.White);
		}
		Acceleration = (int)Mathf.Remap(Energy, 0f, 100f, 0.5 * BaseAcceleration, 1.5 * BaseAcceleration);
	}

	public override void _Process(double delta)
	{

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

		// Reset logic
		if (Input.IsKeyPressed(Key.Backspace))
		{
			Position = Vector2.Zero;
			Velocity = Vector2.Zero;
		}

		// Handle animation & sound
		if (IsInputAdded)
		{
			Sprite.Play();
			if (!WingFlapSounds.Playing)
				WingFlapSounds.Play();
		}
		else
		{
			Sprite.Stop();
		}

		LookAt(Velocity.Normalized() + Position);
		OrientByRotation();
	}

	private float AirResistance = 10;
	private float MaxSpeed = 2000;
	private float TurnSpeed = 1;
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Vector2 input = Vector2.Zero;
		IsInputAdded = false;
		float fDelta = (float)delta;

		if (Input.IsActionPressed("MoveUp")) { input.Y -= 1; IsInputAdded = true; }
		if (Input.IsActionPressed("MoveLeft")) { input.X -= 1; IsInputAdded = true; }
		if (Input.IsActionPressed("MoveDown")) { input.Y += 1; IsInputAdded = true; }
		if (Input.IsActionPressed("MoveRight")) { input.X += 1; IsInputAdded = true; }


		if (IsInputAdded)
		{
			Vector2 targetDir = input.Normalized();

			// Determine how aligned the input is with current velocity
			float alignment = Velocity.Normalized().Dot(targetDir);
			// alignment = 1 → same direction
			// alignment = 0 → perpendicular
			// alignment = -1 → opposite direction

			// If moving and not facing opposite, smooth turn
			if (Velocity.Length() > 0.01f && alignment > -0.7f)
			{
				float currentAngle = Velocity.Angle();
				float targetAngle = targetDir.Angle();
				float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, TurnSpeed * fDelta);
				float speed = Velocity.Length();
				Velocity = Vector2.FromAngle(newAngle) * speed;
			}
			// If nearly opposite input → decelerate instead of rotating
			else if (alignment <= -0.7f)
			{
				// Slow down before reversing
				Velocity = Velocity.MoveToward(Vector2.Zero, Acceleration * fDelta);
			}

			// Accelerate toward target speed
			Velocity = Velocity.MoveToward(targetDir * MaxSpeed, Acceleration * fDelta);
		}
		else
		{
			// Passive air resistance when idle
			Velocity = Velocity.MoveToward(Vector2.Zero, AirResistance * fDelta);
		}

		Velocity += GetGravity() * fDelta;
		bool didCollide = MoveAndSlide();

		if (didCollide)
		{
			var collision = GetLastSlideCollision();
			var collider = collision.GetCollider();

			if (collider is Consumable consumableCollision && !LavityLight.IsEnabled() && IsInstanceValid(collider))
			{
				EatConsumable(consumableCollision.OnConsume());
				OnConsumeSound?.Play();
			}

			if (!DisableCollisionDamage && collider is Lanternfly lanternfly)
			{
				if (LavityLight.IsEnabled())
					Energy -= lanternfly.Damage;
				else
					Health -= lanternfly.Damage;

				DisableCollisionDamage = true;
			}
		}

		// Smoothly rotate sprite toward current movement direction
		if (Velocity.LengthSquared() > 0.1f)
			Rotation = Mathf.LerpAngle(Rotation, Velocity.Angle(), (float)(TurnSpeed * delta));
	}



	private void Repulse()
	{
		Energy -= 10;
		RepulseAnimation.CurrentAnimation = "Repulse";
		Camera.Shake(1f, 2.25f);

		float repulseStrength = 98 * 4f;
		float charVelocityMultiplier = 10f;

		float maxRadius = 0f;
		var cs = RepulseArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (cs != null && cs.Shape != null)
		{
			if (cs.Shape is CircleShape2D cshape)
				maxRadius = cshape.Radius;
			else
				maxRadius = cs.Shape.GetRect().Size.Length() * 0.5f;
		}
		else
		{
			maxRadius = 200f;
		}

		Vector2 origin = GlobalPosition;

		foreach (PhysicsBody2D node in RepulseArea.GetOverlappingBodies())
		{
			if (node == this)
				continue;

			Vector2 toBody = node.GlobalPosition - origin;
			float distance = toBody.Length();
			if (distance <= 0.001f)
				continue;

			Vector2 direction = toBody / distance;

			// quadratic falloff (strong at center, smooth fade to edges)
			float t = Mathf.Clamp(distance / maxRadius, 0f, 1f);
			float falloff = Mathf.Pow(1f - t, 2f);

			float impulseMagnitude = repulseStrength * falloff;

			if (node is RigidBody2D rigidBody)
			{
				rigidBody.ApplyCentralImpulse(direction * impulseMagnitude);
			}
			else if (node is CharacterBody2D charBody)
			{
				charBody.Velocity += direction * impulseMagnitude * charVelocityMultiplier;
			}
		}

	}

	private void FireProjectile()
	{
		List<Consumable> stomachConsumables = GetStomachConsumables();

		if (stomachConsumables.Count == 0)
			return;

		Camera.Shake(0.7f, 0.5f);

		int index = stomachConsumables.Count - 1;
		var stomachConsumable = stomachConsumables[index];
		RemoveChild(stomachConsumable);

		int spawnOffset = 90;

		Vector2 direction = new Vector2(Mathf.Cos(GlobalRotation), Mathf.Sin(GlobalRotation)).Normalized();

		Projectile projectile = ProjectileScene.Instantiate<Projectile>();
		AddChild(projectile);
		projectile.Reparent(GetTree().CurrentScene);

		projectile.GlobalPosition = GlobalPosition + (direction * spawnOffset);

		float impulseStrength = 1500;

		projectile.ApplyCentralImpulse(direction * impulseStrength);

		float torque = rng.RandfRange(-200f, 200f);
		projectile.ApplyTorqueImpulse(torque);

		projectile.SetLifetime(5);
		ProjectileSound.Play();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (@event.IsActionPressed("ToggleLight"))
		{
			LavityLight.Toggle();
		}

		if (@event.IsActionPressed("Repulse") && RepulseAnimation.CurrentAnimation != "Repulse")
		{
			Repulse();
		}

		if (@event.IsActionPressed("FireProjectile"))
		{
			FireProjectile();
		}
	}
}
