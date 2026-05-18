using Godot;

public partial class DeathScreenController : Control
{
    private enum DeathPhase
    {
        Hidden,
        Transition,
        Dead
    }

    [Export] public NodePath CameraShakeTargetPath { get; set; } = new NodePath("../../Player/CameraPivot");
    [Export] public NodePath VhsOverlayPath { get; set; } = new NodePath("../VhsOverlay");

    [ExportGroup("Timing")]
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float ImpactShakeSeconds { get; set; } = 0.38f;
    [Export(PropertyHint.Range, "0.1,2,0.01")] public float CameraShakeSeconds { get; set; } = 0.38f;
    [Export(PropertyHint.Range, "0.03,1,0.01")] public float TitleZoomSeconds { get; set; } = 0.032f;
    [Export(PropertyHint.Range, "0.05,1,0.01")] public float LandingShakeSeconds { get; set; } = 0.42f;
    [Export(PropertyHint.Range, "0,4,0.05")] public float PromptDelaySeconds { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.05,1,0.01")] public float PromptSlideSeconds { get; set; } = 0.14f;

    [ExportGroup("Shake")]
    [Export(PropertyHint.Range, "0,6.5,0.01")] public float MaxCameraShake { get; set; } = 4.3f;
    [Export(PropertyHint.Range, "0,360,0.5")] public float DeathTextShakePixels { get; set; } = 210f;
    [Export(PropertyHint.Range, "1,24,0.05")] public float TitleStartScale { get; set; } = 18.5f;
    [Export(PropertyHint.Range, "0,1200,1")] public float TitleDropPixels { get; set; } = 640f;
    [Export(PropertyHint.Range, "1,140,1")] public float ShakeRate { get; set; } = 86f;

    [ExportGroup("Glitch")]
    [Export(PropertyHint.Range, "0,24,1")] public int GlitchBarCount { get; set; } = 16;
    [Export(PropertyHint.Range, "0,2,0.01")] public float GlitchStrength { get; set; } = 2f;

    private readonly RandomNumberGenerator _random = new();
    private readonly ColorRect[] _glitchBars = new ColorRect[16];
    private readonly Label[] _deathBlurLabels = new Label[3];
    private readonly Label[] _promptBlurLabels = new Label[3];
    private readonly string[] _deathMessages =
    {
        "Your warranty did not cover that.",
        "Skill issue, but cinematic.",
        "The tape ate your survival instinct.",
        "You zigged. It also zigged.",
        "Local cryptid requests a rematch.",
        "Camcorder footage ends here.",
        "You became background ambience.",
        "Fear was faster than sprint.",
        "The flashlight union has filed a complaint.",
        "Try blinking less dramatically next time."
    };
    private DeathPhase _phase = DeathPhase.Hidden;
    private Node3D? _cameraShakeTarget;
    private VhsPostProcessController? _vhsOverlay;
    private ColorRect? _darkness;
    private Label? _deathLabel;
    private Label? _deathMessageLabel;
    private Label? _promptLabel;
    private Button? _yesButton;
    private Button? _noButton;
    private Vector2 _baseUiPosition;
    private Vector2 _deathLabelBasePosition;
    private Vector2 _deathMessageBasePosition;
    private Vector2 _promptLabelBasePosition;
    private Vector2 _yesButtonBasePosition;
    private Vector2 _noButtonBasePosition;
    private Vector2 _deathLabelBaseScale;
    private Vector2 _deathMessageBaseScale;
    private Vector2 _promptLabelBaseScale;
    private Vector2 _yesButtonBaseScale;
    private Vector2 _noButtonBaseScale;
    private Vector3 _baseCameraPosition;
    private bool _hasCameraBase;
    private float _previousVhsIntensity;
    private float _previousVhsNoise;
    private float _previousVhsJitter;
    private float _previousVhsWobble;
    private float _previousVhsTracking;
    private float _previousVhsWhiteLine;
    private float _previousVhsDamage;
    private float _previousVhsDropout;
    private float _previousVhsBottomTear;
    private double _elapsedSeconds;
    private double _nextShakeTime;
    private double _landingShakeStartSeconds = -1;
    private bool _titleHasLanded;
    private string _activeDeathMessage = "";
    private Vector2 _deathTextShakeOffset;
    private Vector2 _promptShakeOffset;
    private Vector3 _cameraShakeOffset;

    public bool IsDeathActive => _phase != DeathPhase.Hidden;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        _baseUiPosition = Position;
        _random.Randomize();
        ResolveNodes();
        BuildOverlay();
        HideOverlay();
    }

