using Godot;

public partial class FirstPersonFlashlightRig : Node3D
{
	[Export] public NodePath ArmsPath { get; set; } = new NodePath("FirstPersonArms");
	[Export] public NodePath FlashlightBodyPath { get; set; } = new NodePath("FlashlightBody");
	[Export] public bool AttachFlashlightToWrist { get; set; } = true;
	[Export] public string WristBoneName { get; set; } = "R_wrist_027";
	[Export] public Vector3 WristPositionOffset { get; set; } = new Vector3(0.002f, -0.018f, -0.035f);
	[Export] public Vector3 WristRotationOffsetDegrees { get; set; } = new Vector3(82f, 2f, -92f);
	[Export] public Vector3 FlashlightScale { get; set; } = new Vector3(0.22f, 0.22f, 0.22f);

	private Node3D? _arms;
	private Node3D? _flashlightBody;
	private Skeleton3D? _skeleton;
	private int _wristBoneIndex = -1;

	public override void _Ready()
	{
		_arms = GetNodeOrNull<Node3D>(ArmsPath);
		_flashlightBody = GetNodeOrNull<Node3D>(FlashlightBodyPath);
		_skeleton = _arms == null ? null : FindSkeleton(_arms);
		_wristBoneIndex = _skeleton?.FindBone(WristBoneName) ?? -1;

		ApplyFlashlightScale();
		UpdateAttachment();
	}

	public override void _Process(double delta)
	{
		UpdateAttachment();
	}

	private void UpdateAttachment()
	{
		if (!AttachFlashlightToWrist || _flashlightBody == null || _skeleton == null || _wristBoneIndex < 0)
		{
			return;
		}

		Transform3D wristTransform = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_wristBoneIndex);
		Basis offsetBasis = Basis.Identity
			.Rotated(Vector3.Right, Mathf.DegToRad(WristRotationOffsetDegrees.X))
			.Rotated(Vector3.Up, Mathf.DegToRad(WristRotationOffsetDegrees.Y))
			.Rotated(Vector3.Forward, Mathf.DegToRad(WristRotationOffsetDegrees.Z))
			.Scaled(FlashlightScale);

		Transform3D target = wristTransform * new Transform3D(offsetBasis, WristPositionOffset);
		_flashlightBody.GlobalTransform = target;
	}

	private void ApplyFlashlightScale()
	{
		if (_flashlightBody == null)
		{
			return;
		}

		Transform3D transform = _flashlightBody.Transform;
		transform.Basis = transform.Basis.Orthonormalized().Scaled(FlashlightScale);
		_flashlightBody.Transform = transform;
	}

	private static Skeleton3D? FindSkeleton(Node node)
	{
		if (node is Skeleton3D skeleton)
		{
			return skeleton;
		}

		foreach (Node child in node.GetChildren())
		{
			Skeleton3D? found = FindSkeleton(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}
