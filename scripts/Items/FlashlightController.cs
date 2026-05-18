using Godot;

public partial class FlashlightController : SpotLight3D
{
	[Export] public bool StartsEnabled { get; set; } = true;
	[Export] public bool ToggleEnabled { get; set; } = true;
	[Export] public Key ToggleKey { get; set; } = Key.F;
	[Export] public NodePath VisualModelPath { get; set; } = new NodePath("../FlashlightBody");

	[ExportGroup("Beam")]
	[Export(PropertyHint.Range, "0,12,0.05")] public float BeamEnergy { get; set; } = 4.8f;
	[Export(PropertyHint.Range, "1,60,0.5")] public float BeamRange { get; set; } = 26f;
	[Export(PropertyHint.Range, "5,90,1")] public float BeamAngleDegrees { get; set; } = 34f;
	[Export(PropertyHint.Range, "0.1,24,0.1")] public float EnergyResponse { get; set; } = 10f;
	[Export] public Color BeamColor { get; set; } = new Color(1f, 0.92f, 0.76f);
	[ExportGroup("Shadows")]
	[Export] public bool ShadowsEnabled { get; set; } = true;
	[Export(PropertyHint.Range, "0,2,0.01")] public float ShadowStrength { get; set; } = 1f;
	[Export(PropertyHint.Range, "0,0.2,0.001")] public float ShadowBiasAmount { get; set; } = 0.006f;
	[Export(PropertyHint.Range, "0,10,0.01")] public float ShadowNormalBiasAmount { get; set; } = 0.45f;
	[Export(PropertyHint.Range, "0,8,0.05")] public float ShadowBlurAmount { get; set; } = 0.15f;
	[Export(PropertyHint.Layers3DRender)] public uint AffectedVisualLayers { get; set; } = 1;
	[Export(PropertyHint.Layers3DRender)] public uint ShadowCasterLayers { get; set; } = 1;
	[Export] public bool ForceSceneGeometryToCastShadows { get; set; } = true;
	[Export(PropertyHint.Range, "0.1,5,0.1")] public float ShadowCasterRefreshSeconds { get; set; } = 1f;

	[ExportGroup("Visual Equip")]
	[Export] public bool AnimateVisualModel { get; set; } = true;
	[Export(PropertyHint.Range, "0.05,1.5,0.01")] public float PullAnimationSeconds { get; set; } = 0.38f;
	[Export] public Vector3 HiddenVisualOffset { get; set; } = new Vector3(0.12f, -0.44f, 0.32f);
	[Export] public Vector3 HiddenVisualRotationDegrees { get; set; } = new Vector3(10f, -18f, -8f);

	[ExportGroup("Battery")]
	[Export] public bool UseBattery { get; set; } = true;
	[Export(PropertyHint.Range, "0,1,0.01")] public float BatteryLevel { get; set; } = 1f;
	[Export(PropertyHint.Range, "0,0.1,0.0005")] public float BatteryDrainPerSecond { get; set; } = 0.0045f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float LowBatteryThreshold { get; set; } = 0.18f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float LowBatteryFlickerStrength { get; set; } = 0.38f;

	private bool _isOn;
	private double _elapsedSeconds;
	private double _nextShadowCasterRefresh;
	private Node3D? _visualModel;
	private Transform3D _visibleVisualTransform;
	private Transform3D _hiddenVisualTransform;
	private Tween? _visualTween;

	public bool IsFlashlightEnabled => _isOn;
	public float BatteryPercent => Mathf.Clamp(BatteryLevel, 0f, 1f) * 100f;