    public override void _Process(double delta)
    {
        if (_phase == DeathPhase.Hidden)
        {
            return;
        }

        _elapsedSeconds += delta;
        float transitionProgress = Mathf.Clamp((float)_elapsedSeconds / Mathf.Max(ImpactShakeSeconds, 0.05f), 0f, 1f);
        UpdateTitleLandingState();
        UpdateShake();
        UpdateDarkness(transitionProgress);
        UpdateGlitchBars(transitionProgress);
        UpdateDeathText();
        UpdateVhsDamage(transitionProgress);

        float finishTime = Mathf.Max(CameraShakeSeconds, GetPromptSlideStartSeconds() + PromptSlideSeconds);
        if (_phase == DeathPhase.Transition && _elapsedSeconds >= finishTime)
        {
            _phase = DeathPhase.Dead;
            Position = _baseUiPosition;
            ResetTextShake();
            RestoreCameraShake();
        }
    }

    public void TriggerDeath()
    {
        if (_phase != DeathPhase.Hidden)
        {
            return;
        }

        ResolveNodes();
        CaptureCameraBase();
        CaptureVhsBase();

        _phase = DeathPhase.Transition;
        _elapsedSeconds = 0;
        _nextShakeTime = 0;
        _landingShakeStartSeconds = -1;
        _titleHasLanded = false;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        if (_deathLabel != null)
        {
            _deathLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            _deathLabel.Scale = Vector2.One * TitleStartScale;
        }

        _activeDeathMessage = _deathMessages[_random.RandiRange(0, _deathMessages.Length - 1)];

        if (_deathMessageLabel != null)
        {
            _deathMessageLabel.Text = _activeDeathMessage;
            _deathMessageLabel.Visible = false;
            _deathMessageLabel.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        if (_promptLabel != null)
        {
            _promptLabel.Visible = false;
            _promptLabel.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        if (_yesButton != null)
        {
            _yesButton.Visible = false;
            _yesButton.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        if (_noButton != null)
        {
            _noButton.Visible = false;
            _noButton.Modulate = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void ResolveNodes()
    {
        _cameraShakeTarget = GetNodeOrNull<Node3D>(CameraShakeTargetPath);
        _vhsOverlay = GetNodeOrNull<VhsPostProcessController>(VhsOverlayPath);
    }

    private void BuildOverlay()
    {
        _darkness = new ColorRect
        {
            Name = "Darkness",
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore,
            Color = Colors.Black
        };
        AddChild(_darkness);

        int barsToCreate = Mathf.Min(GlitchBarCount, _glitchBars.Length);
        for (int i = 0; i < barsToCreate; i++)
        {
            ColorRect bar = new()
            {
                Name = $"GlitchBar{i + 1:00}",
                MouseFilter = MouseFilterEnum.Ignore,
                Color = new Color(0.85f, 0.95f, 1f, 0f)
            };
            AddChild(bar);
            _glitchBars[i] = bar;
        }

        for (int i = 0; i < _deathBlurLabels.Length; i++)
        {
            _deathBlurLabels[i] = CreateMotionBlurLabel($"DeathBlur{i + 1:00}", "YOU DIED", new Vector2(-260f, -86f), new Vector2(260f, 8f), 62, new Color(0.85f, 0.05f, 0.035f));
        }

        _deathLabel = new Label
        {
            Name = "DeathLabel",
            Text = "YOU DIED",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -260f,
            OffsetTop = -86f,
            OffsetRight = 260f,
            OffsetBottom = 8f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _deathLabel.AddThemeFontSizeOverride("font_size", 62);
        _deathLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.05f, 0.035f));
        _deathLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.95f));
        _deathLabel.AddThemeConstantOverride("shadow_offset_x", 4);
        _deathLabel.AddThemeConstantOverride("shadow_offset_y", 4);
        _deathLabel.PivotOffset = new Vector2(260f, 47f);
        AddChild(_deathLabel);

        _deathMessageLabel = new Label
        {
            Name = "DeathMessageLabel",
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -360f,
            OffsetTop = 14f,
            OffsetRight = 360f,
            OffsetBottom = 56f,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        _deathMessageLabel.AddThemeFontSizeOverride("font_size", 20);
        _deathMessageLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.73f, 0.56f));
        _deathMessageLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.95f));
        _deathMessageLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        _deathMessageLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        AddChild(_deathMessageLabel);

