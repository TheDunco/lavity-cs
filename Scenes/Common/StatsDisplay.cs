using Godot;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

public partial class StatsDisplay : CanvasLayer
{
	[Required]
	[Export] private Player Player;
	private TextureProgressBar HealthProgress = null;
	private TextureProgressBar EnergyProgress = null;
	private TextureProgressBar StomachProgress = null;
	private HBoxContainer StomachContentsContainer = null;
	private VBoxContainer StatsScreen = null;
	private VBoxContainer DeathScreen = null;
	private Button ReviveButton = null;

	public override void _Ready()
	{
		base._Ready();

		DeathScreen = GetNode<VBoxContainer>("DeathScreen");
		Debug.Assert(DeathScreen != null);
		StatsScreen = GetNode<VBoxContainer>("StatsScreen");
		Debug.Assert(StatsScreen != null);

		ReviveButton = GetNode<Button>("DeathScreen/Revive");
		Debug.Assert(ReviveButton != null);
		ReviveButton.Pressed += Player.Revive;

		StomachProgress = GetNode<TextureProgressBar>("StatsScreen/HBoxContainer/StomachProgress");
		EnergyProgress = GetNode<TextureProgressBar>("StatsScreen/HBoxContainer/EnergyProgress");
		HealthProgress = GetNode<TextureProgressBar>("StatsScreen/HBoxContainer/HealthProgress");
		Debug.Assert(StomachProgress != null);
		Debug.Assert(EnergyProgress != null);
		Debug.Assert(HealthProgress != null);

		HealthProgress.MaxValue = Player.MaxHealth;
		EnergyProgress.MaxValue = Player.MaxEnergy;
		StomachProgress.MaxValue = Player.MaxStomachSpace;

		StomachContentsContainer = GetNode<HBoxContainer>("StatsScreen/HBoxContainer/StomachContentsContainer");

		var statsManager = GetNode<StatsManager>("/root/StatsManager");
		statsManager.StatsTick += UpdateStomachContentsDisplay;
	}

	public void SwitchToDeathScreen()
	{
		DeathScreen.Visible = true;
		StatsScreen.Visible = false;
	}

	public void SwitchToStatsDisplay()
	{
		DeathScreen.Visible = false;
		StatsScreen.Visible = true;
	}

	public void Quit()
	{
		GetTree().Quit();
	}

	public void UpdateStomachContentsDisplay()
	{
		foreach (Node n in StomachContentsContainer.GetChildren())
		{
			StomachContentsContainer.RemoveChild(n);
		}
		foreach (Consumable c in Player.GetStomachConsumables())
		{
			AddConsumableToStomachContents(c);
		}
	}

	private void AddConsumableToStomachContents(Consumable consumable)
	{
		if (!IsInstanceValid(consumable))
		{
			return;
		}

		MarginContainer container = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		container.AddThemeConstantOverride("margin_left", 20);
		container.AddThemeConstantOverride("margin_right", 20);
		Consumable newInstance = (Consumable)consumable.Duplicate();
		newInstance.Position = Vector2.Zero;
		newInstance.Visible = true;
		container.AddChild(newInstance);

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
		catch (Exception) { GD.PushError($"Didn't find child in stats display to remove at index {indexToRemove}"); }
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		HealthProgress.Value = Player.Health;
		EnergyProgress.Value = Player.Energy;
		StomachProgress.Value = Player.Fullness;
	}

}
