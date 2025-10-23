using Godot;
using System;
using System.Collections.Generic;

public partial class LavityLight : Node2D
{
	private Area2D GravityArea = null;
	private PointLight2D Light = null;
	[Export(PropertyHint.Layers2DPhysics)] private uint CollisionLayer;
	[Export(PropertyHint.Layers2DPhysics)] private uint CollisionMask;

	[Export(PropertyHint.ColorNoAlpha)] private Color LavityLightColor = Colors.White;
	[Export] private int PointUnitDistance = 0;
	private List<GravityCancellation> GravityCancellationList = [];

	private void RemoveBodyFromCancellationList(CharacterBody2D characterBody)
	{
		var CancellationsToRemove = GravityCancellationList.FindAll(b => b != null && b.Body == characterBody);
		foreach (GravityCancellation GC in CancellationsToRemove)
		{
			GC.StopCancellingGravity();
			GC.RayCast.Free();
			GravityCancellationList.Remove(GC);
		}
	}

	public bool IsEnabled()
	{
		return Light.Enabled;
	}

	public void TurnOff()
	{
		Light.Enabled = false;
		GravityArea.ProcessMode = ProcessModeEnum.Disabled;
	}

	public void TurnOn()
	{
		Light.Enabled = true;
		GravityArea.ProcessMode = ProcessModeEnum.Always;
	}

	public void Toggle()
	{
		if (Light.Enabled)
			TurnOff();
		else TurnOn();
	}

	public void SetEnergy(double energy)
	{
		Light.Energy = (float)Mathf.Remap(energy, 0, 100, 0.2, 1.5);
	}

	public void SetColor(Color color)
	{
		Light.Color = color;
	}

	public override void _Ready()
	{
		base._Ready();

		GravityArea = GetNode<Area2D>("GravityArea");
		Light = GetNode<PointLight2D>("LavityPointLight");
		Light.Color = LavityLightColor;

		if (CollisionMask != 0)
		{
			GravityArea.CollisionMask = CollisionMask;
		}
		if (CollisionLayer != 0)
		{
			GravityArea.CollisionLayer = CollisionLayer;
		}
		if (PointUnitDistance != 0)
		{
			GravityArea.GravityPointUnitDistance = PointUnitDistance;
		}
		GravityArea.BodyEntered += (body) =>
		{
			if (body is CharacterBody2D characterBody)
			{
				RayCast2D RayCast = new()
				{
					GlobalPosition = GlobalPosition
				};
				AddChild(RayCast);
				GravityCancellationList.Add(new GravityCancellation(characterBody, RayCast));
			}
		};
		GravityArea.BodyExited += (body) =>
		{
			// Remove the BodyAndCollisionRaycast whose Body matches the exited body
			if (body is CharacterBody2D characterBody)
			{
				CallDeferred(nameof(RemoveBodyFromCancellationList), characterBody);
			}
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		foreach (GravityCancellation GravityCancel in GravityCancellationList)
		{
			GravityCancel.UpdateRaycastGlobalPosition(GlobalPosition);
			bool ShouldCancelGravity = GravityCancel.IsCollisionBeforeBody();
			if (ShouldCancelGravity)
			{
				GravityCancel.StartCancellingGravity();
			}
			else
			{
				GravityCancel.StopCancellingGravity();
			}
		}

	}

}

public partial class GravityCancellation : Node
{
	public CharacterBody2D Body { get; }
	public RayCast2D RayCast { get; }
	private Area2D GravityOverrideArea;

	public GravityCancellation(CharacterBody2D body, RayCast2D rayCast)
	{
		Body = body;
		RayCast = rayCast;

		// Set the raycast's collision mask to match the body's
		RayCast.CollisionMask = body.CollisionMask;

		// Set the raycast's target to point from the light to the body
		UpdateRaycastTarget();
	}

	// Updates the raycast's target to point from the raycast's position to the body's global position
	private void UpdateRaycastTarget()
	{
		if (Body != null && RayCast != null)
		{
			Vector2 direction = Body.GlobalPosition - RayCast.GlobalPosition;
			RayCast.TargetPosition = direction;
		}
	}

	// Returns true if the raycast collides with something before reaching the body
	public bool IsCollisionBeforeBody()
	{
		if (RayCast == null || Body == null)
			return false;

		// Update the raycast direction in case the body moved
		UpdateRaycastTarget();

		RayCast.ForceRaycastUpdate();

		if (!RayCast.IsColliding())
			return false;

		// Check if the first collision is the body itself
		var collider = RayCast.GetCollider();
		return collider != Body;
	}

	public void UpdateRaycastGlobalPosition(Vector2 newGlobalPos)
	{
		RayCast.GlobalPosition = newGlobalPos;
		UpdateRaycastTarget();
	}
	public void StartCancellingGravity()
	{

		if (GravityOverrideArea == null)
		{

			// Clone the body's collision shape
			var CollisionShape = Body.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (CollisionShape == null)
				return;

			// Create a new Area2D to override gravity
			GravityOverrideArea = new()
			{
				Gravity = 0,
				GravitySpaceOverride = Area2D.SpaceOverride.Replace,
				GlobalPosition = Body.GlobalPosition,
				Priority = 10,
			};
			CollisionShape2D GravityOverrideShape = (CollisionShape2D)CollisionShape.Duplicate();
			GravityOverrideArea.AddChild(GravityOverrideShape);

			// Add to the scene tree as a sibling of the body
			Body.GetParent().AddChild(GravityOverrideArea);

		}
		else
		{
			GravityOverrideArea.GlobalPosition = Body.GlobalPosition;
		}
	}
	public void StopCancellingGravity()
	{
		if (GravityOverrideArea != null)
		{
			GravityOverrideArea.Free();
			GravityOverrideArea = null;
		}
	}
}