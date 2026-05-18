using Godot;

public partial class IntroPillarField : Node3D
{
    [Export] public int Columns { get; set; } = 9;
    [Export] public int Rows { get; set; } = 15;
    [Export] public float ColumnSpacing { get; set; } = 5.2f;
    [Export] public float RowSpacing { get; set; } = 7.2f;
    [Export] public float WidthJitter { get; set; } = 0.55f;
    [Export] public float DepthJitter { get; set; } = 0.8f;
    [Export] public float CenterGapWidth { get; set; } = 3.2f;
    [Export] public float PillarHeight { get; set; } = 3.8f;
    [Export] public float PillarRadius { get; set; } = 0.55f;
    [Export] public int RandomSeed { get; set; } = 9137;
    [Export] public Material? PillarMaterial { get; set; }

    public override void _Ready()
    {
        RandomNumberGenerator random = new();
        random.Seed = (ulong)RandomSeed;

        CylinderMesh pillarMesh = new()
        {
            TopRadius = PillarRadius * 0.86f,
            BottomRadius = PillarRadius,
            Height = PillarHeight,
            RadialSegments = 48,
            Rings = 8
        };

        CylinderShape3D pillarShape = new()
        {
            Radius = PillarRadius,
            Height = PillarHeight
        };

        int halfColumns = Columns / 2;
        int halfRows = Rows / 2;

        for (int row = -halfRows; row <= halfRows; row++)
        {
            for (int column = -halfColumns; column <= halfColumns; column++)
            {
                float x = column * ColumnSpacing;
                float z = row * RowSpacing;

                if (Mathf.Abs(x) < CenterGapWidth && z < 42f && z > -36f)
                {
                    continue;
                }

                x += random.RandfRange(-WidthJitter, WidthJitter);
                z += random.RandfRange(-DepthJitter, DepthJitter);

                StaticBody3D body = new()
                {
                    Name = $"GeneratedPillar_{row + halfRows:00}_{column + halfColumns:00}",
                    Position = new Vector3(x, PillarHeight * 0.5f, z)
                };
                AddChild(body);

                MeshInstance3D mesh = new()
                {
                    Name = "Mesh",
                    Mesh = pillarMesh,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.On
                };
                if (PillarMaterial != null)
                {
                    mesh.SetSurfaceOverrideMaterial(0, PillarMaterial);
                }
                body.AddChild(mesh);

                CollisionShape3D collision = new()
                {
                    Name = "CollisionShape3D",
                    Shape = pillarShape
                };
                body.AddChild(collision);
            }
        }
    }
}
