using Godot;

public partial class Projectile : RigidBody2D
{
	public override void _Ready()
	{
		base._Ready();
		ContactMonitor = true;
		MaxContactsReported = 5;
		BodyEntered += body => { if (body is Creature creature && creature is not Player) { creature.Kill(); QueueFree(); } };
	}

	public void SetLifetime(int seconds)
	{
		Timer timer = new() { WaitTime = seconds };
		timer.Timeout += () => { QueueFree(); };
		AddChild(timer);
		timer.Start();
	}
}
