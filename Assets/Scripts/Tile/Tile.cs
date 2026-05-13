using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int z;
    public bool isWalkable = true;
    public int moveCost = 1;

    [Header("可视化")]
    [SerializeField] private Material groundMaterial;   // 半透明底纹材质
    [SerializeField] private Material borderMaterial;   // 边框材质

    private void Awake()
    {
        groundMaterial = Resources.Load<Material>("Art/Materials/gridGround");
        borderMaterial = Resources.Load<Material>("Art/Materials/gridBorder");
    }

    private void Start()
    {
        CreateGroundPlane();
        CreateBorder();
    }

    private void CreateGroundPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.transform.SetParent(transform);
        plane.transform.localPosition = Vector3.zero;

        // 关键：将 Quad 旋转 -90 度绕 X 轴，使其平躺（垂直于 Y 轴）
        plane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        // 缩放：Quad 原始顶点在 XY 平面 (-0.5,-0.5) 到 (0.5,0.5)
        // 旋转后：原 X → 世界 X，原 Y → 世界 Z，原 Z 无用
        // 因此 cellSize 对应原 X 和原 Y 的缩放
        float size = 5f; // 或从 GridManager 获取 cellSize
        plane.transform.localScale = new Vector3(size, size, 1f);

        plane.GetComponent<Renderer>().material = groundMaterial;
        Destroy(plane.GetComponent<Collider>());
    }

    private void CreateBorder()
    {
        // 使用 LineRenderer 画矩形边框
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.material = borderMaterial;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.loop = true;
        lr.positionCount = 4;
        float halfSize = 2.5f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfSize, 0.05f, -halfSize),
            new Vector3( halfSize, 0.05f, -halfSize),
            new Vector3( halfSize, 0.05f,  halfSize),
            new Vector3(-halfSize, 0.05f,  halfSize)
        };
        lr.SetPositions(corners);
    }
}