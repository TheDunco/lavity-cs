using Godot;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

public partial class StatsDisplay : CanvasLayer
{
	[Required]
	[Export] private Player Player;
	private TextureProgressBar HealthProgress = null;
	private TextureProgressBar EnergyProgress = null;
	private TextureProgressBar StomachProgress = null;
	private HBoxContainer StomachContentsContainer = null;

	public override void _Ready()
	{
		base._Ready();
		StomachProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/StomachProgress");
		EnergyProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/EnergyProgress");
		HealthProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/HealthProgress");

		HealthProgress.MaxValue = Player.MaxHealth;
		EnergyProgress.MaxValue = Player.MaxEnergy;
		StomachProgress.MaxValue = Player.MaxStomachSpace;

		StomachContentsContainer = GetNode<HBoxContainer>("VBoxContainer/HBoxContainer/StomachContentsContainer");
	}

	public void AddSpriteToStomachContents(Sprite2D stomachTextureSprite)
	{
		MarginContainer container = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		container.AddThemeConstantOverride("margin_left", 20);
		container.AddThemeConstantOverride("margin_right", 20);
		container.AddChild(stomachTextureSprite.Duplicate());

		StomachContentsContainer.AddChild(container);
	}
	public void RemoveStomachContents(int indexToRemove)
	{
		var spriteContainers = StomachContentsContainer.GetChildren();
		try
		{

			if (!spriteContainers[indexToRemove].IsQueuedForDeletion())
			{
				spriteContainers[indexToRemove].QueueFree();
			}
		}
		catch (Exception _) { GD.PushError("Didn't find child in stats display to remove"); }
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		HealthProgress.Value = Player.Health;
		EnergyProgress.Value = Player.Energy;
		StomachProgress.Value = Player.Fullness;
	}

}
