using System.Text;
using Godot;

public partial class DebugMenuController : Control
{
    [ExportGroup("Target Nodes")]
    [Export] public NodePath SunRigPath { get; set; } = new NodePath("../../SunRig");
    [Export] public NodePath EnvironmentPath { get; set; } = new NodePath("../../WorldEnvironment");
    [Export] public NodePath SunVisualPath { get; set; } = new NodePath("../../SunDisc");
    [Export] public NodePath FlashlightPath { get; set; } = new NodePath("../../Player/CameraPivot/Camera3D/Flashlight");
    [Export] public NodePath VhsOverlayPath { get; set; } = new NodePath("../VhsOverlay");
    [Export] public NodePath CameraHudPath { get; set; } = new NodePath("../CameraHud");
    [Export] public NodePath LightingTestObjectsPath { get; set; } = new NodePath("../../LightingTestObjects");
    [Export] public NodePath EntityPath { get; set; } = new NodePath("../../Entities/Entity01");
    [Export] public NodePath DeathScreenPath { get; set; } = new NodePath("../DeathScreen");

    [ExportGroup("Keys")]
    [Export] public Key ToggleMenuKey { get; set; } = Key.D;
    [Export] public Key ToggleNightKey { get; set; } = Key.N;
    [Export] public Key ToggleFlashlightKey { get; set; } = Key.F;
    [Export] public Key ToggleFlashlightShadowsKey { get; set; } = Key.S;
    [Export] public Key ToggleVhsKey { get; set; } = Key.V;
    [Export] public Key ToggleHudKey { get; set; } = Key.H;
    [Export] public Key CycleTimeKey { get; set; } = Key.T;
    [Export] public Key RechargeFlashlightKey { get; set; } = Key.R;
    [Export] public Key ToggleTestObjectsKey { get; set; } = Key.O;
    [Export] public Key CycleEntityStateKey { get; set; } = Key.C;
    [Export] public Key TriggerDeathKey { get; set; } = Key.K;

    [ExportGroup("Debug Behavior")]
    [Export] public bool AutoEnableFlashlightAtNight { get; set; } = true;
    [Export] public bool RecaptureMouseOnClose { get; set; } = true;

    private readonly float[] _timePresets = { 6.5f, 12f, 15.5f, 19.5f, 23.5f };
    private int _timePresetIndex = 2;

    private SunRig? _sunRig;
    private LightingEnvironmentController? _environmentController;
    private Node3D? _sunVisual;
    private FlashlightController? _flashlight;
    private VhsPostProcessController? _vhsOverlay;
    private CamcorderHudController? _cameraHud;
    private Node3D? _lightingTestObjects;
    private EntityChaseController? _entity;
    private DeathScreenController? _deathScreen;
    private Label? _debugText;
    private bool _nightMode;
    private bool _hasCapturedOriginalState;
    private Input.MouseModeEnum _mouseModeBeforeOpen = Input.MouseModeEnum.Captured;
    private SunSnapshot _sunSnapshot;
    private EnvironmentSnapshot _environmentSnapshot;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        ResolveNodes();
        CaptureOriginalState();
        BuildMenu();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (keyEvent.Keycode == ToggleMenuKey && IsCtrlHeld(keyEvent))
        {
            SetMenuVisible(!Visible);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Visible)
        {
            return;
        }

