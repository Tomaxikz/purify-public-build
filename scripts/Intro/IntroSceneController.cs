using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class IntroSceneController : Node3D
{
    private enum MenuHoverTarget
    {
        None,
        Play,
        Credits,
        Settings,
        AudioDown,
        AudioUp,
        AudioSlider,
        AudioMute,
        SettingsBack,
        CreditsBack
    }

    [Export] public NodePath PlayerPath { get; set; } = new NodePath("Player");
    [Export] public NodePath FlashlightPath { get; set; } = new NodePath("Player/CameraPivot/Camera3D/Flashlight");
    [Export] public NodePath FlashlightModelPath { get; set; } = new NodePath("Player/CameraPivot/Camera3D/FlashlightRig");
    [Export] public NodePath IntroBlackoutPath { get; set; } = new NodePath("IntroLayer/Blackout");
    [Export] public NodePath PrimaryTextPath { get; set; } = new NodePath("IntroLayer/PrimaryText");
    [Export] public NodePath SecondaryTextPath { get; set; } = new NodePath("IntroLayer/SecondaryText");
    [Export] public NodePath CameraPath { get; set; } = new NodePath("Player/CameraPivot/Camera3D");
    [Export] public NodePath EntityPath { get; set; } = new NodePath("Room/EntityReveal/FocusTarget");
    [Export] public NodePath EntityVisualPath { get; set; } = new NodePath("Room/EntityReveal/EntityVisual");
    [Export] public NodePath LeftEyeGlowPath { get; set; } = new NodePath("Room/EntityReveal/LeftEyeGlow");
    [Export] public NodePath RightEyeGlowPath { get; set; } = new NodePath("Room/EntityReveal/RightEyeGlow");
    [Export] public NodePath EyeLightPath { get; set; } = new NodePath("Room/EntityReveal/EyeLight");
    [Export] public NodePath VhsOverlayPath { get; set; } = new NodePath("PostProcessLayer/VhsOverlay");
    [Export] public NodePath MenuLayerPath { get; set; } = new NodePath("MenuLayer");
    [Export] public NodePath PlayButtonPath { get; set; } = new NodePath("MenuLayer/PlayButton");
    [Export] public NodePath CreditsButtonPath { get; set; } = new NodePath("MenuLayer/CreditsButton");
    [Export] public NodePath CreditsLabelPath { get; set; } = new NodePath("MenuLayer/CreditsLabel");
    [Export] public NodePath Menu3DPath { get; set; } = new NodePath("Room/Menu3D");
    [Export] public NodePath PlayAreaPath { get; set; } = new NodePath("Room/Menu3D/PlayArea");
    [Export] public NodePath CreditsAreaPath { get; set; } = new NodePath("Room/Menu3D/CreditsArea");
    [Export] public NodePath CreditsSubmenuPath { get; set; } = new NodePath("Room/Menu3D/CreditsSubmenu");
    [Export] public NodePath CreditsBackAreaPath { get; set; } = new NodePath("Room/Menu3D/CreditsSubmenu/CreditsBackArea");
    [Export] public NodePath SettingsAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsArea");
    [Export] public NodePath SettingsSubmenuPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu");
    [Export] public NodePath AudioDownAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioDownArea");
    [Export] public NodePath AudioUpAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioUpArea");
    [Export] public NodePath AudioSliderAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioSliderArea");
    [Export] public NodePath AudioSliderKnobPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioSliderKnob");
    [Export] public NodePath AudioSliderFillPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioSliderFill");
    [Export] public NodePath AudioMuteAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/AudioMuteArea");
    [Export] public NodePath SettingsBackAreaPath { get; set; } = new NodePath("Room/Menu3D/SettingsSubmenu/SettingsBackArea");
    [Export] public NodePath Credits3DLabelPath { get; set; } = new NodePath("Room/Menu3D/Credits3DLabel");
    [Export] public NodePath Settings3DLabelPath { get; set; } = new NodePath("Room/Menu3D/Settings3DLabel");
    [Export] public NodePath RealLobbyAudioPath { get; set; } = new NodePath("RealLobbyLoop");
    [Export] public string BaseScenePath { get; set; } = "res://Base/Dev.tscn";
    [Export] public string SettingsConfigPath { get; set; } = "user://settings.cfg";
    [Export] public bool MenuIgnoresLighting { get; set; } = true;
    [Export(PropertyHint.Range, "-60,0,0.5")] public float RealLobbyVolumeDb { get; set; } = -16.5f;
    [Export(PropertyHint.Range, "0.05,8,0.05")] public float RealLobbyFadeSeconds { get; set; } = 3.0f;

    [ExportGroup("Menu Feel")]
    [Export(PropertyHint.Range, "0,8,0.1")] public float MenuMouseTiltDegrees { get; set; } = 2.4f;
    [Export(PropertyHint.Range, "0.1,30,0.1")] public float MenuMouseTiltSpeed { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.05,1.5,0.01")] public float MenuSlideSeconds { get; set; } = 0.34f;
    [Export] public float MenuSlideDistance { get; set; } = 6.2f;
    [Export] public float AudioSliderMinX { get; set; } = -1.75f;
    [Export] public float AudioSliderMaxX { get; set; } = 1.75f;

    [ExportGroup("Text Timing")]
    [Export] public float FirstDelaySeconds { get; set; } = 0.6f;
    [Export] public float TypeSecondsPerCharacter { get; set; } = 0.12f;
    [Export] public float BehindTypeSecondsPerCharacter { get; set; } = 0.22f;
    [Export] public float FadeSeconds { get; set; } = 1.1f;
    [Export] public float IntroSceneBlendSeconds { get; set; } = 1.65f;
    [Export] public float WarningRedRampSeconds { get; set; } = 1.4f;

    [ExportGroup("Shake")]
    [Export] public float BehindShakePixels { get; set; } = 9f;
    [Export] public float BehindShakeRate { get; set; } = 42f;

    [ExportGroup("Entity Reveal")]
    [Export] public float NoticeDistance { get; set; } = 18f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float NoticeDotThreshold { get; set; } = 0.78f;
    [Export] public float FocusSeconds { get; set; } = 1.15f;
    [Export(PropertyHint.Range, "0.05,4,0.01")] public float FocusCatchSeconds { get; set; } = 1.75f;
    [Export(PropertyHint.Range, "0.05,4,0.01")] public float FocusZoomSeconds { get; set; } = 1.1f;
    [Export(PropertyHint.Range, "12,75,0.5")] public float FocusTargetFov { get; set; } = 38f;
    [Export] public Vector3 FocusCameraOffset { get; set; } = new Vector3(1.2f, -0.05f, -2.85f);
    [Export] public Vector2 LeftEyeSpriteOffset { get; set; } = new Vector2(-0.13f, 1.39f);
    [Export] public Vector2 RightEyeSpriteOffset { get; set; } = new Vector2(0.05f, 1.39f);
    [Export] public float EyeDepthOffset { get; set; } = 0.03f;

    [ExportGroup("Debug")]
    [Export] public Key ToggleDebugKey { get; set; } = Key.D;
    [Export] public Key SkipIntroKey { get; set; } = Key.Space;
    [Export] public Key SkipIntroAltKey { get; set; } = Key.Enter;

    private PlayerController? _player;
    private FlashlightController? _flashlight;
    private Node3D? _flashlightModel;
    private Camera3D? _camera;
    private Node3D? _entity;
    private Node3D? _entityVisual;
    private Node3D? _leftEyeGlow;
    private Node3D? _rightEyeGlow;
    private Node3D? _eyeLight;
    private VhsPostProcessController? _vhsOverlay;
    private Control? _menuLayer;
    private Button? _playButton;
    private Button? _creditsButton;
    private Label? _creditsLabel;
    private Node3D? _menu3D;
    private Area3D? _playArea;
    private Area3D? _creditsArea;
    private Node3D? _creditsSubmenu;
    private Area3D? _creditsBackArea;
    private Area3D? _settingsArea;
    private Node3D? _settingsSubmenu;
    private Area3D? _audioDownArea;
    private Area3D? _audioUpArea;
    private Area3D? _audioSliderArea;
    private Node3D? _audioSliderKnob;
    private Node3D? _audioSliderFill;
    private Area3D? _audioMuteArea;
    private Area3D? _settingsBackArea;
    private Label3D? _credits3DLabel;
    private Label3D? _settings3DLabel;
    private AudioStreamPlayer? _realLobbyAudio;
    private ColorRect? _introBlackout;
    private RichTextLabel? _primaryText;
    private RichTextLabel? _secondaryText;
    private Control? _debugMenu;
    private Label? _debugText;
    private Vector2 _primaryBasePosition;
    private Vector2 _secondaryBasePosition;
    private Transform3D _menuCameraBaseTransform;
    private Vector2 _menuCameraTilt;
    private readonly RandomNumberGenerator _random = new();
    private readonly System.Collections.Generic.Dictionary<Node3D, Transform3D> _menuBaseTransforms = new();
    private readonly System.Collections.Generic.Dictionary<Label3D, Color> _menuBaseLabelColors = new();
    private readonly System.Collections.Generic.Dictionary<Label3D, Color> _menuBaseOutlineColors = new();
    private readonly System.Collections.Generic.Dictionary<GeometryInstance3D, float> _menuBaseTransparencies = new();
    private MenuHoverTarget _hoverTarget = MenuHoverTarget.None;
    private bool _shakeBehindText;
    private bool _rampWarningTextRed;
    private bool _playerUnlocked;
    private bool _entityNoticeTriggered;
    private bool _menuVisible;
    private bool _menuFadeStarted;
    private bool _realLobbyFadeStarted;
    private bool _creditsOpen;
    private bool _creditsAnimating;
    private bool _settingsOpen;
    private bool _settingsAnimating;
    private bool _audioSliderDragging;
    private bool _realLobbyMuted;
    private float _currentRealLobbyVolumeDb;
    private bool _openingFinished;
    private bool _skipOpeningRequested;
    private bool _debugVisible;
    private int _introSequenceId;
    private double _warningRedRampElapsed;
    private double _nextShakeTime;

    private const string SettingsSection = "audio";
    private const string LobbyVolumeKey = "lobby_volume_db";
    private const string LobbyMutedKey = "lobby_muted";

    public override void _Ready()
    {
        _random.Randomize();
        _player = GetNodeOrNull<PlayerController>(PlayerPath);
        _flashlight = GetNodeOrNull<FlashlightController>(FlashlightPath);
        _flashlightModel = GetNodeOrNull<Node3D>(FlashlightModelPath);
        _camera = GetNodeOrNull<Camera3D>(CameraPath);
        _entity = GetNodeOrNull<Node3D>(EntityPath);
        _entityVisual = GetNodeOrNull<Node3D>(EntityVisualPath);
        _leftEyeGlow = GetNodeOrNull<Node3D>(LeftEyeGlowPath);
        _rightEyeGlow = GetNodeOrNull<Node3D>(RightEyeGlowPath);
        _eyeLight = GetNodeOrNull<Node3D>(EyeLightPath);
        _vhsOverlay = GetNodeOrNull<VhsPostProcessController>(VhsOverlayPath);
        _menuLayer = GetNodeOrNull<Control>(MenuLayerPath);
        _playButton = GetNodeOrNull<Button>(PlayButtonPath);
        _creditsButton = GetNodeOrNull<Button>(CreditsButtonPath);
        _creditsLabel = GetNodeOrNull<Label>(CreditsLabelPath);
        _menu3D = GetNodeOrNull<Node3D>(Menu3DPath);
        _playArea = GetNodeOrNull<Area3D>(PlayAreaPath);
        _creditsArea = GetNodeOrNull<Area3D>(CreditsAreaPath);
        _creditsSubmenu = GetNodeOrNull<Node3D>(CreditsSubmenuPath);
        _creditsBackArea = GetNodeOrNull<Area3D>(CreditsBackAreaPath);
        _settingsArea = GetNodeOrNull<Area3D>(SettingsAreaPath);
        _settingsSubmenu = GetNodeOrNull<Node3D>(SettingsSubmenuPath);
        _audioDownArea = GetNodeOrNull<Area3D>(AudioDownAreaPath);
        _audioUpArea = GetNodeOrNull<Area3D>(AudioUpAreaPath);
        _audioSliderArea = GetNodeOrNull<Area3D>(AudioSliderAreaPath);
        _audioSliderKnob = GetNodeOrNull<Node3D>(AudioSliderKnobPath);
        _audioSliderFill = GetNodeOrNull<Node3D>(AudioSliderFillPath);
        _audioMuteArea = GetNodeOrNull<Area3D>(AudioMuteAreaPath);
        _settingsBackArea = GetNodeOrNull<Area3D>(SettingsBackAreaPath);
        _credits3DLabel = GetNodeOrNull<Label3D>(Credits3DLabelPath);
        _settings3DLabel = GetNodeOrNull<Label3D>(Settings3DLabelPath);
        _realLobbyAudio = GetNodeOrNull<AudioStreamPlayer>(RealLobbyAudioPath);
        _introBlackout = GetNodeOrNull<ColorRect>(IntroBlackoutPath);
        _primaryText = GetNodeOrNull<RichTextLabel>(PrimaryTextPath);
        _secondaryText = GetNodeOrNull<RichTextLabel>(SecondaryTextPath);

        if (_playButton != null)
        {
            _playButton.Pressed += PlayGame;
        }

        if (_creditsButton != null)
        {
            _creditsButton.Pressed += ToggleCredits;
        }

        if (_menuLayer != null)
        {
            _menuLayer.Visible = false;
            _menuLayer.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        if (_creditsLabel != null)
        {
            _creditsLabel.Visible = false;
        }

        if (_menu3D != null)
        {
            _menu3D.Visible = false;
            ApplyMenuLightingMode(_menu3D);
            CaptureMenuVisualState(_menu3D);
        }

        if (_credits3DLabel != null)
        {
            _credits3DLabel.Visible = false;
        }

        SetCreditsSubmenuImmediate(false);

        if (_settings3DLabel != null)
        {
            _settings3DLabel.Visible = false;
        }

        _currentRealLobbyVolumeDb = RealLobbyVolumeDb;
        LoadSettings();
        SetSettingsSubmenuImmediate(false);
        UpdateSettingsText();
        UpdateAudioSliderVisual();

        if (_realLobbyAudio != null)
        {
            _realLobbyAudio.VolumeDb = -80f;
            if (_realLobbyAudio.Stream is AudioStreamMP3 mp3)
            {
                mp3.Loop = true;
            }
        }

        BuildDebugMenu();
        _player?.SetControlEnabled(false);
        if (_flashlight != null)
        {
            _flashlight.ToggleEnabled = false;
            _flashlight.SetFlashlightEnabled(false, animateVisual: false);
        }

        if (_flashlightModel != null)
        {
            _flashlightModel.Visible = false;
        }

        SetIntroBlackoutAlpha(1f);

        if (_primaryText != null)
        {
            _primaryBasePosition = _primaryText.Position;
            _primaryText.Text = "";
            _primaryText.Modulate = Colors.White;
        }

        if (_secondaryText != null)
        {
            _secondaryBasePosition = _secondaryText.Position;
            _secondaryText.Text = "";
            _secondaryText.Modulate = Colors.White;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;
        _ = PlayIntroAsync(++_introSequenceId);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent
            && keyEvent.Pressed
            && !keyEvent.Echo
            && keyEvent.CtrlPressed
            && keyEvent.Keycode == Key.P)
        {
            GetTree().ChangeSceneToFile(BaseScenePath);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey debugKeyEvent && debugKeyEvent.Pressed && !debugKeyEvent.Echo)
        {
            if (debugKeyEvent.CtrlPressed && debugKeyEvent.Keycode == ToggleDebugKey)
            {
                SetDebugVisible(!_debugVisible);
                GetViewport().SetInputAsHandled();
                return;
            }

            if (!_openingFinished && (debugKeyEvent.Keycode == SkipIntroKey || debugKeyEvent.Keycode == SkipIntroAltKey))
            {
                SkipOpening();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_debugVisible)
            {
                if (debugKeyEvent.Keycode == Key.S)
                {
                    SkipOpening();
                }
                else if (debugKeyEvent.Keycode == Key.R)
                {
                    ForceRevealMenu();
                }
                else if (debugKeyEvent.Keycode == Key.F && _flashlight != null)
                {
                    _flashlight.ToggleEnabled = true;
                    _flashlight.ToggleFlashlight();
                }
                else if (debugKeyEvent.Keycode == Key.V && _vhsOverlay != null)
                {
                    _vhsOverlay.Enabled = !_vhsOverlay.Enabled;
                }
                else if (debugKeyEvent.Keycode == Key.Escape)
                {
                    SetDebugVisible(false);
                }
                else
                {
                    return;
                }

                UpdateDebugText();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_menuVisible && @event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                HandleMenuClick(mouseButton.Position);
            }
            else
            {
                _audioSliderDragging = false;
            }

            GetViewport().SetInputAsHandled();
        }

        if (_audioSliderDragging && @event is InputEventMouseMotion motion)
        {
            SetVolumeFromSliderScreenPosition(motion.Position);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        UpdateWarningTextColor(delta);
        CheckEntityNotice();
        UpdateEntityEyeBillboard();
        if (_audioSliderDragging)
        {
            SetVolumeFromSliderScreenPosition(GetViewport().GetMousePosition());
        }

        UpdateMenuHover((float)delta);
        UpdateMenuCameraTilt((float)delta);
        if (_debugVisible)
        {
            UpdateDebugText();
        }

        if (!_shakeBehindText || _primaryText == null)
        {
            return;
        }

        if (Time.GetTicksMsec() / 1000.0 < _nextShakeTime)
        {
            return;
        }

        _nextShakeTime = Time.GetTicksMsec() / 1000.0 + (1.0 / Mathf.Max(BehindShakeRate, 1f));
        _primaryText.Position = _primaryBasePosition + new Vector2(
            _random.RandfRange(-BehindShakePixels, BehindShakePixels),
            _random.RandfRange(-BehindShakePixels, BehindShakePixels));
    }

    private async Task PlayIntroAsync(int sequenceId)
    {
        await WaitSeconds(FirstDelaySeconds);
        if (IsIntroCancelled(sequenceId)) return;
        await TypeInto(_primaryText, "Purify...", TypeSecondsPerCharacter);
        if (IsIntroCancelled(sequenceId)) return;
        await WaitSeconds(0.45f);
        if (IsIntroCancelled(sequenceId)) return;
        await TypeInto(_secondaryText, "Purify that which is impure...", TypeSecondsPerCharacter);
        if (IsIntroCancelled(sequenceId)) return;
        await WaitSeconds(1.25f);
        if (IsIntroCancelled(sequenceId)) return;
        await FadeTextsOut();
        if (IsIntroCancelled(sequenceId)) return;
        ClearTexts();
        await WaitSeconds(0.45f);
        if (IsIntroCancelled(sequenceId)) return;

        await TypeInto(_primaryText, "He's somewhere... ", TypeSecondsPerCharacter);
        if (IsIntroCancelled(sequenceId)) return;
        StartBehindPanic();
        StartWarningRedRamp();
        await TypeInto(_primaryText, "behind", BehindTypeSecondsPerCharacter, append: true);
        if (IsIntroCancelled(sequenceId)) return;
        await WaitSeconds(0.9f);
        if (IsIntroCancelled(sequenceId)) return;
        await FadeTextsOut();
        if (IsIntroCancelled(sequenceId)) return;
        ClearTexts();
        StopBehindPanic();
        EnablePlayer();
        _ = FadeIntroBlackoutOut();
    }

    private async Task TypeInto(RichTextLabel? label, string text, float secondsPerCharacter, bool append = false)
    {
        if (label == null)
        {
            return;
        }

        label.Visible = true;
        label.Modulate = label.Modulate with { A = 1f };
        string prefix = append ? label.Text : "";

        for (int i = 1; i <= text.Length; i++)
        {
            if (_skipOpeningRequested)
            {
                return;
            }

            string typed = text[..i];
            if (_rampWarningTextRed && append)
            {
                float redProgress = Mathf.Clamp(i / (float)Mathf.Max(text.Length, 1), 0f, 1f);
                Color behindColor = Colors.White.Lerp(new Color(1f, 0.04f, 0.035f, 1f), redProgress);
                label.Text = $"{prefix}[color=#{behindColor.ToHtml(false)}]{typed}[/color]";
            }
            else
            {
                label.Text = prefix + typed;
            }

            await WaitSeconds(secondsPerCharacter);
        }
    }

    private void StartBehindPanic()
    {
        _shakeBehindText = true;
        _nextShakeTime = 0;
    }

    private void StopBehindPanic()
    {
        _shakeBehindText = false;
        _rampWarningTextRed = false;

        if (_primaryText != null)
        {
            _primaryText.Position = _primaryBasePosition;
        }
    }

    private void StartWarningRedRamp()
    {
        _rampWarningTextRed = true;
        _warningRedRampElapsed = 0;
    }

    private void UpdateWarningTextColor(double delta)
    {
        if (!_rampWarningTextRed || _primaryText == null)
        {
            return;
        }

        _warningRedRampElapsed += delta;
    }

    private async Task FadeTextsOut()
    {
        float elapsed = 0f;
        while (elapsed < FadeSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float alpha = Mathf.Clamp(1f - elapsed / Mathf.Max(FadeSeconds, 0.05f), 0f, 1f);
            SetTextAlpha(_primaryText, alpha);
            SetTextAlpha(_secondaryText, alpha);
        }
    }

    private async Task FadeIntroBlackoutOut()
    {
        if (_introBlackout == null)
        {
            return;
        }

        _introBlackout.Visible = true;
        float elapsed = 0f;
        while (elapsed < IntroSceneBlendSeconds)
        {
            if (_skipOpeningRequested)
            {
                return;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float progress = Mathf.Clamp(elapsed / Mathf.Max(IntroSceneBlendSeconds, 0.05f), 0f, 1f);
            float alpha = 1f - EaseInOut(progress);
            SetIntroBlackoutAlpha(alpha);
        }

        SetIntroBlackoutAlpha(0f);
    }

    private void SetIntroBlackoutAlpha(float alpha)
    {
        if (_introBlackout == null)
        {
            return;
        }

        alpha = Mathf.Clamp(alpha, 0f, 1f);
        _introBlackout.Visible = alpha > 0.001f;
        _introBlackout.Color = new Color(0f, 0f, 0f, alpha);
    }

    private void ClearTexts()
    {
        if (_primaryText != null)
        {
            _primaryText.Text = "";
            _primaryText.Modulate = Colors.White with { A = 1f };
            _primaryText.Position = _primaryBasePosition;
        }

        if (_secondaryText != null)
        {
            _secondaryText.Text = "";
            _secondaryText.Modulate = Colors.White with { A = 1f };
            _secondaryText.Position = _secondaryBasePosition;
        }
    }

    private void EnablePlayer()
    {
        _openingFinished = true;
        _skipOpeningRequested = false;
        _playerUnlocked = true;
        _player?.SetControlEnabled(true);

        if (_flashlight != null)
        {
            _flashlight.ToggleEnabled = true;
            _flashlight.SetFlashlightEnabled(true);
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private bool IsIntroCancelled(int sequenceId)
    {
        return sequenceId != _introSequenceId || _skipOpeningRequested;
    }

    private void SkipOpening()
    {
        if (_openingFinished)
        {
            return;
        }

        _skipOpeningRequested = true;
        _introSequenceId++;
        _shakeBehindText = false;
        _rampWarningTextRed = false;
        ClearTexts();
        StopBehindPanic();
        SetIntroBlackoutAlpha(0f);
        EnablePlayer();
        UpdateDebugText();
    }

    private void ForceRevealMenu()
    {
        SkipOpening();

        if (_entityNoticeTriggered)
        {
            ShowMenu();
            return;
        }

        _entityNoticeTriggered = true;
        _ = FocusEntityAsync();
    }

    private void CheckEntityNotice()
    {
        if (!_playerUnlocked || _entityNoticeTriggered || _camera == null || _entity == null)
        {
            return;
        }

        Vector3 toEntity = GetNoticeTargetPosition() - _camera.GlobalPosition;
        float distance = toEntity.Length();
        if (distance > NoticeDistance || distance <= 0.01f)
        {
            return;
        }

        Vector3 forward = -_camera.GlobalTransform.Basis.Z.Normalized();
        float dot = forward.Dot(toEntity.Normalized());
        if (dot < NoticeDotThreshold)
        {
            return;
        }

        _entityNoticeTriggered = true;
        _ = FocusEntityAsync();
    }

    private Vector3 GetNoticeTargetPosition()
    {
        if (_leftEyeGlow != null && _rightEyeGlow != null)
        {
            return (_leftEyeGlow.GlobalPosition + _rightEyeGlow.GlobalPosition) * 0.5f;
        }

        if (_entityVisual != null)
        {
            return _entityVisual.GlobalPosition;
        }

        return _entity?.GlobalPosition ?? Vector3.Zero;
    }

    private void UpdateEntityEyeBillboard()
    {
        if (_camera == null || _entityVisual == null || _leftEyeGlow == null || _rightEyeGlow == null)
        {
            return;
        }

        Vector3 origin = _entityVisual.GlobalPosition;
        Vector3 toCamera = (_camera.GlobalPosition - origin).Normalized();
        if (toCamera.LengthSquared() <= 0.0001f)
        {
            return;
        }

        // Sprite3D billboarding is render-only, so separate eye meshes need to be
        // projected onto the same camera-facing plane every frame.
        Vector3 right = _camera.GlobalTransform.Basis.X.Normalized();
        Vector3 up = _camera.GlobalTransform.Basis.Y.Normalized();
        Vector3 forward = toCamera;

        _leftEyeGlow.GlobalPosition = origin + right * LeftEyeSpriteOffset.X + up * LeftEyeSpriteOffset.Y + forward * EyeDepthOffset;
        _rightEyeGlow.GlobalPosition = origin + right * RightEyeSpriteOffset.X + up * RightEyeSpriteOffset.Y + forward * EyeDepthOffset;

        if (_eyeLight != null)
        {
            _eyeLight.GlobalPosition = (_leftEyeGlow.GlobalPosition + _rightEyeGlow.GlobalPosition) * 0.5f + forward * 0.16f;
        }
    }

    private async Task FocusEntityAsync()
    {
        if (_player == null || _camera == null || _entity == null)
        {
            return;
        }

        _introSequenceId++;
        _skipOpeningRequested = true;
        _shakeBehindText = false;
        _rampWarningTextRed = false;
        ClearTexts();
        StopBehindPanic();
        SetIntroBlackoutAlpha(0f);
        if (_flashlight != null)
        {
            _flashlight.SetFlashlightEnabled(false);
            _flashlight.ToggleEnabled = false;
        }

        if (_flashlightModel != null)
        {
            _flashlightModel.Visible = false;
        }

        _player.SetControlEnabled(false);
        Input.MouseMode = Input.MouseModeEnum.Visible;

        Vector3 startPosition = _player.GlobalPosition;
        Vector3 targetPosition = _entity.GlobalPosition + FocusCameraOffset;
        targetPosition.Y = Mathf.Max(targetPosition.Y, 1.05f);
        float startFov = _camera.Fov;
        Basis startPlayerBasis = _player.GlobalTransform.Basis;
        Basis startCameraBasis = _camera.GlobalTransform.Basis;
        float elapsed = 0f;

        while (elapsed < FocusCatchSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float progress = Mathf.Clamp(elapsed / Mathf.Max(FocusCatchSeconds, 0.05f), 0f, 1f);
            float eased = EaseInOut(progress);
            _player.GlobalPosition = startPosition.Lerp(targetPosition, eased);
            SmoothLookAtReveal(_player, _camera, startPlayerBasis, startCameraBasis, eased);
            _camera.Fov = startFov;
        }

        _player.GlobalPosition = targetPosition;
        SmoothLookAtReveal(_player, _camera, startPlayerBasis, startCameraBasis, 1f);

        elapsed = 0f;
        while (elapsed < FocusZoomSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float progress = Mathf.Clamp(elapsed / Mathf.Max(FocusZoomSeconds, 0.05f), 0f, 1f);
            float eased = EaseInOut(progress);
            SmoothLookAtReveal(_player, _camera, startPlayerBasis, startCameraBasis, 1f);
            _camera.Fov = Mathf.Lerp(startFov, FocusTargetFov, eased);
        }

        _camera.Fov = FocusTargetFov;

        ShowMenu();
    }

    private static float EaseInOut(float progress)
    {
        progress = Mathf.Clamp(progress, 0f, 1f);
        return progress * progress * (3f - 2f * progress);
    }

    private void SmoothLookAtReveal(Node3D player, Camera3D camera, Basis startPlayerBasis, Basis startCameraBasis, float progress)
    {
        Transform3D playerLook = player.GlobalTransform;
        playerLook = playerLook.LookingAt(_entity!.GlobalPosition, Vector3.Up);
        Transform3D cameraLook = camera.GlobalTransform;
        cameraLook = cameraLook.LookingAt(_entity.GlobalPosition, Vector3.Up);
        float easedRotation = EaseInOut(progress);

        Transform3D playerTransform = player.GlobalTransform;
        playerTransform.Basis = startPlayerBasis.Slerp(playerLook.Basis, easedRotation).Orthonormalized();
        player.GlobalTransform = playerTransform;

        Transform3D cameraTransform = camera.GlobalTransform;
        cameraTransform.Basis = startCameraBasis.Slerp(cameraLook.Basis, easedRotation).Orthonormalized();
        camera.GlobalTransform = cameraTransform;
    }

    private static float EaseOutCubic(float progress)
    {
        progress = Mathf.Clamp(progress, 0f, 1f);
        float inverse = 1f - progress;
        return 1f - inverse * inverse * inverse;
    }

    private void ShowMenu()
    {
        ClearTexts();
        StopBehindPanic();
        ApplyMenuVhsLook();
        if (_menu3D != null)
        {
            ApplyMenuLightingMode(_menu3D);
        }

        if (_camera != null)
        {
            _menuCameraBaseTransform = _camera.GlobalTransform;
            _menuCameraTilt = Vector2.Zero;
        }

        _menuVisible = true;

        if (_menu3D != null)
        {
            _menu3D.Visible = true;
            _ = FadeMenu3DAsync();
        }

        _ = FadeInRealLobbyAudioAsync();

        if (_menuLayer != null)
        {
            _menuLayer.Visible = false;
        }
    }

    private void ApplyMenuVhsLook()
    {
        if (_vhsOverlay == null)
        {
            return;
        }

        _vhsOverlay.Intensity = 1f;
        _vhsOverlay.TapeResolution = new Vector2(280f, 170f);
        _vhsOverlay.PixelationStrength = 0.68f;
        _vhsOverlay.NoiseStrength = 0.46f;
        _vhsOverlay.ChromaticAberration = 0.08f;
        _vhsOverlay.RandomJitterStrength = 0.32f;
        _vhsOverlay.WobbleStrength = 0.5f;
        _vhsOverlay.TrackingStrength = 0.52f;
        _vhsOverlay.WhiteLineStrength = 0.62f;
        _vhsOverlay.SceneBrightness = 1.02f;
        _vhsOverlay.SceneLift = 0.12f;
        _vhsOverlay.CameraContrast = 1.05f;
        _vhsOverlay.CameraSaturation = 0.68f;
        _vhsOverlay.VignetteStrength = 0.42f;
        _vhsOverlay.GlareStrength = 0.42f;
    }

    private void UpdateMenuCameraTilt(float delta)
    {
        if (!_menuVisible || _camera == null)
        {
            return;
        }

        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize.X <= 1f || viewportSize.Y <= 1f)
        {
            return;
        }

        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 normalized = new(
            Mathf.Clamp((mouse.X / viewportSize.X - 0.5f) * 2f, -1f, 1f),
            Mathf.Clamp((mouse.Y / viewportSize.Y - 0.5f) * 2f, -1f, 1f));

        Vector2 targetTilt = normalized * Mathf.DegToRad(MenuMouseTiltDegrees);
        float blend = 1f - Mathf.Exp(-MenuMouseTiltSpeed * delta);
        _menuCameraTilt = _menuCameraTilt.Lerp(targetTilt, blend);

        Transform3D transform = _menuCameraBaseTransform;
        Basis basis = transform.Basis.Rotated(Vector3.Up, -_menuCameraTilt.X);
        basis = basis.Rotated(basis.X.Normalized(), -_menuCameraTilt.Y * 0.72f);
        transform.Basis = basis.Orthonormalized();
        _camera.GlobalTransform = transform;
    }

    private void ApplyMenuLightingMode(Node node)
    {
        if (!MenuIgnoresLighting)
        {
            return;
        }

        if (node is GeometryInstance3D geometry)
        {
            geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }

        if (node is MeshInstance3D meshInstance)
        {
            for (int surfaceIndex = 0; surfaceIndex < meshInstance.GetSurfaceOverrideMaterialCount(); surfaceIndex++)
            {
                Material? material = meshInstance.GetSurfaceOverrideMaterial(surfaceIndex);
                if (material is BaseMaterial3D baseMaterial)
                {
                    baseMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                    baseMaterial.DisableReceiveShadows = true;
                    baseMaterial.NoDepthTest = true;
                    baseMaterial.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled;
                    baseMaterial.RenderPriority = (int)Material.RenderPriorityMax;
                    if (baseMaterial.AlbedoColor.A < 0.98f)
                    {
                        baseMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                        baseMaterial.AlbedoColor = baseMaterial.AlbedoColor with { A = 1f };
                    }
                }
            }
        }

        if (node is Label3D label)
        {
            label.Shaded = false;
            label.NoDepthTest = true;
            label.OutlineSize = 0;
            label.OutlineModulate = new Color(0f, 0f, 0f, 0f);
            label.RenderPriority = (int)Material.RenderPriorityMax;
            label.OutlineRenderPriority = (int)Material.RenderPriorityMax - 1;
        }

        foreach (Node child in node.GetChildren())
        {
            ApplyMenuLightingMode(child);
        }
    }

    private void OnPlayAreaInput(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (_menuVisible && @event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            PlayGame();
        }
    }

    private void HandleMenuClick(Vector2 screenPosition)
    {
        MenuHoverTarget target = GetMenuTargetAtScreenPosition(screenPosition);
        if (target == MenuHoverTarget.Play)
        {
            PlayGame();
        }
        else if (target == MenuHoverTarget.Credits)
        {
            ToggleCredits();
        }
        else if (target == MenuHoverTarget.Settings)
        {
            ToggleSettings();
        }
        else if (target == MenuHoverTarget.AudioSlider)
        {
            _audioSliderDragging = true;
            SetVolumeFromSliderScreenPosition(screenPosition);
        }
        else if (target == MenuHoverTarget.AudioMute)
        {
            ToggleRealLobbyMute();
        }
        else if (target == MenuHoverTarget.SettingsBack)
        {
            SetSettingsSubmenuVisible(false);
        }
        else if (target == MenuHoverTarget.CreditsBack)
        {
            SetCreditsSubmenuVisible(false);
        }
    }

    private MenuHoverTarget GetMenuTargetAtScreenPosition(Vector2 screenPosition)
    {
        if (_camera == null || !_menuVisible)
        {
            return MenuHoverTarget.None;
        }

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 end = origin + _camera.ProjectRayNormal(screenPosition) * 120f;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollideWithAreas = true;
        query.CollideWithBodies = false;
        Dictionary result = spaceState.IntersectRay(query);

        if (!result.TryGetValue("collider", out Variant colliderVariant))
        {
            return MenuHoverTarget.None;
        }

        Node? collider = colliderVariant.AsGodotObject() as Node;
        if (collider == _playArea)
        {
            return MenuHoverTarget.Play;
        }

        if (collider == _creditsArea)
        {
            return MenuHoverTarget.Credits;
        }

        if (collider == _settingsArea)
        {
            return MenuHoverTarget.Settings;
        }

        if (_settingsOpen && collider == _audioDownArea)
        {
            return MenuHoverTarget.AudioDown;
        }

        if (_settingsOpen && collider == _audioUpArea)
        {
            return MenuHoverTarget.AudioUp;
        }

        if (_settingsOpen && collider == _audioSliderArea)
        {
            return MenuHoverTarget.AudioSlider;
        }

        if (_settingsOpen && collider == _audioMuteArea)
        {
            return MenuHoverTarget.AudioMute;
        }

        if (_settingsOpen && collider == _settingsBackArea)
        {
            return MenuHoverTarget.SettingsBack;
        }

        if (_creditsOpen && collider == _creditsBackArea)
        {
            return MenuHoverTarget.CreditsBack;
        }

        return MenuHoverTarget.None;
    }

    private void UpdateMenuHover(float delta)
    {
        if (!_menuVisible || _menu3D == null || _settingsAnimating || _creditsAnimating || _audioSliderDragging)
        {
            if (_hoverTarget != MenuHoverTarget.None)
            {
                ResetMenuHoverVisuals();
                _hoverTarget = MenuHoverTarget.None;
            }

            return;
        }

        MenuHoverTarget target = GetMenuTargetAtScreenPosition(GetViewport().GetMousePosition());
        if (target != _hoverTarget)
        {
            ResetMenuHoverVisuals();
            _hoverTarget = target;
        }

        if (_hoverTarget == MenuHoverTarget.None)
        {
            return;
        }

        ApplyMenuHoverGlitch(_hoverTarget, delta);
    }

    private void CaptureMenuVisualState(Node node)
    {
        if (node is Node3D node3D)
        {
            _menuBaseTransforms[node3D] = node3D.Transform;
        }

        if (node is GeometryInstance3D geometry)
        {
            _menuBaseTransparencies[geometry] = geometry.Transparency;
        }

        if (node is Label3D label)
        {
            _menuBaseLabelColors[label] = label.Modulate;
            _menuBaseOutlineColors[label] = label.OutlineModulate;
        }

        foreach (Node child in node.GetChildren())
        {
            CaptureMenuVisualState(child);
        }
    }

    private void ResetMenuHoverVisuals()
    {
        foreach ((Node3D node, Transform3D transform) in _menuBaseTransforms)
        {
            if (IsInstanceValid(node))
            {
                node.Transform = transform;
            }
        }

        foreach ((Label3D label, Color baseColor) in _menuBaseLabelColors)
        {
            if (!IsInstanceValid(label))
            {
                continue;
            }

            float alpha = label.Modulate.A;
            label.Modulate = baseColor with { A = alpha };

            if (_menuBaseOutlineColors.TryGetValue(label, out Color outline))
            {
                float outlineAlpha = label.OutlineModulate.A;
                label.OutlineModulate = outline with { A = outlineAlpha };
            }
        }

        foreach ((GeometryInstance3D geometry, float transparency) in _menuBaseTransparencies)
        {
            if (IsInstanceValid(geometry))
            {
                geometry.Transparency = transparency;
            }
        }
    }

    private void ApplyMenuHoverGlitch(MenuHoverTarget target, float delta)
    {
        float flicker = _random.RandfRange(0.82f, 1.18f);
        float dropout = _random.Randf() > 0.78f ? _random.RandfRange(0.08f, 0.26f) : 0f;

        foreach ((GeometryInstance3D geometry, float baseTransparency) in _menuBaseTransparencies)
        {
            if (!IsInstanceValid(geometry) || !BelongsToMenuButton(geometry, target))
            {
                continue;
            }

            geometry.Transparency = Mathf.Clamp(baseTransparency + dropout, 0f, 0.35f);
        }

        foreach ((Label3D label, Color baseColor) in _menuBaseLabelColors)
        {
            if (!IsInstanceValid(label) || !BelongsToMenuButton(label, target))
            {
                continue;
            }

            float alpha = label.Modulate.A;
            label.Modulate = new Color(
                Mathf.Clamp(baseColor.R * 1.25f * flicker, 0f, 1f),
                Mathf.Clamp(baseColor.G * 1.18f * flicker, 0f, 1f),
                Mathf.Clamp(baseColor.B * 0.92f * flicker + 0.12f, 0f, 1f),
                alpha);

            label.OutlineModulate = new Color(0f, 0f, 0f, 0f);
        }
    }

    private static bool BelongsToMenuButton(Node3D node, MenuHoverTarget target)
    {
        string name = node.Name.ToString();
        return target switch
        {
            MenuHoverTarget.Play => name.StartsWith("Play"),
            MenuHoverTarget.Credits => name.StartsWith("Credits") && name != "Credits3DLabel",
            MenuHoverTarget.Settings => name.StartsWith("Settings") && name != "Settings3DLabel",
            MenuHoverTarget.AudioDown => name.StartsWith("AudioDown"),
            MenuHoverTarget.AudioUp => name.StartsWith("AudioUp"),
            MenuHoverTarget.AudioSlider => name.StartsWith("AudioSlider"),
            MenuHoverTarget.AudioMute => name.StartsWith("AudioMute"),
            MenuHoverTarget.SettingsBack => name.StartsWith("SettingsBack"),
            MenuHoverTarget.CreditsBack => name.StartsWith("CreditsBack"),
            _ => false
        };
    }

    private async Task FadeInRealLobbyAudioAsync()
    {
        if (_realLobbyFadeStarted || _realLobbyAudio == null)
        {
            return;
        }

        _realLobbyFadeStarted = true;
        _realLobbyAudio.VolumeDb = -80f;
        if (!_realLobbyAudio.Playing)
        {
            _realLobbyAudio.Play();
        }

        float targetVolumeDb = _realLobbyMuted ? -80f : _currentRealLobbyVolumeDb;
        float elapsed = 0f;
        while (elapsed < RealLobbyFadeSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float progress = Mathf.Clamp(elapsed / Mathf.Max(RealLobbyFadeSeconds, 0.05f), 0f, 1f);
            float eased = progress * progress * (3f - 2f * progress);
            _realLobbyAudio.VolumeDb = Mathf.Lerp(-80f, targetVolumeDb, eased);
        }

        _realLobbyAudio.VolumeDb = targetVolumeDb;
    }

    private async Task FadeMenu3DAsync()
    {
        if (_menuFadeStarted || _menu3D == null)
        {
            return;
        }

        _menuFadeStarted = true;
        Array<Node> children = _menu3D.GetChildren();
        foreach (Node child in children)
        {
            SetNode3DAlpha(child, 0f);
        }

        foreach (Node child in children)
        {
            if (child is Area3D)
            {
                continue;
            }

            Tween tween = CreateTween();
            tween.TweenMethod(Callable.From<float>(alpha => SetNode3DAlpha(child, alpha)), 0f, 1f, 0.24f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            await WaitSeconds(0.13f);
        }
    }

    private static void SetNode3DAlpha(Node node, float alpha)
    {
        if (node is GeometryInstance3D geometry)
        {
            geometry.Transparency = 1f - alpha;
        }

        if (node is Label3D label)
        {
            Color modulate = label.Modulate;
            modulate.A = alpha;
            label.Modulate = modulate;
            Color outline = label.OutlineModulate;
            outline.A = alpha;
            label.OutlineModulate = outline;
        }

        foreach (Node child in node.GetChildren())
        {
            SetNode3DAlpha(child, alpha);
        }
    }

    private void OnCreditsAreaInput(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (_menuVisible && @event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            ToggleCredits();
        }
    }

    private void OnSettingsAreaInput(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (_menuVisible && @event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            ToggleSettings();
        }
    }

    private void PlayGame()
    {
        GetTree().ChangeSceneToFile(BaseScenePath);
    }

    private void ToggleCredits()
    {
        if (!_creditsOpen)
        {
            SetSettingsSubmenuImmediate(false);
        }

        _ = SetCreditsSubmenuVisibleAsync(!_creditsOpen);
    }

    private void SetCreditsSubmenuVisible(bool visible)
    {
        _ = SetCreditsSubmenuVisibleAsync(visible);
    }

    private void SetCreditsSubmenuImmediate(bool visible)
    {
        _creditsOpen = visible;
        _creditsAnimating = false;
        SetMainMenuButtonsVisible(!visible);
        ApplyMenuSlideOffsets(visible ? new Vector3(MenuSlideDistance, 0f, 0f) : Vector3.Zero, Vector3.Zero, visible ? Vector3.Zero : new Vector3(MenuSlideDistance, 0f, 0f));

        if (_creditsSubmenu != null)
        {
            _creditsSubmenu.Visible = visible;
        }

        if (_credits3DLabel != null)
        {
            _credits3DLabel.Visible = visible;
        }

        SetAreaCollision(_playArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsArea, visible ? 0u : 1u);
        SetAreaCollision(_settingsArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsBackArea, visible ? 1u : 0u);
    }

    private async Task SetCreditsSubmenuVisibleAsync(bool visible)
    {
        _creditsOpen = visible;
        _creditsAnimating = true;
        _audioSliderDragging = false;
        ResetMenuHoverVisuals();
        _hoverTarget = MenuHoverTarget.None;

        if (_creditsSubmenu != null)
        {
            _creditsSubmenu.Visible = true;
        }

        if (_credits3DLabel != null)
        {
            _credits3DLabel.Visible = true;
        }

        SetMainMenuButtonsVisible(true);
        SetAreaCollision(_playArea, 0u);
        SetAreaCollision(_creditsArea, 0u);
        SetAreaCollision(_settingsArea, 0u);
        SetAreaCollision(_creditsBackArea, 0u);

        Vector3 right = new(MenuSlideDistance, 0f, 0f);
        Vector3 mainStart = visible ? Vector3.Zero : right;
        Vector3 mainEnd = visible ? right : Vector3.Zero;
        Vector3 creditsStart = visible ? right : Vector3.Zero;
        Vector3 creditsEnd = visible ? Vector3.Zero : right;
        float elapsed = 0f;

        while (elapsed < MenuSlideSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            elapsed += (float)GetProcessDeltaTime();
            float progress = Mathf.Clamp(elapsed / Mathf.Max(MenuSlideSeconds, 0.05f), 0f, 1f);
            float eased = EaseInOut(progress);
            ApplyMenuSlideOffsets(mainStart.Lerp(mainEnd, eased), Vector3.Zero, creditsStart.Lerp(creditsEnd, eased));
        }

        ApplyMenuSlideOffsets(mainEnd, Vector3.Zero, creditsEnd);
        SetMainMenuButtonsVisible(!visible);

        if (_creditsSubmenu != null)
        {
            _creditsSubmenu.Visible = visible;
        }

        if (_credits3DLabel != null)
        {
            _credits3DLabel.Visible = visible;
        }

        SetAreaCollision(_playArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsArea, visible ? 0u : 1u);
        SetAreaCollision(_settingsArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsBackArea, visible ? 1u : 0u);
        _creditsAnimating = false;
    }

    private void ToggleSettings()
    {
        if (!_settingsOpen && _credits3DLabel != null)
        {
            _credits3DLabel.Visible = false;
        }

        _ = SetSettingsSubmenuVisibleAsync(!_settingsOpen);
    }

    private void SetSettingsSubmenuVisible(bool visible)
    {
        _ = SetSettingsSubmenuVisibleAsync(visible);
    }

    private void SetSettingsSubmenuImmediate(bool visible)
    {
        _settingsOpen = visible;
        _settingsAnimating = false;
        _audioSliderDragging = false;
        SetMainMenuButtonsVisible(!visible);
        ApplyMenuSlideOffsets(visible ? new Vector3(MenuSlideDistance, 0f, 0f) : Vector3.Zero, visible ? Vector3.Zero : new Vector3(MenuSlideDistance, 0f, 0f), Vector3.Zero);

        if (_settingsSubmenu != null)
        {
            _settingsSubmenu.Visible = visible;
        }

        if (_settings3DLabel != null)
        {
            _settings3DLabel.Visible = visible;
        }

        SetAreaCollision(_playArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsArea, visible ? 0u : 1u);
        SetAreaCollision(_settingsArea, visible ? 0u : 1u);
        SetAreaCollision(_audioDownArea, 0u);
        SetAreaCollision(_audioUpArea, 0u);
        SetAreaCollision(_audioSliderArea, visible ? 1u : 0u);
        SetAreaCollision(_audioMuteArea, visible ? 1u : 0u);
        SetAreaCollision(_settingsBackArea, visible ? 1u : 0u);
        HideLegacyAudioButtons();
    }

    private async Task SetSettingsSubmenuVisibleAsync(bool visible)
    {
        _settingsOpen = visible;
        _settingsAnimating = true;
        _audioSliderDragging = false;
        ResetMenuHoverVisuals();
        _hoverTarget = MenuHoverTarget.None;

        if (_settingsSubmenu != null)
        {
            _settingsSubmenu.Visible = true;
        }

        if (_settings3DLabel != null)
        {
            _settings3DLabel.Visible = true;
        }

        SetMainMenuButtonsVisible(true);
        SetAreaCollision(_playArea, 0u);
        SetAreaCollision(_creditsArea, 0u);
        SetAreaCollision(_settingsArea, 0u);
        SetAreaCollision(_audioDownArea, 0u);
        SetAreaCollision(_audioUpArea, 0u);
        SetAreaCollision(_audioSliderArea, 0u);
        SetAreaCollision(_audioMuteArea, 0u);
        SetAreaCollision(_settingsBackArea, 0u);
        HideLegacyAudioButtons();
        UpdateSettingsText();
        UpdateAudioSliderVisual();

        Vector3 right = new(MenuSlideDistance, 0f, 0f);
        Vector3 mainStart = visible ? Vector3.Zero : right;
        Vector3 mainEnd = visible ? right : Vector3.Zero;
        Vector3 settingsStart = visible ? right : Vector3.Zero;
        Vector3 settingsEnd = visible ? Vector3.Zero : right;
        float elapsed = 0f;

        while (elapsed < MenuSlideSeconds)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            elapsed += (float)GetProcessDeltaTime();
            float progress = Mathf.Clamp(elapsed / Mathf.Max(MenuSlideSeconds, 0.05f), 0f, 1f);
            float eased = EaseInOut(progress);
            Vector3 mainOffset = mainStart.Lerp(mainEnd, eased);
            Vector3 settingsOffset = settingsStart.Lerp(settingsEnd, eased);
            ApplyMenuSlideOffsets(mainOffset, settingsOffset, Vector3.Zero);
        }

        ApplyMenuSlideOffsets(mainEnd, settingsEnd, Vector3.Zero);
        SetMainMenuButtonsVisible(!visible);

        if (_settingsSubmenu != null)
        {
            _settingsSubmenu.Visible = visible;
        }

        if (_settings3DLabel != null)
        {
            _settings3DLabel.Visible = visible;
        }

        SetAreaCollision(_playArea, visible ? 0u : 1u);
        SetAreaCollision(_creditsArea, visible ? 0u : 1u);
        SetAreaCollision(_settingsArea, visible ? 0u : 1u);

        uint collisionLayer = visible ? 1u : 0u;
        SetAreaCollision(_audioSliderArea, collisionLayer);
        SetAreaCollision(_audioMuteArea, collisionLayer);
        SetAreaCollision(_settingsBackArea, collisionLayer);
        _settingsAnimating = false;
    }

    private void SetMainMenuButtonsVisible(bool visible)
    {
        if (_menu3D == null)
        {
            return;
        }

        foreach (Node child in _menu3D.GetChildren())
        {
            if (child is not Node3D node3D)
            {
                continue;
            }

            string name = node3D.Name.ToString();
            if (name == "SettingsSubmenu" || name == "Settings3DLabel" || name == "Credits3DLabel")
            {
                continue;
            }

            if (name.StartsWith("Play") || name.StartsWith("Credits") || name.StartsWith("Settings"))
            {
                node3D.Visible = visible;
            }
        }
    }

    private void ApplyMenuSlideOffsets(Vector3 mainOffset, Vector3 settingsOffset, Vector3 creditsOffset)
    {
        foreach ((Node3D node, Transform3D baseTransform) in _menuBaseTransforms)
        {
            if (!IsInstanceValid(node) || node == _menu3D)
            {
                continue;
            }

            string name = node.Name.ToString();
            bool isSettingsPanel = IsSettingsSubmenuNode(node) || name == "Settings3DLabel";
            bool isCreditsPanel = IsCreditsSubmenuNode(node) || name == "Credits3DLabel";
            bool isMainMenu = IsMainMenuVisualName(name);
            if (!isSettingsPanel && !isCreditsPanel && !isMainMenu)
            {
                continue;
            }

            Transform3D transform = baseTransform;
            transform.Origin += isSettingsPanel ? settingsOffset : isCreditsPanel ? creditsOffset : mainOffset;
            node.Transform = transform;
        }
    }

    private bool IsSettingsSubmenuNode(Node node)
    {
        Node? current = node;
        while (current != null && current != _menu3D)
        {
            if (current == _settingsSubmenu)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    private bool IsCreditsSubmenuNode(Node node)
    {
        Node? current = node;
        while (current != null && current != _menu3D)
        {
            if (current == _creditsSubmenu)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    private static bool IsMainMenuVisualName(string name)
    {
        return name == "Title3D"
            || name.StartsWith("Play")
            || (name.StartsWith("Credits") && name != "Credits3DLabel" && !name.StartsWith("CreditsBack"))
            || (name.StartsWith("Settings") && name != "Settings3DLabel" && !name.StartsWith("SettingsBack"));
    }

    private void HideLegacyAudioButtons()
    {
        SetLegacyAudioButtonVisible("AudioDown", false);
        SetLegacyAudioButtonVisible("AudioUp", false);
    }

    private void SetLegacyAudioButtonVisible(string prefix, bool visible)
    {
        if (_settingsSubmenu == null)
        {
            return;
        }

        foreach (Node child in _settingsSubmenu.GetChildren())
        {
            if (child is Node3D node3D && node3D.Name.ToString().StartsWith(prefix))
            {
                node3D.Visible = visible;
            }
        }
    }

    private static void SetAreaCollision(Area3D? area, uint collisionLayer)
    {
        if (area == null)
        {
            return;
        }

        area.CollisionLayer = collisionLayer;
        area.CollisionMask = collisionLayer;
        area.Monitorable = collisionLayer != 0;
        area.Monitoring = collisionLayer != 0;
    }

    private void AdjustRealLobbyVolume(float deltaDb)
    {
        if (_realLobbyMuted && deltaDb > 0f)
        {
            _realLobbyMuted = false;
        }

        _currentRealLobbyVolumeDb = Mathf.Clamp(_currentRealLobbyVolumeDb + deltaDb, -42f, -6f);
        RealLobbyVolumeDb = _currentRealLobbyVolumeDb;
        ApplyRealLobbyVolume();
        UpdateSettingsText();
        UpdateAudioSliderVisual();
        SaveSettings();
    }

    private void ToggleRealLobbyMute()
    {
        _realLobbyMuted = !_realLobbyMuted;
        ApplyRealLobbyVolume();
        UpdateSettingsText();
        UpdateAudioSliderVisual();
        SaveSettings();
    }

    private void ApplyRealLobbyVolume()
    {
        if (_realLobbyAudio == null)
        {
            return;
        }

        _realLobbyAudio.VolumeDb = _realLobbyMuted ? -80f : _currentRealLobbyVolumeDb;
    }

    private void UpdateSettingsText()
    {
        if (_settings3DLabel == null)
        {
            return;
        }

        string volume = _realLobbyMuted ? "MUTED" : $"{Mathf.RoundToInt(DbToPercent(_currentRealLobbyVolumeDb))}%";
        _settings3DLabel.Text =
            "SETTINGS\n" +
            $"LOBBY AUDIO: {volume}\n" +
            "DRAG SLIDER / MUTE";
    }

    private static float DbToPercent(float db)
    {
        return Mathf.Clamp(Mathf.Pow(10f, db / 20f) * 100f, 0f, 100f);
    }

    private void SetVolumeFromSliderScreenPosition(Vector2 screenPosition)
    {
        if (_camera == null || _settingsSubmenu == null)
        {
            return;
        }

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 end = origin + _camera.ProjectRayNormal(screenPosition) * 120f;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollideWithAreas = true;
        query.CollideWithBodies = false;
        Dictionary result = spaceState.IntersectRay(query);

        if (!result.TryGetValue("position", out Variant positionVariant))
        {
            return;
        }

        Vector3 local = _settingsSubmenu.ToLocal(positionVariant.AsVector3());
        float normalized = Mathf.InverseLerp(AudioSliderMinX, AudioSliderMaxX, local.X);
        SetRealLobbyVolumeNormalized(normalized);
    }

    private void SetRealLobbyVolumeNormalized(float normalized)
    {
        normalized = Mathf.Clamp(normalized, 0f, 1f);
        _realLobbyMuted = normalized <= 0.01f;
        _currentRealLobbyVolumeDb = Mathf.Lerp(-42f, -6f, normalized);
        RealLobbyVolumeDb = _currentRealLobbyVolumeDb;
        ApplyRealLobbyVolume();
        UpdateSettingsText();
        UpdateAudioSliderVisual();
        SaveSettings();
    }

    private void UpdateAudioSliderVisual()
    {
        float normalized = _realLobbyMuted ? 0f : Mathf.InverseLerp(-42f, -6f, _currentRealLobbyVolumeDb);
        float x = Mathf.Lerp(AudioSliderMinX, AudioSliderMaxX, Mathf.Clamp(normalized, 0f, 1f));

        if (_audioSliderKnob != null)
        {
            Vector3 position = _audioSliderKnob.Position;
            position.X = x;
            _audioSliderKnob.Position = position;
        }

        if (_audioSliderFill != null)
        {
            Vector3 position = _audioSliderFill.Position;
            Vector3 scale = _audioSliderFill.Scale;
            float width = Mathf.Max(x - AudioSliderMinX, 0.02f);
            position.X = AudioSliderMinX + width * 0.5f;
            scale.X = width / Mathf.Max(AudioSliderMaxX - AudioSliderMinX, 0.01f);
            _audioSliderFill.Position = position;
            _audioSliderFill.Scale = scale;
        }
    }

    private void LoadSettings()
    {
        ConfigFile config = new();
        Error error = config.Load(SettingsConfigPath);
        if (error != Error.Ok)
        {
            RealLobbyVolumeDb = _currentRealLobbyVolumeDb;
            return;
        }

        Variant volumeValue = config.GetValue(SettingsSection, LobbyVolumeKey, _currentRealLobbyVolumeDb);
        Variant mutedValue = config.GetValue(SettingsSection, LobbyMutedKey, false);
        _currentRealLobbyVolumeDb = Mathf.Clamp((float)volumeValue, -42f, -6f);
        RealLobbyVolumeDb = _currentRealLobbyVolumeDb;
        _realLobbyMuted = (bool)mutedValue;
    }

    private void SaveSettings()
    {
        ConfigFile config = new();
        config.SetValue(SettingsSection, LobbyVolumeKey, _currentRealLobbyVolumeDb);
        config.SetValue(SettingsSection, LobbyMutedKey, _realLobbyMuted);

        Error error = config.Save(SettingsConfigPath);
        if (error != Error.Ok)
        {
            GD.PushWarning($"{Name}: failed to save settings to {SettingsConfigPath}: {error}");
        }
    }

    private void BuildDebugMenu()
    {
        CanvasLayer layer = new()
        {
            Name = "IntroDebugLayer",
            Layer = 120,
            Visible = false
        };
        AddChild(layer);

        Control root = new()
        {
            Name = "IntroDebugMenu",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        layer.AddChild(root);
        _debugMenu = root;

        ColorRect panel = new()
        {
            Name = "Panel",
            Position = new Vector2(24f, 86f),
            Size = new Vector2(430f, 230f),
            Color = new Color(0.02f, 0.025f, 0.03f, 0.86f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(panel);

        _debugText = new Label
        {
            Name = "DebugText",
            Position = new Vector2(42f, 104f),
            Size = new Vector2(394f, 194f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _debugText.AddThemeColorOverride("font_color", new Color(0.88f, 0.95f, 0.84f, 0.95f));
        _debugText.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        _debugText.AddThemeConstantOverride("shadow_offset_x", 2);
        _debugText.AddThemeConstantOverride("shadow_offset_y", 2);
        _debugText.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(_debugText);
        UpdateDebugText();
    }

    private void SetDebugVisible(bool visible)
    {
        _debugVisible = visible;

        if (_debugMenu?.GetParent() is CanvasLayer layer)
        {
            layer.Visible = visible;
        }

        Input.MouseMode = visible || _menuVisible || !_playerUnlocked
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;
        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
        if (_debugText == null)
        {
            return;
        }

        _debugText.Text =
            "INTRO DEBUG (Ctrl+D)\n" +
            $"Opening: {(_openingFinished ? "done" : "playing")}\n" +
            $"Player unlocked: {_playerUnlocked}\n" +
            $"Entity noticed: {_entityNoticeTriggered}\n" +
            $"Menu visible: {_menuVisible}\n" +
            $"VHS: {(_vhsOverlay?.Enabled == true ? "on" : "off")}\n\n" +
            "Space/Enter - skip opening text\n" +
            "S - skip opening\n" +
            "R - force entity/menu reveal\n" +
            "F - toggle flashlight\n" +
            "V - toggle VHS\n" +
            "Esc - close debug";
    }

    private static void SetTextAlpha(RichTextLabel? label, float alpha)
    {
        if (label == null)
        {
            return;
        }

        Color modulate = label.Modulate;
        modulate.A = alpha;
        label.Modulate = modulate;
    }

    private async Task WaitSeconds(float seconds)
    {
        await ToSignal(GetTree().CreateTimer(Mathf.Max(seconds, 0f), processAlways: false), SceneTreeTimer.SignalName.Timeout);
    }
}
