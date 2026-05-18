using Godot;

public partial class LightingTestObjects : Node3D
{
    private const string SpawnedMetaKey = "lighting_test_spawned";

    [Export] public bool SpawnOnReady { get; set; } = true;
    [Export] public bool ClearSpawnedBeforeBuild { get; set; } = true;
    [Export] public bool AddCollision { get; set; } = true;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;
    [Export(PropertyHint.Range, "1,8,0.25")] public float ObjectSpacing { get; set; } = 4f;
    [Export] public Color WarmMaterialColor { get; set; } = new Color(0.72f, 0.64f, 0.49f);
    [Export] public Color CoolMaterialColor { get; set; } = new Color(0.42f, 0.52f, 0.58f);
    [Export] public Color DarkMaterialColor { get; set; } = new Color(0.24f, 0.25f, 0.23f);

    public override void _Ready()
    {
        if (SpawnOnReady)
        {
            BuildObjects();
        }
    }

    public void BuildObjects()
    {
        if (ClearSpawnedBeforeBuild)
        {
            ClearSpawnedChildren();
        }

        StandardMaterial3D warm = CreateMaterial("Warm Matte", WarmMaterialColor, 0.92f);
        StandardMaterial3D cool = CreateMaterial("Cool Matte", CoolMaterialColor, 0.86f);
        StandardMaterial3D dark = CreateMaterial("Dark Rough", DarkMaterialColor, 0.98f);

        AddSolidObject(
            "Shadow Cube",
            new BoxMesh { Size = Vector3.One },
            new BoxShape3D { Size = Vector3.One * 1.1f },
            new Vector3(-ObjectSpacing, 0.55f, 0f),
            Vector3.One * 1.1f,
            warm);

        AddSolidObject(
            "Haze Sphere",
            new SphereMesh { Radius = 0.65f, Height = 1.3f, RadialSegments = 32, Rings = 16 },
            new SphereShape3D { Radius = 0.65f },
            new Vector3(0f, 0.68f, 0f),
            Vector3.One,
            cool);

        AddSolidObject(
            "Value Cylinder",
            new CylinderMesh { TopRadius = 0.55f, BottomRadius = 0.55f, Height = 1.6f, RadialSegments = 32 },
            new CylinderShape3D { Radius = 0.55f, Height = 1.6f },
            new Vector3(ObjectSpacing, 0.8f, 0f),
            Vector3.One,
            dark);

        AddArch(new Vector3(ObjectSpacing * 2f, 0f, 0f), dark);
    }

    private void AddArch(Vector3 origin, Material material)
    {
        BoxMesh archMesh = new BoxMesh { Size = Vector3.One };
        AddBoxSegment("Arch Left", archMesh, origin + new Vector3(-0.65f, 1.05f, 0f), new Vector3(0.35f, 2.1f, 0.75f), material);
        AddBoxSegment("Arch Right", archMesh, origin + new Vector3(0.65f, 1.05f, 0f), new Vector3(0.35f, 2.1f, 0.75f), material);
        AddBoxSegment("Arch Top", archMesh, origin + new Vector3(0f, 2.25f, 0f), new Vector3(1.65f, 0.35f, 0.75f), material);
    }

    private void AddBoxSegment(string objectName, Mesh mesh, Vector3 position, Vector3 size, Material material)
    {
        AddSolidObject(objectName, mesh, new BoxShape3D { Size = size }, position, size, material);
    }

    private StaticBody3D AddSolidObject(string objectName, Mesh mesh, Shape3D collisionShape, Vector3 position, Vector3 scale, Material material)
    {
        StaticBody3D body = new StaticBody3D
        {
            Name = objectName,
            Position = position,
            CollisionLayer = CollisionLayer,
            CollisionMask = CollisionMask
        };

        MeshInstance3D meshInstance = new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = mesh,
            Scale = scale
        };

        meshInstance.SetSurfaceOverrideMaterial(0, material);
        body.AddChild(meshInstance);

        if (AddCollision)
        {
            CollisionShape3D shape = new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = collisionShape
            };

            body.AddChild(shape);
        }

        body.SetMeta(SpawnedMetaKey, true);
        AddChild(body);
        return body;
    }

    private static StandardMaterial3D CreateMaterial(string materialName, Color color, float roughness)
    {
        return new StandardMaterial3D
        {
            ResourceName = materialName,
            AlbedoColor = color,
            Roughness = roughness,
            Metallic = 0f
        };
    }

    private void ClearSpawnedChildren()
    {
        foreach (Node child in GetChildren())
        {
            if (child.HasMeta(SpawnedMetaKey))
            {
                child.QueueFree();
            }
        }
    }
}
