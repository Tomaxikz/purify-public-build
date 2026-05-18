using Godot;

[Tool]
public partial class SunRig : Node3D
{
    [Export] public NodePath DirectionalLightPath { get; set; } = new NodePath("../Sun");
    [Export] public NodePath SunVisualPath { get; set; } = new NodePath("../SunDisc");
    [Export] public NodePath VisualOriginPath { get; set; } = new NodePath("../Player/CameraPivot");

    [ExportGroup("Sun Angle")]
    [Export] public bool UseTimeOfDay { get; set; } = false;
    [Export(PropertyHint.Range, "0,24,0.1")] public float TimeOfDayHours { get; set; } = 15.5f;
    [Export(PropertyHint.Range, "-90,90,0.5")] public float SunPitchDegrees { get; set; } = -42f;
    [Export(PropertyHint.Range, "-180,180,0.5")] public float SunYawDegrees { get; set; } = 160f;
    [Export(PropertyHint.Range, "5,85,0.5")] public float MaxSunElevationDegrees { get; set; } = 68f;

    [ExportGroup("Light")]
    [Export(PropertyHint.Range, "0,8,0.05")] public float DayLightEnergy { get; set; } = 2.6f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float NightLightEnergy { get; set; } = 0.08f;
    [Export] public Color DayLightColor { get; set; } = new Color(1f, 0.92f, 0.78f);
    [Export] public Color NightLightColor { get; set; } = new Color(0.34f, 0.42f, 0.62f);
    [Export] public bool UpdateContinuously { get; set; } = true;

    [ExportGroup("Visible Sun")]
    [Export] public bool UpdateSunVisual { get; set; } = true;
    [Export(PropertyHint.Range, "10,250,1")] public float SunVisualDistance { get; set; } = 58f;

    private DirectionalLight3D? _light;
    private Node3D? _sunVisual;
    private Node3D? _visualOrigin;

    public override void _Ready()
    {
        ResolveNodes();
        ApplySun();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() || UpdateContinuously)
        {
            ResolveNodes();
            ApplySun();
        }
    }

    private void ResolveNodes()
    {
        if (_light == null || !GodotObject.IsInstanceValid(_light))
        {
            _light = GetNodeOrNull<DirectionalLight3D>(DirectionalLightPath);
        }

        if (_sunVisual == null || !GodotObject.IsInstanceValid(_sunVisual))
        {
            _sunVisual = GetNodeOrNull<Node3D>(SunVisualPath);
        }

        if (_visualOrigin == null || !GodotObject.IsInstanceValid(_visualOrigin))
        {
            _visualOrigin = GetNodeOrNull<Node3D>(VisualOriginPath);
        }
    }

    private void ApplySun()
    {
        if (_light == null)
        {
            return;
        }

        float pitchDegrees = GetActivePitchDegrees();
        _light.RotationDegrees = new Vector3(pitchDegrees, SunYawDegrees, 0f);

        float daylightAmount = Mathf.Clamp(-pitchDegrees / Mathf.Max(MaxSunElevationDegrees, 1f), 0f, 1f);
        _light.LightEnergy = Mathf.Lerp(NightLightEnergy, DayLightEnergy, daylightAmount);
        _light.LightColor = NightLightColor.Lerp(DayLightColor, daylightAmount);

        UpdateVisibleSun();
    }

    private void UpdateVisibleSun()
    {
        if (!UpdateSunVisual || _light == null || _sunVisual == null)
        {
            return;
        }

        Vector3 origin = _visualOrigin?.GlobalPosition ?? Vector3.Zero;
        Vector3 sourceDirection = _light.GlobalTransform.Basis.Z.Normalized();
        _sunVisual.GlobalPosition = origin + sourceDirection * SunVisualDistance;
        _sunVisual.LookAt(origin, Vector3.Up);
    }

    private float GetActivePitchDegrees()
    {
        if (!UseTimeOfDay)
        {
            return SunPitchDegrees;
        }

        float normalizedTime = Mathf.PosMod(TimeOfDayHours, 24f) / 24f;
        float orbitRadians = (normalizedTime - 0.25f) * Mathf.Tau;
        return -Mathf.Sin(orbitRadians) * MaxSunElevationDegrees;
    }
}