        for (int i = 0; i < _promptBlurLabels.Length; i++)
        {
            _promptBlurLabels[i] = CreateMotionBlurLabel($"PromptBlur{i + 1:00}", "DO YOU WANT TO TRY AGAIN?", new Vector2(-330f, -76f), new Vector2(330f, -18f), 30, new Color(0.88f, 0.86f, 0.72f));
        }

        _promptLabel = new Label
        {
            Name = "PromptLabel",
            Text = "DO YOU WANT TO TRY AGAIN?",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -330f,
            OffsetTop = -76f,
            OffsetRight = 330f,
            OffsetBottom = -18f,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        _promptLabel.AddThemeFontSizeOverride("font_size", 30);
        _promptLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.86f, 0.72f));
        _promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.95f));
        _promptLabel.AddThemeConstantOverride("shadow_offset_x", 3);
        _promptLabel.AddThemeConstantOverride("shadow_offset_y", 3);
        _promptLabel.PivotOffset = new Vector2(330f, 29f);
        AddChild(_promptLabel);

        _yesButton = new Button
        {
            Name = "YesButton",
            Text = "YES",
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -150f,
            OffsetTop = 20f,
            OffsetRight = -22f,
            OffsetBottom = 68f,
            FocusMode = FocusModeEnum.All
        };
        _yesButton.AddThemeFontSizeOverride("font_size", 18);
        _yesButton.PivotOffset = new Vector2(64f, 24f);
        _yesButton.Pressed += Respawn;
        AddChild(_yesButton);

        _noButton = new Button
        {
            Name = "NoButton",
            Text = "NO",
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = 22f,
            OffsetTop = 20f,
            OffsetRight = 150f,
            OffsetBottom = 68f,
            FocusMode = FocusModeEnum.All
        };
        _noButton.AddThemeFontSizeOverride("font_size", 18);
        _noButton.PivotOffset = new Vector2(64f, 24f);
        _noButton.Pressed += QuitGame;
        AddChild(_noButton);

        _deathLabelBasePosition = _deathLabel.Position;
        _deathMessageBasePosition = _deathMessageLabel.Position;
        _promptLabelBasePosition = _promptLabel.Position;
        _yesButtonBasePosition = _yesButton.Position;
        _noButtonBasePosition = _noButton.Position;
        _deathLabelBaseScale = _deathLabel.Scale;
        _deathMessageBaseScale = _deathMessageLabel.Scale;
        _promptLabelBaseScale = _promptLabel.Scale;
        _yesButtonBaseScale = _yesButton.Scale;
        _noButtonBaseScale = _noButton.Scale;
    }

    private Label CreateMotionBlurLabel(string labelName, string text, Vector2 offsetStart, Vector2 offsetEnd, int fontSize, Color color)
    {
        Label label = new()
        {
            Name = labelName,
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AnchorsPreset = (int)LayoutPreset.Center,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = offsetStart.X,
            OffsetTop = offsetStart.Y,
            OffsetRight = offsetEnd.X,
            OffsetBottom = offsetEnd.Y,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        label.PivotOffset = new Vector2((offsetEnd.X - offsetStart.X) * 0.5f, (offsetEnd.Y - offsetStart.Y) * 0.5f);
        AddChild(label);
        return label;
    }

    private void HideOverlay()
    {
        if (_darkness != null)
        {
            _darkness.Color = new Color(0f, 0f, 0f, 0f);
        }

        foreach (ColorRect? bar in _glitchBars)
        {
            if (bar != null)
            {
                bar.Visible = false;
            }
        }

        HideMotionBlurLabels(_deathBlurLabels);
        HideMotionBlurLabels(_promptBlurLabels);

        if (_deathLabel != null)
        {
            _deathLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            _deathLabel.Position = _deathLabelBasePosition;
            _deathLabel.Scale = _deathLabelBaseScale * TitleStartScale;
        }

        if (_deathMessageLabel != null)
        {
            _deathMessageLabel.Visible = false;
            _deathMessageLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            _deathMessageLabel.Position = _deathMessageBasePosition;
            _deathMessageLabel.Scale = _deathMessageBaseScale;
        }

        if (_promptLabel != null)
        {
            _promptLabel.Visible = false;
            _promptLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            _promptLabel.Position = _promptLabelBasePosition;
            _promptLabel.Scale = _promptLabelBaseScale;
        }

        if (_yesButton != null)
        {
            _yesButton.Visible = false;
            _yesButton.Modulate = new Color(1f, 1f, 1f, 0f);
            _yesButton.Position = _yesButtonBasePosition;
            _yesButton.Scale = _yesButtonBaseScale;
        }

        if (_noButton != null)
        {
            _noButton.Visible = false;
            _noButton.Modulate = new Color(1f, 1f, 1f, 0f);
            _noButton.Position = _noButtonBasePosition;
            _noButton.Scale = _noButtonBaseScale;
        }
    }

    private void CaptureCameraBase()
    {
        if (_cameraShakeTarget == null)
        {
            _hasCameraBase = false;
            return;
        }

        _baseCameraPosition = _cameraShakeTarget.Position;
        _hasCameraBase = true;
    }

    private void CaptureVhsBase()
    {
        if (_vhsOverlay == null)
        {
            return;
        }

        _previousVhsIntensity = _vhsOverlay.Intensity;
        _previousVhsNoise = _vhsOverlay.NoiseStrength;
        _previousVhsJitter = _vhsOverlay.RandomJitterStrength;
        _previousVhsWobble = _vhsOverlay.WobbleStrength;
        _previousVhsTracking = _vhsOverlay.TrackingStrength;
        _previousVhsWhiteLine = _vhsOverlay.WhiteLineStrength;
        _previousVhsDamage = _vhsOverlay.DamageFrequency;
        _previousVhsDropout = _vhsOverlay.DropoutStrength;
        _previousVhsBottomTear = _vhsOverlay.BottomTearStrength;
        _vhsOverlay.Enabled = true;
    }

    private void UpdateShake()
    {
        if (_elapsedSeconds < _nextShakeTime)
        {
            ApplyShake();
            return;
        }

        _nextShakeTime = _elapsedSeconds + (1.0 / Mathf.Max(ShakeRate, 1f));
        float originalCameraShakeSeconds = Mathf.Min(Mathf.Max(CameraShakeSeconds, 0.05f), Mathf.Max(ImpactShakeSeconds, 0.05f));
        float progress = Mathf.Clamp((float)_elapsedSeconds / originalCameraShakeSeconds, 0f, 1f);
        float envelope = _phase == DeathPhase.Transition && _elapsedSeconds < ImpactShakeSeconds
            ? Mathf.Pow(Mathf.Clamp(1f - progress, 0f, 1f), 0.16f)
            : 0f;
        float cameraAmount = MaxCameraShake * envelope;
        float landingTime = _titleHasLanded ? (float)(_elapsedSeconds - _landingShakeStartSeconds) : -1f;
        float landingProgress = Mathf.Clamp(landingTime / Mathf.Max(LandingShakeSeconds, 0.05f), 0f, 1f);
        float landingEnvelope = landingTime >= 0f && landingProgress < 1f ? Mathf.Sin((1f - landingProgress) * Mathf.Pi * 0.5f) : 0f;
        float textAmount = DeathTextShakePixels * landingEnvelope;

        _cameraShakeOffset = new Vector3(
            _random.RandfRange(-cameraAmount, cameraAmount),
            _random.RandfRange(-cameraAmount, cameraAmount),
            0f);

        _deathTextShakeOffset = new Vector2(
            _random.RandfRange(-textAmount * 0.28f, textAmount * 0.28f),
            _random.RandfRange(-textAmount * 0.9f, textAmount * 0.9f));

        _promptShakeOffset = new Vector2(
            _random.RandfRange(-textAmount * 0.18f, textAmount * 0.18f),
            _random.RandfRange(-textAmount * 0.34f, textAmount * 0.34f));

        ApplyShake();
    }

    private void UpdateTitleLandingState()
    {
        if (_titleHasLanded)
        {
            return;
        }

        double landingSeconds = ImpactShakeSeconds + TitleZoomSeconds;
        if (_elapsedSeconds >= landingSeconds)
        {
            _titleHasLanded = true;
            _landingShakeStartSeconds = landingSeconds;
            _nextShakeTime = 0;
        }
    }

    private void ApplyShake()
    {
        Position = _baseUiPosition;

        if (_hasCameraBase && _cameraShakeTarget != null)
        {
            _cameraShakeTarget.Position = _baseCameraPosition + _cameraShakeOffset;
        }

        if (_deathLabel != null)
        {
            _deathLabel.Position = _deathLabelBasePosition + _deathTextShakeOffset;
        }

        if (_deathMessageLabel != null)
        {
            _deathMessageLabel.Position = _deathMessageBasePosition + _deathTextShakeOffset * 0.18f;
        }

        if (_promptLabel != null)
        {
            _promptLabel.Position = _promptLabelBasePosition + _promptShakeOffset;
        }

        if (_yesButton != null)
        {
            _yesButton.Position = _yesButtonBasePosition + _promptShakeOffset;
        }

        if (_noButton != null)
        {
            _noButton.Position = _noButtonBasePosition + _promptShakeOffset;
        }
    }

    private void RestoreCameraShake()
    {
        if (_hasCameraBase && _cameraShakeTarget != null)
        {
            _cameraShakeTarget.Position = _baseCameraPosition;
        }
    }

    private void ResetTextShake()
    {
        _deathTextShakeOffset = Vector2.Zero;
        _promptShakeOffset = Vector2.Zero;

        if (_deathLabel != null)
        {
            _deathLabel.Position = _deathLabelBasePosition;
            _deathLabel.Scale = _deathLabelBaseScale;
        }

        if (_deathMessageLabel != null)
        {
            _deathMessageLabel.Position = _deathMessageBasePosition;
            _deathMessageLabel.Scale = _deathMessageBaseScale;
        }

        if (_promptLabel != null)
        {
            _promptLabel.Position = _promptLabelBasePosition;
            _promptLabel.Scale = _promptLabelBaseScale;
        }

        if (_yesButton != null)
        {
            _yesButton.Position = _yesButtonBasePosition;
            _yesButton.Scale = _yesButtonBaseScale;
        }

        if (_noButton != null)
        {
            _noButton.Position = _noButtonBasePosition;
            _noButton.Scale = _noButtonBaseScale;
        }
    }

    private void UpdateDarkness(float progress)
    {
        if (_darkness == null)
        {
            return;
        }

        float alpha = Mathf.SmoothStep(0f, 1f, progress);
        _darkness.Color = new Color(0f, 0f, 0f, Mathf.Clamp(alpha, 0f, 1f));
    }

    private void UpdateGlitchBars(float progress)
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float intensity = GetDeathScreenAggression(progress) * GlitchStrength;

        foreach (ColorRect? bar in _glitchBars)
        {
            if (bar == null)
            {
                continue;
            }

            bool visible = _phase == DeathPhase.Transition && _random.Randf() < 0.92f * intensity;
            bar.Visible = visible;

            if (!visible)
            {
                continue;
            }

            float height = _random.RandfRange(5f, 74f);
            float y = _random.RandfRange(0f, Mathf.Max(viewportSize.Y - height, 0f));
            float xOffset = _random.RandfRange(-190f, 190f) * intensity;
            bar.Position = new Vector2(xOffset, y);
            bar.Size = new Vector2(viewportSize.X + 380f, height);
            bar.Color = new Color(
                _random.RandfRange(0.7f, 1f),
                _random.RandfRange(0.03f, 0.22f),
                _random.RandfRange(0.02f, 0.18f),
                _random.RandfRange(0.18f, 0.68f) * intensity);
        }
    }

    private void UpdateDeathText()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float promptSlideStart = GetPromptSlideStartSeconds();
        float promptProgress = Mathf.Clamp(((float)_elapsedSeconds - promptSlideStart) / Mathf.Max(PromptSlideSeconds, 0.05f), 0f, 1f);
        float promptEase = 1f - Mathf.Pow(1f - promptProgress, 4f);
        float deathExitX = -viewportSize.X * 1.35f * promptEase;
        float promptEnterX = viewportSize.X * 1.28f * (1f - promptEase);
        float promptAlpha = promptProgress > 0f ? promptEase : 0f;

        if (_deathLabel != null)
        {
            float titleTime = (float)_elapsedSeconds - ImpactShakeSeconds;
            float zoomProgress = Mathf.Clamp(titleTime / Mathf.Max(TitleZoomSeconds, 0.05f), 0f, 1f);
            float easedZoom = 1f - Mathf.Pow(1f - zoomProgress, 5f);
            float titleScale = Mathf.Lerp(TitleStartScale, 1f, easedZoom);
            float dropOffset = Mathf.Lerp(-TitleDropPixels, 0f, easedZoom);
            float titleAlpha = titleTime >= 0f ? 1f : 0f;
            Vector2 deathPosition = _deathLabelBasePosition + new Vector2(deathExitX, dropOffset) + _deathTextShakeOffset;
            Vector2 deathTrail = GetDeathTrailOffset(dropOffset, deathExitX);

            _deathLabel.Modulate = new Color(1f, 1f, 1f, titleAlpha);
            _deathLabel.Position = deathPosition;
            _deathLabel.Scale = _deathLabelBaseScale * titleScale;
            UpdateMotionBlurLabels(_deathBlurLabels, deathPosition, _deathLabelBaseScale * titleScale, deathTrail, titleAlpha);
        }

        if (_deathMessageLabel != null)
        {
            float messageStart = ImpactShakeSeconds + TitleZoomSeconds + 0.16f;
            float messageFade = Mathf.Clamp(((float)_elapsedSeconds - messageStart) / 0.35f, 0f, 1f);
            float messageExitFade = promptProgress > 0f ? 1f - promptEase : 1f;
            float messageAlpha = messageFade * messageExitFade;
            _deathMessageLabel.Visible = messageAlpha > 0.01f;
            _deathMessageLabel.Modulate = new Color(1f, 1f, 1f, messageAlpha);
            _deathMessageLabel.Position = _deathMessageBasePosition + new Vector2(deathExitX * 0.35f, 0f) + _deathTextShakeOffset * 0.18f;
            _deathMessageLabel.Scale = _deathMessageBaseScale;
        }

        if (_promptLabel != null)
        {
            Vector2 promptPosition = _promptLabelBasePosition + new Vector2(promptEnterX, 0f) + _promptShakeOffset;
            Vector2 promptTrail = new Vector2(190f * (1f - promptEase), 0f);
            _promptLabel.Visible = promptAlpha > 0.01f;
            _promptLabel.Modulate = new Color(1f, 1f, 1f, promptAlpha);
            _promptLabel.Position = promptPosition;
            _promptLabel.Scale = _promptLabelBaseScale;
            UpdateMotionBlurLabels(_promptBlurLabels, promptPosition, _promptLabelBaseScale, promptTrail, promptAlpha);
        }

        if (_yesButton != null)
        {
            _yesButton.Visible = promptAlpha > 0.01f;
            _yesButton.Modulate = new Color(1f, 1f, 1f, promptAlpha);
            _yesButton.Position = _yesButtonBasePosition + new Vector2(promptEnterX, 0f) + _promptShakeOffset;
            _yesButton.Scale = _yesButtonBaseScale;
        }

        if (_noButton != null)
        {
            _noButton.Visible = promptAlpha > 0.01f;
            _noButton.Modulate = new Color(1f, 1f, 1f, promptAlpha);
            _noButton.Position = _noButtonBasePosition + new Vector2(promptEnterX, 0f) + _promptShakeOffset;
            _noButton.Scale = _noButtonBaseScale;
        }
    }

    private float GetPromptSlideStartSeconds()
    {
        return ImpactShakeSeconds + TitleZoomSeconds + LandingShakeSeconds + PromptDelaySeconds;
    }

    private float GetDeathScreenAggression(float impactProgress)
    {
        float impact = Mathf.Pow(Mathf.Clamp(1f - impactProgress, 0f, 1f), 0.45f);
        float landingTime = _titleHasLanded ? (float)(_elapsedSeconds - _landingShakeStartSeconds) : -1f;
        float landingProgress = Mathf.Clamp(landingTime / Mathf.Max(LandingShakeSeconds, 0.05f), 0f, 1f);
        float landing = landingTime >= 0f && landingProgress < 1f ? Mathf.Sin((1f - landingProgress) * Mathf.Pi * 0.5f) * 0.78f : 0f;
        float promptProgress = Mathf.Clamp(((float)_elapsedSeconds - GetPromptSlideStartSeconds()) / Mathf.Max(PromptSlideSeconds, 0.05f), 0f, 1f);
        float prompt = promptProgress > 0f && promptProgress < 1f ? Mathf.Sin(promptProgress * Mathf.Pi) * 0.62f : 0f;
        return Mathf.Clamp(Mathf.Max(impact, Mathf.Max(landing, prompt)), 0f, 1f);
    }

    private Vector2 GetDeathTrailOffset(float dropOffset, float deathExitX)
    {
        if (Mathf.Abs(deathExitX) > 0.1f)
        {
            return new Vector2(230f, 0f);
        }

        float verticalTrail = Mathf.Clamp(Mathf.Abs(dropOffset) * 0.5f, 0f, 260f);
        return new Vector2(0f, -verticalTrail);
    }

    private void UpdateMotionBlurLabels(Label[] labels, Vector2 basePosition, Vector2 baseScale, Vector2 trailOffset, float alpha)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            Label label = labels[i];
            float step = i + 1f;
            float weight = 1f - (i / (float)labels.Length);
            label.Visible = alpha > 0.02f && trailOffset.LengthSquared() > 1f;
            label.Position = basePosition + trailOffset * step * 0.5f;
            label.Scale = baseScale;
            label.Modulate = new Color(1f, 1f, 1f, alpha * 0.34f * weight);
        }
    }

    private static void HideMotionBlurLabels(Label[] labels)
    {
        foreach (Label label in labels)
        {
            label.Visible = false;
            label.Modulate = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void UpdateVhsDamage(float progress)
    {
        if (_vhsOverlay == null)
        {
            return;
        }

        float impactEnvelope = Mathf.Pow(GetDeathScreenAggression(progress), 0.22f);
        _vhsOverlay.Intensity = Mathf.Clamp(_previousVhsIntensity + impactEnvelope * 0.8f, 0f, 1f);
        _vhsOverlay.NoiseStrength = Mathf.Clamp(_previousVhsNoise + impactEnvelope * 1.7f, 0f, 1f);
        _vhsOverlay.RandomJitterStrength = Mathf.Clamp(_previousVhsJitter + impactEnvelope * 1.9f, 0f, 1f);
        _vhsOverlay.WobbleStrength = Mathf.Clamp(_previousVhsWobble + impactEnvelope * 1.24f, 0f, 1f);
        _vhsOverlay.TrackingStrength = Mathf.Clamp(_previousVhsTracking + impactEnvelope * 1.5f, 0f, 1f);
        _vhsOverlay.WhiteLineStrength = Mathf.Clamp(_previousVhsWhiteLine + impactEnvelope * 1.4f, 0f, 1f);
        _vhsOverlay.DamageFrequency = Mathf.Clamp(_previousVhsDamage + impactEnvelope * 1.3f, 0f, 1f);
        _vhsOverlay.DropoutStrength = Mathf.Clamp(_previousVhsDropout + impactEnvelope * 1.1f, 0f, 1f);
        _vhsOverlay.BottomTearStrength = Mathf.Clamp(_previousVhsBottomTear + impactEnvelope * 1f, 0f, 1f);
    }

    private void Respawn()
    {
        RestoreCameraShake();

        if (_vhsOverlay != null)
        {
            _vhsOverlay.Intensity = _previousVhsIntensity;
            _vhsOverlay.NoiseStrength = _previousVhsNoise;
            _vhsOverlay.RandomJitterStrength = _previousVhsJitter;
            _vhsOverlay.WobbleStrength = _previousVhsWobble;
            _vhsOverlay.TrackingStrength = _previousVhsTracking;
            _vhsOverlay.WhiteLineStrength = _previousVhsWhiteLine;
            _vhsOverlay.DamageFrequency = _previousVhsDamage;
            _vhsOverlay.DropoutStrength = _previousVhsDropout;
            _vhsOverlay.BottomTearStrength = _previousVhsBottomTear;
        }

        GetTree().ReloadCurrentScene();
    }

    private void QuitGame()
    {
        GetTree().Quit();
    }
}
