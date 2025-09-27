using Godot;
using System;
using System.Reflection;

public partial class Player : CharacterBody2D
{
	private int Acceleration = 1000;
	private float AirResistance = 0.0002f;
	private float MaxVelocity = 1500;

	private AnimatedSprite2D Sprite = null;
	private AudioStreamPlayer WingFlapSounds = null;
	private Camera2D Camera = null;
	[Export] public float ZoomSpeed = 1f;     // How fast zoom target changes when holding
	[Export] public float MinZoom = 0.25f;       // Minimum zoom factor
	[Export] public float MaxZoom = 2.0f;       // Maximum zoom factor
	[Export] public float ZoomTweenTime = 0.01f; // Time for smooth interpolation
	private Vector2 targetZoom;
	private Tween zoomTween;


	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WingFlapSounds = GetNode<AudioStreamPlayer>("WingFlapSounds");
		Camera = GetNode<Camera2D>("Camera");
		targetZoom = Camera.Zoom;
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

		bool IsInputAdded = false;
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

		if (Input.IsKeyPressed(Key.Space))
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

		MoveAndSlide();
	}


}
