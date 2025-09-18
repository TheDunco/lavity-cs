using Godot;
using System;
using System.Drawing;

public partial class LavityLight : Node2D
{
	private Area2D GravityArea = null;
	private PointLight2D LavityPointLight = null;
	public override void _Ready()
	{
		base._Ready();
		GravityArea = GetNode<Area2D>("GravityArea");
		LavityPointLight = GetNode<PointLight2D>("LavityPointLight");
	}
}
