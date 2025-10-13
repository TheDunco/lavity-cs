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

	public List<Consumable> Stomach = [];

	private Tween statTween;
	private double pendingEnergy;
	private double pendingHealth;
	private bool IsInputAdded = false;

	// Movement
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

	private bool DisableCollisionDamage = false;

	private Area2D RepulseArea = null;
	private AnimationPlayer RepulseAnimation = null;

	// Light
	private LavityLight PlayerLight = null;
	private RandomNumberGenerator rng = null;

	public override void _Ready()
	{
		base._Ready();
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<Camera2D>("Camera");
		targetZoom = Camera.Zoom;
		PlayerLight = GetNode<LavityLight>("LavityLight");
		OnConsumeSound = GetNode<AudioStreamPlayer>("OnConsumeSound");
		statsDisplay = GetNode<StatsDisplay>("../StatsDisplay");
		RepulseArea = GetNode<Area2D>("RepulseArea");
		RepulseAnimation = GetNode<AnimationPlayer>("RepulseAnimation");
		rng = GetNode<RngManager>("/root/RngManager").Rng;

		Energy = MaxEnergy * 0.75;
		Health = MaxHealth;

		var statsManager = GetNode<StatsManager>("/root/StatsManager");
		statsManager.StatsTick += OnStatsTick;
	}
	public double GetCurrentFullness()
	{
		double fullness = 0;
		foreach (var consumable in Stomach)
		{
			fullness += consumable.Effect.StomachSpace;
		}
		return fullness;
	}
	public void EatPlant(Consumable consumable)
	{
		double Fullness = GetCurrentFullness();
		if (Fullness < MaxStomachSpace)
		{
			Stomach.Add(consumable);
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
			var effect = Stomach[i].Effect;
			newEnergy += effect.EnergyMod;
			newHealth += effect.HealthMod;

			effect.Duration -= 1.0;
			effect.StomachSpace -= effect.StomachSpace / effect.Duration;
			plantsDigestedThisTick += 1;
			if (effect.Duration <= 0 || effect.StomachSpace <= 0)
			{
				Stomach.RemoveAt(i);
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

		PlayerLight.SetEnergy(newEnergy);

		if (Health == 0)
		{
			GetTree().Quit();
		}
		Acceleration = (int)Mathf.Remap(Energy, 0f, 100f, 0.5 * BaseAcceleration, 1.25 * BaseAcceleration);
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
			var collider = collision.GetCollider();
			if (collider is Consumable consumableCollision && !PlayerLight.IsEnabled())
			{
				EatPlant(consumableCollision.OnConsume());
				OnConsumeSound?.Play();
			}

			if (!DisableCollisionDamage && collider is Lanternfly lanternfly)
			{
				if (PlayerLight.IsEnabled())
				{
					Energy -= lanternfly.Damage;
				}
				else
				{
					Health -= lanternfly.Damage;
				}
				DisableCollisionDamage = true;
			}
		}
	}

	private void Repulse()
	{
		Energy -= 10;
		RepulseAnimation.CurrentAnimation = "Repulse";

		// Base parameters
		float repulseStrength = 98 * 4f; // base impulse magnitude (tweak to taste)
		float charVelocityMultiplier = 10f; // multiplier for CharacterBody2D; tune for feel

		// Determine max radius from the area collision shape (assumes a CircleShape2D or a rectangular shape)
		float maxRadius = 0f;
		var cs = RepulseArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (cs != null && cs.Shape != null)
		{
			// If it's a CircleShape2D use radius, otherwise approximate from bounding rect
			if (cs.Shape is CircleShape2D cshape)
				maxRadius = cshape.Radius;
			else
				maxRadius = cs.Shape.GetRect().Size.Length() * 0.5f;
		}
		else
		{
			// fallback
			maxRadius = 200f;
		}

		Vector2 origin = GlobalPosition;

		foreach (PhysicsBody2D node in RepulseArea.GetOverlappingBodies())
		{
			if (node == this) // don't push self
				continue;

			// compute vector from origin to body
			Vector2 toBody = node.GlobalPosition - origin;
			float distance = toBody.Length();
			if (distance <= 0.001f)
				continue; // ignore bodies exactly on origin

			Vector2 direction = toBody / distance;

			// quadratic falloff (strong at center, smooth fade to edges)
			float t = Mathf.Clamp(distance / maxRadius, 0f, 1f);
			float falloff = Mathf.Pow(1f - t, 2f);

			// final impulse magnitude (tweak repulseStrength or falloff as needed)
			float impulseMagnitude = repulseStrength * falloff;

			if (node is RigidBody2D rigidBody)
			{
				// Impulse acts immediately; mass is accounted for by the physics engine
				// Multiply if you want a stronger instantaneous effect:
				rigidBody.ApplyCentralImpulse(direction * impulseMagnitude);
			}
			else if (node is CharacterBody2D charBody)
			{
				// CharacterBody2D doesn't accept impulses — modify its velocity instead.
				// Additive so existing player/enemy motion is preserved.
				charBody.Velocity += direction * impulseMagnitude * charVelocityMultiplier;
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

		if (@event.IsActionPressed("Repulse") && RepulseAnimation.CurrentAnimation != "Repulse")
		{
			Repulse();
		}

		if (@event.IsActionPressed("FireProjectile"))
		{
			if (Stomach.Count == 0)
				return; // nothing to fire!

			int index = Stomach.Count - 1;
			Consumable projectile = Stomach[index];
			if (projectile.IsQueuedForDeletion())
			{
				return;
			}
			Stomach.RemoveAt(index);

			// Convert Consumable to projectile
			projectile.SetScript(GD.Load<Script>("res://Scenes/Common/Projectile.cs"));
			projectile.ProcessMode = ProcessModeEnum.Always;

			// Make sure the projectile is an active physics body
			if (projectile is RigidBody2D body)
			{
				// Ensure it's visible and detached from player
				body.Reparent(GetTree().CurrentScene);

				body.GlobalPosition = GlobalPosition;

				// Reset rotation & velocity before firing
				body.Rotation = GlobalRotation;
				body.LinearVelocity = Vector2.Zero;
				body.AngularVelocity = 0;

				// Compute base direction: player facing
				Vector2 direction = new Vector2(Mathf.Cos(GlobalRotation), Mathf.Sin(GlobalRotation)).Normalized();

				// Add a small random spread (optional)
				float spreadAngle = rng.RandfRange(-0.15f, 0.15f); // ~±9 degrees
				direction = direction.Rotated(spreadAngle);

				// Fire speed / impulse strength
				float impulseStrength = 200f;

				// Apply impulse in facing direction
				body.ApplyCentralImpulse(direction * impulseStrength);

				// Add random spin
				float torque = rng.RandfRange(-200f, 200f);
				body.ApplyTorqueImpulse(torque);

				// Optionally give it a lifetime so it despawns later
				if (body.HasMethod("SetLifetime"))
					body.Call("SetLifetime", 5.0f); // 5 seconds
			}
		}

	}
}
