using Godot;

public partial class Consumable : RigidBody2D
{
	private PlantEffect Effect;
	public Consumable(PlantEffect effect)
	{
		Effect = effect;
	}

	public Consumable()
	{
	}

	public void SetEffect(PlantEffect effect)
	{
		Effect = effect;
	}

	public virtual PlantEffect OnConsume()
	{
		this.QueueFree();
		return Effect;
	}
}

