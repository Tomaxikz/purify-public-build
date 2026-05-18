using Godot;

public partial class CamcorderHudController : Control
{
    [Export] public bool ShowHud { get; set; } = true;

    [ExportGroup("Labels")]
    [Export] public NodePath RecLabelPath { get; set; } = new NodePath("RecLabel");
    [Export] public NodePath RecDotPath { get; set; } = new NodePath("RecDot");
    [Export] public NodePath TimecodeLabelPath { get; set; } = new NodePath("TimecodeLabel");
    [Export] public NodePath ChapterLabelPath { get; set; } = new NodePath("ChapterLabel");
    [Export] public NodePath RecPathLabelPath { get; set; } = new NodePath("RecPathLabel");
    [Export] public NodePath SpeedLabelPath { get; set; } = new NodePath("SpeedLabel");
    [Export] public NodePath BatteryFillPath { get; set; } = new NodePath("BatteryFrame/BatteryFill");

    [ExportGroup("Recording")]
    [Export] public string CameraName { get; set; } = "CAM_CH1-01";
    [Export] public string ChapterText { get; set; } = "CHAPTER 1";
    [Export] public string TapeSpeed { get; set; } = "SP";
    [Export(PropertyHint.Range, "1,60,1")] public int TimecodeFps { get; set; } = 30;
    [Export(PropertyHint.Range, "0,7200,0.1")] public float TimecodeStartSeconds { get; set; } = 44.2f;
    [Export(PropertyHint.Range, "0.25,8,0.05")] public float RecBlinkSpeed { get; set; } = 1.45f;

    [ExportGroup("Wear")]
    [Export(PropertyHint.Range, "0,4,0.1")] public float HudJitterPixels { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "1,24,1")] public float HudJitterRate { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BatteryLevel { get; set; } = 0.68f;
    [Export(PropertyHint.Range, "8,120,1")] public float BatteryFillWidth { get; set; } = 52f;

    private Label? _recLabel;
    private ColorRect? _recDot;
    private Label? _timecodeLabel;
    private Label? _chapterLabel;
    private Label? _recPathLabel;
    private Label? _speedLabel;
    private ColorRect? _batteryFill;
    private readonly RandomNumberGenerator _random = new();
    private Vector2 _basePosition;
    private Vector2 _jitterOffset;
    private double _elapsedSeconds;
    private double _nextJitterTime;

    public override void _Ready()
    {
        _random.Randomize();
        _basePosition = Position;
        ResolveNodes();
        UpdateStaticLabels();
    }

    public override void _Process(double delta)
    {
        _elapsedSeconds += delta;
        Visible = ShowHud;

        if (!ShowHud)
        {
            return;
        }

        UpdateJitter();
        UpdateRecordingLabels();
        UpdateBattery();
    }

    private void ResolveNodes()
    {
        _recLabel = GetNodeOrNull<Label>(RecLabelPath);
        _recDot = GetNodeOrNull<ColorRect>(RecDotPath);
        _timecodeLabel = GetNodeOrNull<Label>(TimecodeLabelPath);
        _chapterLabel = GetNodeOrNull<Label>(ChapterLabelPath);
        _recPathLabel = GetNodeOrNull<Label>(RecPathLabelPath);
        _speedLabel = GetNodeOrNull<Label>(SpeedLabelPath);
        _batteryFill = GetNodeOrNull<ColorRect>(BatteryFillPath);
    }

    private void UpdateStaticLabels()
    {
        if (_recLabel != null)
        {
            _recLabel.Text = "REC";
        }

        if (_chapterLabel != null)
        {
            _chapterLabel.Text = ChapterText;
        }

        if (_recPathLabel != null)
        {
            _recPathLabel.Text = $"REC PATH\n{CameraName}";
        }

        if (_speedLabel != null)
        {
            _speedLabel.Text = TapeSpeed;
        }
    }

    private void UpdateRecordingLabels()
    {
        bool blinkOn = Mathf.Sin((float)_elapsedSeconds * RecBlinkSpeed * Mathf.Tau) > -0.2f;

        if (_recLabel != null)
        {
            _recLabel.Visible = blinkOn;
        }

        if (_recDot != null)
        {
            _recDot.Visible = blinkOn;
        }

        if (_timecodeLabel != null)
        {
            _timecodeLabel.Text = FormatTimecode(_elapsedSeconds + TimecodeStartSeconds);
        }
    }

    private void UpdateJitter()
    {
        double rate = Mathf.Max(HudJitterRate, 1f);
        if (_elapsedSeconds >= _nextJitterTime)
        {
            _nextJitterTime = _elapsedSeconds + (1.0 / rate);
            _jitterOffset = new Vector2(
                _random.RandfRange(-HudJitterPixels, HudJitterPixels),
                _random.RandfRange(-HudJitterPixels, HudJitterPixels));
        }

        Position = _basePosition + _jitterOffset;
    }

    private void UpdateBattery()
    {
        if (_batteryFill == null)
        {
            return;
        }

        float pulse = Mathf.Sin((float)_elapsedSeconds * 2.1f) * 0.025f;
        float charge = Mathf.Clamp(BatteryLevel + pulse, 0.04f, 1f);
        Vector2 size = _batteryFill.Size;
        size.X = BatteryFillWidth * charge;
        _batteryFill.Size = size;
        _batteryFill.Color = charge < 0.18f
            ? new Color(0.95f, 0.2f, 0.16f, 0.86f)
            : new Color(0.9f, 0.86f, 0.58f, 0.82f);
    }

    private string FormatTimecode(double seconds)
    {
        int fps = Mathf.Max(TimecodeFps, 1);
        int totalSeconds = Mathf.Max((int)seconds, 0);
        int frames = (int)((seconds - totalSeconds) * fps) % fps;
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds / 60) % 60;
        int displaySeconds = totalSeconds % 60;

        return $"{hours:00}:{minutes:00}:{displaySeconds:00}:{frames:00}";
    }
}
