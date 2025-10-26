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

	// Movement
	private float AirResistance = 0.0002f;
	private float MaxVelocity = 1500;

	private AnimatedSprite2D Sprite = null;
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
	private LavityLight PlayerLight = null;
	private RandomNumberGenerator rng = null;
	private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes/Common/Projectile.tscn");
	private AudioStreamPlayer ProjectileSound = null;
	private LavityLight lavityLight = null;
	public override void _Ready()
	{
		base._Ready();
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<CameraController>("../Camera");
		targetZoom = Camera.Zoom;
		PlayerLight = GetNode<LavityLight>("LavityLight");
		OnConsumeSound = GetNode<AudioStreamPlayer>("OnConsumeSound");
		statsDisplay = GetNode<StatsDisplay>("../StatsDisplay");
		RepulseArea = GetNode<Area2D>("RepulseArea");
		RepulseAnimation = GetNode<AnimationPlayer>("RepulseAnimation");
		rng = GetNode<RngManager>("/root/RngManager").Rng;
		ProjectileSound = GetNode<AudioStreamPlayer>("ProjectileSound");
		lavityLight = GetNode<LavityLight>("LavityLight");

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

		PlayerLight.SetEnergy(newEnergy);

		if (Health == 0)
		{
			GetTree().Quit();
		}
		else if (Health < MaxHealth * 0.25)
		{
			PlayerLight.SetColor(Colors.Red);
		}
		else
		{
			PlayerLight.SetColor(Colors.White);
		}
		Acceleration = (int)Mathf.Remap(Energy, 0f, 100f, 0.5 * BaseAcceleration, 1.5 * BaseAcceleration);
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
			if (collider is Consumable consumableCollision && !PlayerLight.IsEnabled() && IsInstanceValid(collider))
			{
				EatConsumable(consumableCollision.OnConsume());
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
			PlayerLight.Toggle();
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
