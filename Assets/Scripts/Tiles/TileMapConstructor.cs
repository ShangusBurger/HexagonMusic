using CubeCoordinates;
using UnityEngine;

public class TileMapConstructor : MonoBehaviour
{
    public static TileMapConstructor Instance;
    public GameObject coordinatesGroup;

    // Prefab for the tile to be instantiated
    public GameObject tilePrefab;
    
    // Default grid size for instantiating via rows/columns
    [SerializeField] private Vector2Int defaultGridSizeRect;
    
    // Default grid size for instantiating via radius
    [SerializeField] private int defaultGridRadius;

    //Start with main tower in center
    [SerializeField] private bool startWithCenterTower = true;

    public static Container allTiles;

    // Prefabs for Towers
    public GameObject defaultTowerPrefab;
    public GameObject sourceTowerPrefab;
    public GameObject monoTowerPrefab;
    public GameObject splitterTowerPrefab;
    public GameObject sinkTowerPrefab;
    public GameObject lobberTowerPrefab;
    public GameObject sprayerTowerPrefab;
    public GameObject bufferTowerPrefab;
    public GameObject switcherTowerPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
        allTiles = ConstructGrid();
        
        if (startWithCenterTower)
        {
            GroundTile centerTile = Coordinates.Instance.GetContainer().GetCoordinate(Vector3.zero).go.GetComponent<GroundTile>();
            centerTile.AddTowerToTile(TowerType.Source);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    Container ConstructGrid(int gridRadius = -1)
    {
        if (gridRadius == -1)
            gridRadius = defaultGridRadius;

        Coordinates coordinates = Coordinates.Instance;
        coordinates.SetCoordinateType(Coordinate.Type.Prefab, tilePrefab);
        coordinates.CreateCoordinates(
            Cubes.GetNeighbors(Vector3.zero, gridRadius)
        );
        coordinatesGroup = coordinates.Build();
        foreach (Coordinate coord in coordinates.GetContainer().GetAllCoordinates())
        {
            coord.go.GetComponent<GroundTile>().tileCoordinate = coord;
        }

        return coordinates.GetContainer();
    }
}
