using Godot;

public partial class VhsPostProcessController : ColorRect
{
    [Export] public bool Enabled { get; set; } = true;

    [ExportGroup("VHS Shader")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float Intensity { get; set; } = 0.86f;
    [Export] public Vector2 TapeResolution { get; set; } = new Vector2(360f, 240f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float PixelationStrength { get; set; } = 0.82f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ScanlineStrength { get; set; } = 0.42f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float NoiseStrength { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ChromaticAberration { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RandomJitterStrength { get; set; } = 0.28f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float WobbleStrength { get; set; } = 0.48f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float TrackingStrength { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float WhiteLineStrength { get; set; } = 0.64f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float WhiteLineFrequency { get; set; } = 0.52f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DamageFrequency { get; set; } = 0.46f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DropoutStrength { get; set; } = 0.22f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BottomTearStrength { get; set; } = 0.38f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float GlareStrength { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "0.2,2,0.01")] public float GlareThreshold { get; set; } = 0.82f;
    [Export(PropertyHint.Range, "1,96,1")] public float GlareSpread { get; set; } = 42f;
    [Export(PropertyHint.Range, "-0.3,0.3,0.005")] public float BarrelDistortion { get; set; } = 0.025f;
    [Export(PropertyHint.Range, "0.5,1.8,0.01")] public float SceneBrightness { get; set; } = 1.08f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float SceneLift { get; set; } = 0.12f;
    [Export(PropertyHint.Range, "0.5,1.5,0.01")] public float CameraContrast { get; set; } = 1.04f;
    [Export(PropertyHint.Range, "0,1.5,0.01")] public float CameraSaturation { get; set; } = 0.78f;
    [Export] public Color TapeTint { get; set; } = new Color(1f, 0.88f, 0.64f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float TintStrength { get; set; } = 0.16f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float VignetteStrength { get; set; } = 0.32f;

    private ShaderMaterial? _shaderMaterial;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        if (Material is ShaderMaterial shaderMaterial)
        {
            _shaderMaterial = shaderMaterial.Duplicate() as ShaderMaterial;
            Material = _shaderMaterial;
        }

        ApplyShaderParameters();
    }

    public override void _Process(double delta)
    {
        ApplyShaderParameters();
    }

    private void ApplyShaderParameters()
    {
        Visible = Enabled;

        if (_shaderMaterial == null)
        {
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        _shaderMaterial.SetShaderParameter("intensity", Intensity);
        _shaderMaterial.SetShaderParameter("tape_resolution", TapeResolution);
        _shaderMaterial.SetShaderParameter("pixelation_strength", PixelationStrength);
        _shaderMaterial.SetShaderParameter("scanline_strength", ScanlineStrength);
        _shaderMaterial.SetShaderParameter("noise_strength", NoiseStrength);
        _shaderMaterial.SetShaderParameter("chromatic_aberration", ChromaticAberration);
        _shaderMaterial.SetShaderParameter("random_jitter_strength", RandomJitterStrength);
        _shaderMaterial.SetShaderParameter("wobble_strength", WobbleStrength);
        _shaderMaterial.SetShaderParameter("tracking_strength", TrackingStrength);
        _shaderMaterial.SetShaderParameter("white_line_strength", WhiteLineStrength);
        _shaderMaterial.SetShaderParameter("white_line_frequency", WhiteLineFrequency);
        _shaderMaterial.SetShaderParameter("damage_frequency", DamageFrequency);
        _shaderMaterial.SetShaderParameter("dropout_strength", DropoutStrength);
        _shaderMaterial.SetShaderParameter("bottom_tear_strength", BottomTearStrength);
        _shaderMaterial.SetShaderParameter("glare_strength", GlareStrength);
        _shaderMaterial.SetShaderParameter("glare_threshold", GlareThreshold);
        _shaderMaterial.SetShaderParameter("glare_spread", GlareSpread);
        _shaderMaterial.SetShaderParameter("barrel_distortion", BarrelDistortion);
        _shaderMaterial.SetShaderParameter("scene_brightness", SceneBrightness);
        _shaderMaterial.SetShaderParameter("scene_lift", SceneLift);
        _shaderMaterial.SetShaderParameter("camera_contrast", CameraContrast);
        _shaderMaterial.SetShaderParameter("camera_saturation", CameraSaturation);
        _shaderMaterial.SetShaderParameter("tape_tint", TapeTint);
        _shaderMaterial.SetShaderParameter("tint_strength", TintStrength);
        _shaderMaterial.SetShaderParameter("vignette_strength", VignetteStrength);
        _shaderMaterial.SetShaderParameter("resolution", viewportSize);
    }
}
