using Godot;

public partial class CameraController : Camera2D
{
	[Export] public NodePath TargetPath;
	[Export] public float FollowLerp = 30f;
	[Export] public float ShakeAmplitude = 8f;
	[Export] public float ShakeFrequency = 20f;

	private Node2D _target;
	private FastNoiseLite _noise;
	private float shakeTime = 0f;
	private float shakeDuration = 0f;
	private float shakeStrength = 0f;
	private float noiseSeed = 0f;

	public override void _Ready()
	{
		_target = GetNodeOrNull<Node2D>(TargetPath);
		_noise = new FastNoiseLite
		{
			Seed = (int)GD.Randi(),
			NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
			Frequency = 1f
		};
		noiseSeed = GD.Randf() * 1000f;
	}

	public void Shake(float duration, float strength = 1f)
	{
		if (duration <= 0f)
			return; // Prevent division by zero later

		shakeDuration = duration;
		shakeStrength = strength;
		shakeTime = 0f;
	}

	public override void _Process(double delta)
	{
		if (_target == null)
			return;

		// Base follow
		Position = Position.Lerp(_target.GlobalPosition, (float)(delta * FollowLerp));

		// If shaking, apply noise offset
		if (shakeTime < shakeDuration)
		{
			shakeTime += (float)delta;

			float normalized = shakeDuration > 0f ? shakeTime / shakeDuration : 1f;
			float damping = Mathf.Exp(-3f * normalized);

			float nx = _noise.GetNoise1D(noiseSeed + shakeTime * ShakeFrequency);
			float ny = _noise.GetNoise1D(noiseSeed + 1000f + shakeTime * ShakeFrequency);

			// Ensure noise output isn’t NaN
			if (float.IsNaN(nx) || float.IsNaN(ny))
			{
				GD.PushError($"Noise returned NaN at t={shakeTime}, resetting...");
				nx = ny = 0f;
			}

			Vector2 shakeOffset = new(nx, ny);
			shakeOffset *= shakeStrength * damping * ShakeAmplitude;

			if (!shakeOffset.IsFinite())
			{
				GD.PushError($"Invalid shake offset: {shakeOffset}");
				shakeOffset = Vector2.Zero;
			}

			Position += shakeOffset;
		}
	}
}
