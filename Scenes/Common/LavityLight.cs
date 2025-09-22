using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class BodyAndCollisionRaycast : Node
{
	public CharacterBody2D Body { get; }
	private RayCast2D RayCast;

	public BodyAndCollisionRaycast(CharacterBody2D body, RayCast2D rayCast)
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
}

public partial class LavityLight : Node2D
{
	private Area2D GravityArea = null;
	private List<BodyAndCollisionRaycast> GravityCancellationList = [];

	private void RemoveBodyFromCancellationList(CharacterBody2D characterBody)
	{
		//TODO: Remove the raycast as a child as well
		GravityCancellationList.RemoveAll(b => b != null && b.Body == characterBody);
	}

	public override void _Ready()
	{
		base._Ready();

		GravityArea = GetNode<Area2D>("GravityArea");
		GravityArea.BodyEntered += (body) =>
		{
			if (body is CharacterBody2D characterBody)
			{
				RayCast2D RayCast = new()
				{
					GlobalPosition = GlobalPosition
				};
				AddChild(RayCast);
				GravityCancellationList.Add(new BodyAndCollisionRaycast(characterBody, RayCast));
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

		foreach (BodyAndCollisionRaycast BandR in GravityCancellationList)
		{
			BandR.UpdateRaycastGlobalPosition(GlobalPosition);
			bool ShouldCancelGravity = BandR.IsCollisionBeforeBody();
			GD.Print("Should cancel gravity?", ShouldCancelGravity);
			// TODO: Cancel gravity for this particular Body from this particular LavityLight
		}

	}

}
