using Godot;

public partial class LoopingAudioPlayer : AudioStreamPlayer
{
    [Export] public bool PlayOnReady { get; set; } = true;

    public override void _Ready()
    {
        Finished += Restart;

        if (PlayOnReady && Stream != null && !Playing)
        {
            Play();
        }
    }

    private void Restart()
    {
        if (Stream != null)
        {
            Play();
        }
    }
}
