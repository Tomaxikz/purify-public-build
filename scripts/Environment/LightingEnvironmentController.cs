using Godot;

[Tool]
public partial class LightingEnvironmentController : WorldEnvironment
{
	[ExportGroup("Sky")]
	[Export] public Color SkyTopColor { get; set; } = new Color(0.31f, 0.42f, 0.52f);
	[Export] public Color SkyHorizonColor { get; set; } = new Color(0.73f, 0.68f, 0.58f);
	[Export] public Color GroundHorizonColor { get; set; } = new Color(0.28f, 0.30f, 0.28f);
	[Export(PropertyHint.Range, "0,4,0.05")] public float SkyEnergy { get; set; } = 0.85f;

	[ExportGroup("Ambient")]
	[Export] public Color AmbientLightColor { get; set; } = new Color(0.55f, 0.61f, 0.62f);
	[Export(PropertyHint.Range, "0,4,0.05")] public float AmbientLightEnergy { get; set; } = 0.55f;

	[ExportGroup("Fog And Exposure")]
	[Export] public bool FogEnabled { get; set; } = true;
	[Export] public Color FogColor { get; set; } = new Color(0.55f, 0.58f, 0.54f);
	[Export(PropertyHint.Range, "0,0.08,0.001")] public float FogDensity { get; set; } = 0.012f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float FogSkyAffect { get; set; } = 0.45f;
	[Export(PropertyHint.Range, "0.1,4,0.05")] public float Exposure { get; set; } = 1.05f;

	[ExportGroup("Glow")]
	[Export] public bool GlowEnabled { get; set; } = true;
	[Export(PropertyHint.Range, "0,2,0.01")] public float GlowIntensity { get; set; } = 0.55f;
	[Export(PropertyHint.Range, "0,2,0.01")] public float GlowStrength { get; set; } = 0.72f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float GlowBloom { get; set; } = 0.08f;
	[Export(PropertyHint.Range, "0,4,0.05")] public float GlowHdrThreshold { get; set; } = 0.78f;
	[Export(PropertyHint.Range, "0,8,0.05")] public float GlowHdrScale { get; set; } = 2.0f;

	[ExportGroup("Color Grade")]
	[Export(PropertyHint.Range, "0.5,1.5,0.01")] public float Contrast { get; set; } = 1.04f;
	[Export(PropertyHint.Range, "0.5,1.5,0.01")] public float Saturation { get; set; } = 0.88f;
	[Export(PropertyHint.Range, "0.5,1.5,0.01")] public float Brightness { get; set; } = 1.0f;
	[Export] public bool ApplyContinuously { get; set; } = true;

	public override void _Ready()
	{
		ApplyEnvironment();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || ApplyContinuously)
		{
			ApplyEnvironment();
		}
	}

	private void ApplyEnvironment()
	{
		Godot.Environment environment = Environment ?? new Godot.Environment();
		Environment = environment;

		environment.BackgroundMode = Godot.Environment.BGMode.Sky;
		environment.Sky = EnsureSky(environment.Sky);
		environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		environment.AmbientLightColor = AmbientLightColor;
		environment.AmbientLightEnergy = AmbientLightEnergy;
		environment.FogEnabled = FogEnabled;
		environment.FogLightColor = FogColor;
		environment.FogDensity = FogDensity;
		environment.FogSkyAffect = FogSkyAffect;
		environment.TonemapExposure = Exposure;
		environment.GlowEnabled = GlowEnabled;
		environment.GlowIntensity = GlowIntensity;
		environment.GlowStrength = GlowStrength;
		environment.GlowBloom = GlowBloom;
		environment.GlowHdrThreshold = GlowHdrThreshold;
		environment.GlowHdrScale = GlowHdrScale;
		environment.GlowHdrLuminanceCap = 12f;
		environment.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Screen;
		environment.AdjustmentEnabled = true;
		environment.AdjustmentContrast = Contrast;
		environment.AdjustmentSaturation = Saturation;
		environment.AdjustmentBrightness = Brightness;
	}

	private Sky EnsureSky(Sky? existingSky)
	{
		Sky sky = existingSky ?? new Sky();

		if (sky.SkyMaterial is not ProceduralSkyMaterial skyMaterial)
		{
			skyMaterial = new ProceduralSkyMaterial();
			sky.SkyMaterial = skyMaterial;
		}

		skyMaterial.SkyTopColor = SkyTopColor;
		skyMaterial.SkyHorizonColor = SkyHorizonColor;
		skyMaterial.GroundHorizonColor = GroundHorizonColor;
		skyMaterial.GroundBottomColor = GroundHorizonColor.Darkened(0.2f);
		skyMaterial.SkyEnergyMultiplier = SkyEnergy;

		return sky;
	}
}