        if (keyEvent.Keycode == Key.Escape)
        {
            SetMenuVisible(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == ToggleNightKey)
        {
            SetNightMode(!_nightMode);
        }
        else if (keyEvent.Keycode == ToggleFlashlightKey)
        {
            _flashlight?.ToggleFlashlight();
        }
        else if (keyEvent.Keycode == ToggleFlashlightShadowsKey && _flashlight != null)
        {
            _flashlight.ShadowsEnabled = !_flashlight.ShadowsEnabled;
        }
        else if (keyEvent.Keycode == ToggleVhsKey && _vhsOverlay != null)
        {
            _vhsOverlay.Enabled = !_vhsOverlay.Enabled;
        }
        else if (keyEvent.Keycode == ToggleHudKey && _cameraHud != null)
        {
            _cameraHud.ShowHud = !_cameraHud.ShowHud;
        }
        else if (keyEvent.Keycode == CycleTimeKey)
        {
            CycleTimeOfDay();
        }
        else if (keyEvent.Keycode == RechargeFlashlightKey)
        {
            _flashlight?.Recharge();
        }
        else if (keyEvent.Keycode == ToggleTestObjectsKey && _lightingTestObjects != null)
        {
            _lightingTestObjects.Visible = !_lightingTestObjects.Visible;
        }
        else if (keyEvent.Keycode == CycleEntityStateKey && _entity != null)
        {
            CycleEntityState();
        }
        else if (keyEvent.Keycode == TriggerDeathKey)
        {
            SetMenuVisible(false);
            _deathScreen?.TriggerDeath();
        }
        else
        {
            return;
        }

        UpdateDebugText();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            UpdateDebugText();
        }
    }

    private void ResolveNodes()
    {
        _sunRig = GetNodeOrNull<SunRig>(SunRigPath);
        _environmentController = GetNodeOrNull<LightingEnvironmentController>(EnvironmentPath);
        _sunVisual = GetNodeOrNull<Node3D>(SunVisualPath);
        _flashlight = GetNodeOrNull<FlashlightController>(FlashlightPath);
        _vhsOverlay = GetNodeOrNull<VhsPostProcessController>(VhsOverlayPath);
        _cameraHud = GetNodeOrNull<CamcorderHudController>(CameraHudPath);
        _lightingTestObjects = GetNodeOrNull<Node3D>(LightingTestObjectsPath);
        _entity = GetNodeOrNull<EntityChaseController>(EntityPath);
        _deathScreen = GetNodeOrNull<DeathScreenController>(DeathScreenPath);
    }

    private void BuildMenu()
    {
        ColorRect panel = new()
        {
            Name = "Panel",
            Position = new Vector2(24f, 118f),
            Size = new Vector2(438f, 288f),
            Color = new Color(0.02f, 0.025f, 0.03f, 0.84f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(panel);

        _debugText = new Label
        {
            Name = "DebugText",
            Position = new Vector2(42f, 136f),
            Size = new Vector2(402f, 252f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        _debugText.AddThemeColorOverride("font_color", new Color(0.88f, 0.95f, 0.84f, 0.95f));
        _debugText.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        _debugText.AddThemeConstantOverride("shadow_offset_x", 2);
        _debugText.AddThemeConstantOverride("shadow_offset_y", 2);
        _debugText.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_debugText);
        UpdateDebugText();
    }

    private void SetMenuVisible(bool menuVisible)
    {
        ResolveNodes();
        Visible = menuVisible;

        if (menuVisible)
        {
            _mouseModeBeforeOpen = Input.MouseMode;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            UpdateDebugText();
            return;
        }

        if (RecaptureMouseOnClose && _mouseModeBeforeOpen == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void SetNightMode(bool enabled)
    {
        ResolveNodes();
        CaptureOriginalState();
        _nightMode = enabled;

        if (enabled)
        {
            ApplyNightState();

            if (AutoEnableFlashlightAtNight)
            {
                _flashlight?.SetFlashlightEnabled(true);
            }

            return;
        }

        RestoreOriginalState();
    }

    private void ApplyNightState()
    {
        if (_sunRig != null)
        {
            _sunRig.UseTimeOfDay = false;
            _sunRig.SunPitchDegrees = 16f;
            _sunRig.NightLightEnergy = 0.025f;
            _sunRig.NightLightColor = new Color(0.18f, 0.23f, 0.42f);
            _sunRig.UpdateSunVisual = false;
        }

        if (_sunVisual != null)
        {
            _sunVisual.Visible = false;
        }

        if (_environmentController != null)
        {
            _environmentController.SkyTopColor = new Color(0.006f, 0.012f, 0.032f);
            _environmentController.SkyHorizonColor = new Color(0.035f, 0.045f, 0.08f);
            _environmentController.GroundHorizonColor = new Color(0.015f, 0.018f, 0.018f);
            _environmentController.SkyEnergy = 0.16f;
            _environmentController.AmbientLightColor = new Color(0.13f, 0.16f, 0.24f);
            _environmentController.AmbientLightEnergy = 0.08f;
            _environmentController.FogColor = new Color(0.05f, 0.065f, 0.08f);
            _environmentController.FogDensity = 0.018f;
            _environmentController.Exposure = 0.72f;
            _environmentController.GlowIntensity = 0.28f;
            _environmentController.GlowStrength = 0.42f;
            _environmentController.Contrast = 1.08f;
            _environmentController.Saturation = 0.72f;
            _environmentController.Brightness = 0.82f;
        }
    }

    private void CycleTimeOfDay()
    {
        ResolveNodes();

        if (_sunRig == null)
        {
            return;
        }

        if (_nightMode)
        {
            SetNightMode(false);
        }

        _timePresetIndex = (_timePresetIndex + 1) % _timePresets.Length;
        _sunRig.UseTimeOfDay = true;
        _sunRig.TimeOfDayHours = _timePresets[_timePresetIndex];
        _sunRig.UpdateSunVisual = _sunSnapshot.UpdateSunVisual;

        if (_sunVisual != null)
        {
            _sunVisual.Visible = _sunSnapshot.SunVisualVisible;
        }
    }

    private void CaptureOriginalState()
    {
        if (_hasCapturedOriginalState)
        {
            return;
        }

        if (_sunRig != null)
        {
            _sunSnapshot = new SunSnapshot
            {
                UseTimeOfDay = _sunRig.UseTimeOfDay,
                TimeOfDayHours = _sunRig.TimeOfDayHours,
                SunPitchDegrees = _sunRig.SunPitchDegrees,
                SunYawDegrees = _sunRig.SunYawDegrees,
                DayLightEnergy = _sunRig.DayLightEnergy,
                NightLightEnergy = _sunRig.NightLightEnergy,
                DayLightColor = _sunRig.DayLightColor,
                NightLightColor = _sunRig.NightLightColor,
                UpdateSunVisual = _sunRig.UpdateSunVisual,
                SunVisualVisible = _sunVisual?.Visible ?? true
            };
        }

        if (_environmentController != null)
        {
            _environmentSnapshot = new EnvironmentSnapshot
            {
                SkyTopColor = _environmentController.SkyTopColor,
                SkyHorizonColor = _environmentController.SkyHorizonColor,
                GroundHorizonColor = _environmentController.GroundHorizonColor,
                SkyEnergy = _environmentController.SkyEnergy,
                AmbientLightColor = _environmentController.AmbientLightColor,
                AmbientLightEnergy = _environmentController.AmbientLightEnergy,
                FogColor = _environmentController.FogColor,
                FogDensity = _environmentController.FogDensity,
                Exposure = _environmentController.Exposure,
                GlowIntensity = _environmentController.GlowIntensity,
                GlowStrength = _environmentController.GlowStrength,
                Contrast = _environmentController.Contrast,
                Saturation = _environmentController.Saturation,
                Brightness = _environmentController.Brightness
            };
        }

        _hasCapturedOriginalState = true;
    }

    private void RestoreOriginalState()
    {
        if (_sunRig != null)
        {
            _sunRig.UseTimeOfDay = _sunSnapshot.UseTimeOfDay;
            _sunRig.TimeOfDayHours = _sunSnapshot.TimeOfDayHours;
            _sunRig.SunPitchDegrees = _sunSnapshot.SunPitchDegrees;
            _sunRig.SunYawDegrees = _sunSnapshot.SunYawDegrees;
            _sunRig.DayLightEnergy = _sunSnapshot.DayLightEnergy;
            _sunRig.NightLightEnergy = _sunSnapshot.NightLightEnergy;
            _sunRig.DayLightColor = _sunSnapshot.DayLightColor;
            _sunRig.NightLightColor = _sunSnapshot.NightLightColor;
            _sunRig.UpdateSunVisual = _sunSnapshot.UpdateSunVisual;
        }

        if (_sunVisual != null)
        {
            _sunVisual.Visible = _sunSnapshot.SunVisualVisible;
        }

        if (_environmentController != null)
        {
            _environmentController.SkyTopColor = _environmentSnapshot.SkyTopColor;
            _environmentController.SkyHorizonColor = _environmentSnapshot.SkyHorizonColor;
            _environmentController.GroundHorizonColor = _environmentSnapshot.GroundHorizonColor;
            _environmentController.SkyEnergy = _environmentSnapshot.SkyEnergy;
            _environmentController.AmbientLightColor = _environmentSnapshot.AmbientLightColor;
            _environmentController.AmbientLightEnergy = _environmentSnapshot.AmbientLightEnergy;
            _environmentController.FogColor = _environmentSnapshot.FogColor;
            _environmentController.FogDensity = _environmentSnapshot.FogDensity;
            _environmentController.Exposure = _environmentSnapshot.Exposure;
            _environmentController.GlowIntensity = _environmentSnapshot.GlowIntensity;
            _environmentController.GlowStrength = _environmentSnapshot.GlowStrength;
            _environmentController.Contrast = _environmentSnapshot.Contrast;
            _environmentController.Saturation = _environmentSnapshot.Saturation;
            _environmentController.Brightness = _environmentSnapshot.Brightness;
        }
    }

    private void UpdateDebugText()
    {
        if (_debugText == null)
        {
            return;
        }

        StringBuilder text = new();
        text.AppendLine("DEBUG MENU");
        text.AppendLine("Ctrl+D close   Esc close");
        text.AppendLine();
        text.AppendLine($"N  Night Mode: {OnOff(_nightMode)}");
        text.AppendLine($"F  Flashlight: {GetFlashlightStatus()}");
        text.AppendLine($"S  Flashlight Shadows: {OnOff(_flashlight?.ShadowsEnabled ?? false)}");
        text.AppendLine($"R  Recharge Flashlight");
        text.AppendLine($"V  VHS: {OnOff(_vhsOverlay?.Enabled ?? false)}");
        text.AppendLine($"H  Camcorder HUD: {OnOff(_cameraHud?.ShowHud ?? false)}");
        text.AppendLine($"T  Time Preset: {GetTimeStatus()}");
        text.AppendLine($"O  Test Objects: {OnOff(_lightingTestObjects?.Visible ?? false)}");
        text.AppendLine($"C  Entity State: {GetEntityStatus()}");
        text.AppendLine("K  Test Death Scene");

        _debugText.Text = text.ToString();
    }

    private string GetFlashlightStatus()
    {
        if (_flashlight == null)
        {
            return "MISSING";
        }

        return $"{OnOff(_flashlight.IsFlashlightEnabled)}  battery {_flashlight.BatteryPercent:0}%";
    }

    private string GetTimeStatus()
    {
        if (_sunRig == null)
        {
            return "MISSING";
        }

        return _sunRig.UseTimeOfDay ? $"{_sunRig.TimeOfDayHours:00.0}h" : "fixed sun";
    }

    private void CycleEntityState()
    {
        if (_entity == null)
        {
            return;
        }

        EntityChaseController.EntityState nextState = _entity.State switch
        {
            EntityChaseController.EntityState.Idle => EntityChaseController.EntityState.Wander,
            EntityChaseController.EntityState.Wander => EntityChaseController.EntityState.Chase,
            _ => EntityChaseController.EntityState.Idle
        };

        _entity.SetState(nextState);
    }

    private string GetEntityStatus()
    {
        return _entity == null ? "MISSING" : _entity.State.ToString().ToUpperInvariant();
    }

    private static bool IsCtrlHeld(InputEventKey keyEvent)
    {
        return keyEvent.CtrlPressed || keyEvent.MetaPressed;
    }

    private static string OnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private struct SunSnapshot
    {
        public bool UseTimeOfDay;
        public float TimeOfDayHours;
        public float SunPitchDegrees;
        public float SunYawDegrees;
        public float DayLightEnergy;
        public float NightLightEnergy;
        public Color DayLightColor;
        public Color NightLightColor;
        public bool UpdateSunVisual;
        public bool SunVisualVisible;
    }

    private struct EnvironmentSnapshot
    {
        public Color SkyTopColor;
        public Color SkyHorizonColor;
        public Color GroundHorizonColor;
        public float SkyEnergy;
        public Color AmbientLightColor;
        public float AmbientLightEnergy;
        public Color FogColor;
        public float FogDensity;
        public float Exposure;
        public float GlowIntensity;
        public float GlowStrength;
        public float Contrast;
        public float Saturation;
        public float Brightness;
    }
}