	public override void _Ready()
	{
		ResolveVisualModel();
		ConfigureBeam();
		ForceSceneShadowCasters();
		SetFlashlightEnabled(StartsEnabled && HasCharge(), animateVisual: false);
		LightEnergy = _isOn ? BeamEnergy : 0f;
		Visible = _isOn;
		SetVisualImmediate(_isOn);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& !keyEvent.Echo
			&& keyEvent.Keycode == ToggleKey
			&& ToggleEnabled
			&& Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			ToggleFlashlight();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Process(double delta)
	{
		float frameDelta = (float)delta;
		_elapsedSeconds += delta;
		ConfigureBeam();
		RefreshShadowCastersIfNeeded();

		if (_isOn && UseBattery)
		{
			BatteryLevel = Mathf.Max(0f, BatteryLevel - BatteryDrainPerSecond * frameDelta);

			if (!HasCharge())
			{
				SetFlashlightEnabled(false);
			}
		}

		float desiredEnergy = _isOn ? BeamEnergy * GetBatteryEnergyMultiplier() : 0f;
		float energyStep = Mathf.Max(EnergyResponse, 0.1f) * Mathf.Max(BeamEnergy, 1f) * frameDelta;
		LightEnergy = Mathf.MoveToward(LightEnergy, desiredEnergy, energyStep);
		Visible = _isOn || LightEnergy > 0.02f;
	}

	public void ToggleFlashlight()
	{
		SetFlashlightEnabled(!_isOn);
	}

	public void SetFlashlightEnabled(bool enabled)
	{
		SetFlashlightEnabled(enabled, animateVisual: true);
	}

	public void SetFlashlightEnabled(bool enabled, bool animateVisual)
	{
		_isOn = enabled && HasCharge();
		if (animateVisual)
		{
			AnimateVisualTo(_isOn);
		}
		else
		{
			SetVisualImmediate(_isOn);
		}
	}

	public void Recharge()
	{
		BatteryLevel = 1f;
	}

	private void ConfigureBeam()
	{
		LightColor = BeamColor;
		LightCullMask = AffectedVisualLayers;
		SpotRange = BeamRange;
		SpotAngle = BeamAngleDegrees;
		ShadowEnabled = ShadowsEnabled;
		ShadowOpacity = ShadowStrength;
		ShadowBias = ShadowBiasAmount;
		ShadowNormalBias = ShadowNormalBiasAmount;
		ShadowBlur = ShadowBlurAmount;
		ShadowCasterMask = ShadowCasterLayers;
	}

	private void ResolveVisualModel()
	{
		_visualModel = GetNodeOrNull<Node3D>(VisualModelPath);
		if (_visualModel == null)
		{
			return;
		}

		_visibleVisualTransform = _visualModel.Transform;
		_hiddenVisualTransform = _visibleVisualTransform;
		_hiddenVisualTransform.Origin += HiddenVisualOffset;
		Basis hiddenRotation = _hiddenVisualTransform.Basis
			.Rotated(Vector3.Right, Mathf.DegToRad(HiddenVisualRotationDegrees.X))
			.Rotated(Vector3.Up, Mathf.DegToRad(HiddenVisualRotationDegrees.Y))
			.Rotated(Vector3.Forward, Mathf.DegToRad(HiddenVisualRotationDegrees.Z));
		_hiddenVisualTransform.Basis = hiddenRotation;
	}

	private void SetVisualImmediate(bool pulledOut)
	{
		if (!AnimateVisualModel || _visualModel == null)
		{
			return;
		}

		_visualTween?.Kill();
		_visualModel.Visible = pulledOut;
		_visualModel.Transform = pulledOut ? _visibleVisualTransform : _hiddenVisualTransform;
	}

	private void AnimateVisualTo(bool pulledOut)
	{
		if (!AnimateVisualModel || _visualModel == null)
		{
			return;
		}

		_visualTween?.Kill();
		if (pulledOut && !_visualModel.Visible)
		{
			_visualModel.Transform = _hiddenVisualTransform;
			_visualModel.Visible = true;
		}

		_visualTween = CreateTween();
		_visualTween.SetParallel(true);
		_visualTween.TweenProperty(_visualModel, Node3D.PropertyName.Transform.ToString(), pulledOut ? _visibleVisualTransform : _hiddenVisualTransform, PullAnimationSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(pulledOut ? Tween.EaseType.Out : Tween.EaseType.In);
		_visualTween.SetParallel(false);

		if (!pulledOut)
		{
			_visualTween.TweenCallback(Callable.From(() =>
			{
				if (_visualModel != null && !_isOn)
				{
					_visualModel.Visible = false;
				}
			}));
		}
	}

	private void RefreshShadowCastersIfNeeded()
	{
		if (!ForceSceneGeometryToCastShadows || _elapsedSeconds < _nextShadowCasterRefresh)
		{
			return;
		}

		_nextShadowCasterRefresh = _elapsedSeconds + Mathf.Max(ShadowCasterRefreshSeconds, 0.1f);
		ForceSceneShadowCasters();
	}

	private void ForceSceneShadowCasters()
	{
		if (!ForceSceneGeometryToCastShadows)
		{
			return;
		}

		Node? root = GetTree()?.CurrentScene;
		if (root == null)
		{
			return;
		}

		ForceShadowCastingRecursive(root);
	}

	private void ForceShadowCastingRecursive(Node node)
	{
		if (node is GeometryInstance3D geometry)
		{
			geometry.Layers |= ShadowCasterLayers;
			geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		}

		foreach (Node child in node.GetChildren())
		{
			ForceShadowCastingRecursive(child);
		}
	}

	private bool HasCharge()
	{
		return !UseBattery || BatteryLevel > 0.001f;
	}

	private float GetBatteryEnergyMultiplier()
	{
		if (!UseBattery)
		{
			return 1f;
		}

		float charge = Mathf.Clamp(BatteryLevel, 0f, 1f);
		float lowThreshold = Mathf.Max(LowBatteryThreshold, 0.001f);

		if (charge > lowThreshold)
		{
			return 1f;
		}

		float lowRatio = charge / lowThreshold;
		float flickerDepth = (1f - lowRatio) * LowBatteryFlickerStrength;
		float time = (float)_elapsedSeconds;
		float unevenWave = 0.5f + 0.5f * Mathf.Sin(time * 38f + Mathf.Sin(time * 6f) * 3f);
		float stutter = Mathf.Sin(time * 93f) > 0.94f ? 0.45f : 1f;

		return Mathf.Clamp((1f - unevenWave * flickerDepth) * stutter, 0.2f, 1f);
	}
}
