using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using Random = UnityEngine.Random;

using UnityEngine.Experimental.U2D;
using UnityEngine.UIElements;
using Cinemachine.Utility;
using UnityEngine.Splines; 

public enum Orientation
{
    Horizontal = 0,
    Vertical = 1
}
public class Line
{
    public Orientation orientation;
    public Vector2Int coordinates;

    public Line(Orientation _orientation, Vector2Int _coordinates)
    {
        orientation = _orientation;
        coordinates = _coordinates;
    }
}

public class BinarySpacePartitioner
{
    public RoomNode rootNode;


    public BinarySpacePartitioner(int _DungeonWidth, int _DungeonLenght)
    {
        this.rootNode = new RoomNode(new Vector2Int(0, 0), new Vector2Int(_DungeonWidth, _DungeonLenght), null, 0);
    }

    public List<RoomNode> PrepareNodesCollections(int maxIterations, int roomWidthMin, int roomLenghtMin)
    {
        Queue<RoomNode> graph = new Queue<RoomNode>();
        List<RoomNode> listToReturn = new List<RoomNode>();

        graph.Enqueue(rootNode);
        listToReturn.Add(rootNode);
        int iterations = 0;
        while (iterations < maxIterations && graph.Count > 0)
        {
            iterations++;
            RoomNode currentNode = graph.Dequeue();
            if (currentNode.Width >= roomWidthMin * 2 || currentNode.Lenght >= roomLenghtMin * 2)
            {
                SplitTheSpace(currentNode, listToReturn, roomLenghtMin, roomWidthMin, graph);
            }
        }


        return listToReturn;
    }

    private Line GetLineDividingSpace(Vector2Int BottomLeftAreaCorner, Vector2Int TopRightAreaCorner, int roomWidthMin, int roomLengthMin)
    {
        Orientation orientation;
        bool lenghtStatus = (TopRightAreaCorner.y - BottomLeftAreaCorner.y) > 2 * roomLengthMin;
        bool widthStatus = (TopRightAreaCorner.x - BottomLeftAreaCorner.x) >= 2 * roomWidthMin;
        if (lenghtStatus && widthStatus)
        {
            orientation = (Orientation)(Random.Range(0, 2));
        }
        else if (widthStatus)
        {
            orientation = Orientation.Vertical;
        }
        else
        {
            orientation = Orientation.Horizontal;
        }

        return new Line(orientation, GetCoordinatesForOrientation(orientation, BottomLeftAreaCorner, TopRightAreaCorner, roomWidthMin, roomLengthMin));

    }

    private Vector2Int GetCoordinatesForOrientation(Orientation orientation, Vector2Int BottomLeftAreaCorner, Vector2Int TopRightAreaCorner, int roomWidthMin, int roomLenghtMin)
    {
        Vector2Int coordinates = Vector2Int.zero;
        if (orientation == Orientation.Horizontal)
        {
            coordinates = new Vector2Int(0, Random.Range((BottomLeftAreaCorner.y + roomLenghtMin), (TopRightAreaCorner.y - roomLenghtMin)));
        }
        else
        {
            coordinates = new Vector2Int(Random.Range((BottomLeftAreaCorner.x + roomWidthMin), (TopRightAreaCorner.x - roomWidthMin)), 0);
        }
        return coordinates;
    }

    private void SplitTheSpace(RoomNode currentNode, List<RoomNode> listToReturn, int roomLenghtMin, int roomWidthMin, Queue<RoomNode> graph)
    {
        Line line = GetLineDividingSpace(currentNode.BottomLeftAreaCorner, currentNode.TopRightAreaCorner, roomWidthMin, roomLenghtMin);

        RoomNode node1, node2;

        if (line.orientation == Orientation.Horizontal)
        {
            node1 = new RoomNode(currentNode.BottomLeftAreaCorner, new Vector2Int(currentNode.TopRightAreaCorner.x, line.coordinates.y), currentNode, currentNode.TreeLayerIndex + 1);
            node2 = new RoomNode(new Vector2Int(currentNode.BottomLeftAreaCorner.x, line.coordinates.y), currentNode.TopRightAreaCorner, currentNode, currentNode.TreeLayerIndex + 1);
        }
        else
        {
            node1 = new RoomNode(currentNode.BottomLeftAreaCorner, new Vector2Int(line.coordinates.x, currentNode.TopRightAreaCorner.y), currentNode, currentNode.TreeLayerIndex + 1);
            node2 = new RoomNode(new Vector2Int(line.coordinates.x, currentNode.BottomLeftAreaCorner.y), currentNode.TopRightAreaCorner, currentNode, currentNode.TreeLayerIndex + 1);
        }

        AddNewNodeToCollections(listToReturn, graph, node1);
        AddNewNodeToCollections(listToReturn, graph, node2);

    }

    private void AddNewNodeToCollections(List<RoomNode> listToReturn, Queue<RoomNode> graph, RoomNode node)
    {
        listToReturn.Add(node);
        graph.Enqueue(node);
    }
}

public abstract class Node
{
    public Node parent;

    public List<Node> childrenNodeList;

    public bool Visited { get; set; }
    public Vector2Int BottomLeftAreaCorner { get; set; }
    public Vector2Int BottomRightAreaCorner { get; set; }
    public Vector2Int TopRightAreaCorner { get; set; }
    public Vector2Int TopLeftAreaCorner { get; set; }

    public int TreeLayerIndex { get; set; }

    public Node(Node _parent)
    {
        childrenNodeList = new List<Node>();
        this.parent = _parent;
        if (_parent != null)
        {
            _parent.AddChild(this);
        }
    }

    public void AddChild(Node node)
    {
        childrenNodeList.Add(node);
    }

    public void RemoveChild(Node node)
    {
        childrenNodeList.Remove(node);
    }

}

public class RoomNode : Node
{
    public int Width { get => (int)(TopRightAreaCorner.x - BottomLeftAreaCorner.x); }
    public int Lenght { get => (int)(TopRightAreaCorner.y - BottomLeftAreaCorner.y); }

    public RoomNode(Vector2Int bottomLeftAreaCorner, Vector2Int TopRightAreaCorner, Node parentNode, int index) : base(parentNode)
    {
        this.BottomLeftAreaCorner = bottomLeftAreaCorner;
        this.TopRightAreaCorner = TopRightAreaCorner;
        this.BottomRightAreaCorner = new Vector2Int(TopRightAreaCorner.x, BottomLeftAreaCorner.y);
        this.TopLeftAreaCorner = new Vector2Int(BottomLeftAreaCorner.x, TopRightAreaCorner.y);

        this.TreeLayerIndex = index;

    }
}

public class RoomGenerator
{
    public int maxIterations, roomLengthMin, roomWidthMin;
    public RoomGenerator(int _maxIterations, int _roomLengthMin, int _roomWidthMin)
    {
        maxIterations = _maxIterations;
        roomLengthMin = _roomLengthMin;
        roomWidthMin = _roomWidthMin;
    }

    public List<RoomNode> GenerateRoomsInGivenSpaces(List<Node> roomSpaces, float roomBottomCornerModifier, float roomTopCornerModifier, int roomOffset)
    {
        List<RoomNode> listToReturn = new List<RoomNode>();
        foreach (var space in roomSpaces)
        {
            Vector2Int newBottomLeftPoint = StructureHelper.GenerateBottomLeftCornerBetween(space.BottomLeftAreaCorner, space.TopRightAreaCorner, roomBottomCornerModifier, roomOffset);
            Vector2Int newTopRightPoint = StructureHelper.GenerateTopRightCornerBetween(space.BottomLeftAreaCorner, space.TopRightAreaCorner, roomTopCornerModifier, roomOffset);

            space.BottomLeftAreaCorner = newBottomLeftPoint;
            space.TopRightAreaCorner = newTopRightPoint;
            space.BottomRightAreaCorner = new Vector2Int(newTopRightPoint.x, newBottomLeftPoint.y);
            space.TopLeftAreaCorner = new Vector2Int(newBottomLeftPoint.x, newTopRightPoint.y);
            listToReturn.Add((RoomNode)space);
        }
        return listToReturn;
    }
}

public class DungeonGenerator
{
    RoomNode rootNode;
    List<RoomNode> allNodesCollection = new List<RoomNode>();
    public int dungeonWidth, dungeonLenght;
    public DungeonGenerator(int _dungeonWidth, int _dungeonLenght)
    {
        dungeonWidth = _dungeonWidth;
        dungeonLenght = _dungeonLenght;
    }

    public List<Node> CalculateDungeon(int maxIterations, int roomWidthMin, int roomLenghtMin, float roomBottomCornerModifier, float roomTopCornerModifier, int roomOffset, int corridorWidth)
    {
        BinarySpacePartitioner bsp = new BinarySpacePartitioner(dungeonWidth, dungeonLenght);
        allNodesCollection = bsp.PrepareNodesCollections(maxIterations, roomWidthMin, roomLenghtMin);
        List<Node> roomSpaces = StructureHelper.TraverseGraphToExtractLowerLeaves(bsp.rootNode);
        RoomGenerator roomGenerator = new RoomGenerator(maxIterations, roomLenghtMin, roomWidthMin);
        List<RoomNode> roomList = roomGenerator.GenerateRoomsInGivenSpaces(roomSpaces, roomBottomCornerModifier, roomTopCornerModifier, roomOffset);

        CorridorGenerator corridorGenerator = new CorridorGenerator();
        var corridorList = corridorGenerator.CreateCorridor(allNodesCollection, corridorWidth);

        return new List<Node>(roomList).Concat(corridorList).ToList();
    }

}

public class CorridorGenerator
{
    public CorridorGenerator() { }

    public List<Node> CreateCorridor(List<RoomNode> allNodesCollection, int corridorWidth)
    {
        List<Node> corridorList = new List<Node>();
        Queue<RoomNode> structuresToCheck = new Queue<RoomNode>(allNodesCollection.OrderByDescending(node => node.TreeLayerIndex).ToList());

        while (structuresToCheck.Count > 0)
        {
            var node = structuresToCheck.Dequeue();
            if (node.childrenNodeList.Count == 0)
            {
                continue;
            }
            CorridorNode corridor = new CorridorNode(node.childrenNodeList[0], node.childrenNodeList[1], corridorWidth);
            corridorList.Add(corridor);
        }
        return corridorList;
    }

}

public enum RelativePosition
{
    Up,
    Down,
    Right,
    Left
}


public class CorridorNode : Node
{

    private Node structure1;
    private Node structure2;
    private int corridorWidth;
    public int modifierDistanceFromWall = 100;
    public int modifierDistanceRelation = 50;

    public CorridorNode(Node node1, Node node2, int corridorWidth) : base(null)
    {
        this.structure1 = node1;
        this.structure2 = node2;
        this.corridorWidth = corridorWidth;
        GenerateCorridor();
    }

    private void GenerateCorridor()
    {
        var relativePositionOfStructure2 = CheckPositionStructure2AgainstStructure1();
        switch (relativePositionOfStructure2)
        {
            case RelativePosition.Up:
                ProcessRoomInRelationUpOrDown(this.structure1, this.structure2);
                break;
            case RelativePosition.Down:
                ProcessRoomInRelationUpOrDown(this.structure2, this.structure1);
                break;
            case RelativePosition.Right:
                ProcessRoomInRelationRightOrLeft(this.structure1, this.structure2);
                break;
            case RelativePosition.Left:
                ProcessRoomInRelationRightOrLeft(this.structure2, this.structure1);
                break;
            default:
                break;
        }
    }

    private void ProcessRoomInRelationRightOrLeft(Node structure1, Node structure2)
    {
        Node leftStructure = null;
        List<Node> leftStructureChildren = StructureHelper.TraverseGraphToExtractLowerLeaves((RoomNode)structure1);
        Node rightStructure = null;
        List<Node> rightStructureChildren = StructureHelper.TraverseGraphToExtractLowerLeaves((RoomNode)structure2);

        var sortedLeftStructure = leftStructureChildren.OrderByDescending(child => child.TopRightAreaCorner.x).ToList();
        if (sortedLeftStructure.Count == 1)
        {
            leftStructure = sortedLeftStructure[0];
        }
        else
        {
            int maxX = sortedLeftStructure[0].TopRightAreaCorner.x;
            sortedLeftStructure = sortedLeftStructure.Where(children => Math.Abs(maxX - children.TopRightAreaCorner.x) < modifierDistanceRelation).ToList();
            int index = UnityEngine.Random.Range(0, sortedLeftStructure.Count);
            leftStructure = sortedLeftStructure[index];
        }

        var possibleNeighboursInRightStructureList = rightStructureChildren.Where(
            child => GetValidYForNeighourLeftRight(
                leftStructure.TopRightAreaCorner,
                leftStructure.BottomRightAreaCorner,
                child.TopLeftAreaCorner,
                child.BottomLeftAreaCorner
                ) != -1
            ).OrderBy(child => child.BottomRightAreaCorner.x).ToList();

        if (possibleNeighboursInRightStructureList.Count <= 0)
        {
            rightStructure = structure2;
        }
        else
        {
            rightStructure = possibleNeighboursInRightStructureList[0];
        }
        int y = GetValidYForNeighourLeftRight(leftStructure.TopLeftAreaCorner, leftStructure.BottomRightAreaCorner,
            rightStructure.TopLeftAreaCorner,
            rightStructure.BottomLeftAreaCorner);
        while (y == -1 && sortedLeftStructure.Count > 1)
        {
            sortedLeftStructure = sortedLeftStructure.Where(
                child => child.TopLeftAreaCorner.y != leftStructure.TopLeftAreaCorner.y).ToList();
            leftStructure = sortedLeftStructure[0];
            y = GetValidYForNeighourLeftRight(leftStructure.TopLeftAreaCorner, leftStructure.BottomRightAreaCorner,
            rightStructure.TopLeftAreaCorner,
            rightStructure.BottomLeftAreaCorner);
        }
        BottomLeftAreaCorner = new Vector2Int(leftStructure.BottomRightAreaCorner.x, y);
        TopRightAreaCorner = new Vector2Int(rightStructure.TopLeftAreaCorner.x, y + this.corridorWidth);
    }

    private int GetValidYForNeighourLeftRight(Vector2Int leftNodeUp, Vector2Int leftNodeDown, Vector2Int rightNodeUp, Vector2Int rightNodeDown)
    {
        if (rightNodeUp.y >= leftNodeUp.y && leftNodeDown.y >= rightNodeDown.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                leftNodeDown + new Vector2Int(0, modifierDistanceFromWall),
                leftNodeUp - new Vector2Int(0, modifierDistanceFromWall + this.corridorWidth)
                ).y;
        }
        if (rightNodeUp.y <= leftNodeUp.y && leftNodeDown.y <= rightNodeDown.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                rightNodeDown + new Vector2Int(0, modifierDistanceFromWall),
                rightNodeUp - new Vector2Int(0, modifierDistanceFromWall + this.corridorWidth)
                ).y;
        }
        if (leftNodeUp.y >= rightNodeDown.y && leftNodeUp.y <= rightNodeUp.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                rightNodeDown + new Vector2Int(0, modifierDistanceFromWall),
                leftNodeUp - new Vector2Int(0, modifierDistanceFromWall)
                ).y;
        }
        if (leftNodeDown.y >= rightNodeDown.y && leftNodeDown.y <= rightNodeUp.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                leftNodeDown + new Vector2Int(0, modifierDistanceFromWall),
                rightNodeUp - new Vector2Int(0, modifierDistanceFromWall + this.corridorWidth)
                ).y;
        }
        return -1;
    }

    private void ProcessRoomInRelationUpOrDown(Node structure1, Node structure2)
    {
        Node bottomStructure = null;
        List<Node> structureBottmChildren = StructureHelper.TraverseGraphToExtractLowerLeaves((RoomNode)structure1);
        Node topStructure = null;
        List<Node> structureAboveChildren = StructureHelper.TraverseGraphToExtractLowerLeaves((RoomNode)structure2);

        var sortedBottomStructure = structureBottmChildren.OrderByDescending(child => child.TopRightAreaCorner.y).ToList();

        if (sortedBottomStructure.Count == 1)
        {
            bottomStructure = structureBottmChildren[0];
        }
        else
        {
            int maxY = sortedBottomStructure[0].TopLeftAreaCorner.y;
            sortedBottomStructure = sortedBottomStructure.Where(child => Mathf.Abs(maxY - child.TopLeftAreaCorner.y) < modifierDistanceRelation).ToList();
            int index = UnityEngine.Random.Range(0, sortedBottomStructure.Count);
            bottomStructure = sortedBottomStructure[index];
        }

        var possibleNeighboursInTopStructure = structureAboveChildren.Where(
            child => GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                child.BottomLeftAreaCorner,
                child.BottomRightAreaCorner)
            != -1).OrderBy(child => child.BottomRightAreaCorner.y).ToList();
        if (possibleNeighboursInTopStructure.Count == 0)
        {
            topStructure = structure2;
        }
        else
        {
            topStructure = possibleNeighboursInTopStructure[0];
        }
        int x = GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                topStructure.BottomLeftAreaCorner,
                topStructure.BottomRightAreaCorner);
        while (x == -1 && sortedBottomStructure.Count > 1)
        {
            sortedBottomStructure = sortedBottomStructure.Where(child => child.TopLeftAreaCorner.x != topStructure.TopLeftAreaCorner.x).ToList();
            bottomStructure = sortedBottomStructure[0];
            x = GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                topStructure.BottomLeftAreaCorner,
                topStructure.BottomRightAreaCorner);
        }
        BottomLeftAreaCorner = new Vector2Int(x, bottomStructure.TopLeftAreaCorner.y);
        TopRightAreaCorner = new Vector2Int(x + this.corridorWidth, topStructure.BottomLeftAreaCorner.y);
    }

    private int GetValidXForNeighbourUpDown(Vector2Int bottomNodeLeft,
        Vector2Int bottomNodeRight, Vector2Int topNodeLeft, Vector2Int topNodeRight)
    {
        if (topNodeLeft.x < bottomNodeLeft.x && bottomNodeRight.x < topNodeRight.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                bottomNodeRight - new Vector2Int(this.corridorWidth + modifierDistanceFromWall, 0)
                ).x;
        }
        if (topNodeLeft.x >= bottomNodeLeft.x && bottomNodeRight.x >= topNodeRight.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                topNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                topNodeRight - new Vector2Int(this.corridorWidth + modifierDistanceFromWall, 0)
                ).x;
        }
        if (bottomNodeLeft.x >= (topNodeLeft.x) && bottomNodeLeft.x <= topNodeRight.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                topNodeRight - new Vector2Int(this.corridorWidth + modifierDistanceFromWall, 0)

                ).x;
        }
        if (bottomNodeRight.x <= topNodeRight.x && bottomNodeRight.x >= topNodeLeft.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                topNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                bottomNodeRight - new Vector2Int(this.corridorWidth + modifierDistanceFromWall, 0)

                ).x;
        }
        return -1;
    }

    private RelativePosition CheckPositionStructure2AgainstStructure1()
    {
        Vector2 middlePointStructure1Temp = ((Vector2)structure1.TopRightAreaCorner + structure1.BottomLeftAreaCorner) / 2;
        Vector2 middlePointStructure2Temp = ((Vector2)structure2.TopRightAreaCorner + structure2.BottomLeftAreaCorner) / 2;
        float angle = CalculateAngle(middlePointStructure1Temp, middlePointStructure2Temp);
        if ((angle < 45 && angle >= 0) || (angle > -45 && angle < 0))
        {
            return RelativePosition.Right;
        }
        else if (angle > 45 && angle < 135)
        {
            return RelativePosition.Up;
        }
        else if (angle > -135 && angle < -45)
        {
            return RelativePosition.Down;
        }
        else
        {
            return RelativePosition.Left;
        }
    }

    private float CalculateAngle(Vector2 middlePointStructure1Temp, Vector2 middlePointStructure2Temp)
    {
        return Mathf.Atan2(middlePointStructure2Temp.y - middlePointStructure1Temp.y,
            middlePointStructure2Temp.x - middlePointStructure1Temp.x) * Mathf.Rad2Deg;
    }
}

public static class StructureHelper
{
    public static List<Node> TraverseGraphToExtractLowerLeaves(RoomNode parentNode)
    {
        Queue<Node> nodesToCheck = new Queue<Node>();
        List<Node> listToReturn = new List<Node>();

        if (parentNode.childrenNodeList.Count == 0)
        {
            return new List<Node>() { parentNode };
        }

        foreach (var child in parentNode.childrenNodeList)
        {
            nodesToCheck.Enqueue(child);
        }

        while (nodesToCheck.Count > 0)
        {
            var currentNode = nodesToCheck.Dequeue();
            if (currentNode.childrenNodeList.Count == 0)
            {
                listToReturn.Add(currentNode);
            }
            else
            {
                foreach (var child in currentNode.childrenNodeList)
                {
                    nodesToCheck.Enqueue(child);
                }

            }

        }

        return listToReturn;
    }

    public static Vector2Int GenerateBottomLeftCornerBetween(Vector2Int boundaryLeftPoint, Vector2Int boundaryRightPoint, float pointModifier, int offset)
    {
        int minX = boundaryLeftPoint.x + offset;
        int maxX = boundaryRightPoint.x - offset;
        int minY = boundaryLeftPoint.y + offset;
        int maxY = boundaryRightPoint.y - offset;

        return new Vector2Int(
            Random.Range(minX, (int)(minX + (maxX - minX) * pointModifier)),
            Random.Range(minY, (int)(minY + (maxY - minY) * pointModifier))
            );
    }


    public static Vector2Int GenerateTopRightCornerBetween(Vector2Int boundaryLeftPoint, Vector2Int boundaryRightPoint, float pointModifier, int offset)
    {
        int minX = boundaryLeftPoint.x + offset;
        int maxX = boundaryRightPoint.x - offset;
        int minY = boundaryLeftPoint.y + offset;
        int maxY = boundaryRightPoint.y - offset;

        return new Vector2Int(
            Random.Range((int)(minX + (maxX - minX) * pointModifier), maxX),
            Random.Range((int)(minY + (maxY - minY) * pointModifier), maxY)
            );
    }

    public static Vector2Int CalculateMiddlePoint(Vector2Int v1, Vector2Int v2)
    {
        Vector2 sum = v1 + v2;
        Vector2 tempVector = sum / 2;
        return new Vector2Int((int)tempVector.x, (int)tempVector.y);
    }
}

public class DungeonGeneratorTestv2 : MonoBehaviour
{

    public int DungeonWidth;
    public int DungeonLenght;

    public int roomWidthMin;
    public int roomHeightMin;

    public int maxIterations;
    public int corridorWidth;

    public Material material;
    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1.0f)]
    public float roomTopCornerModifier;
    public int roomOffset;
    List<List<Vector3>> RoomWallLoop = new List<List<Vector3>>();
    public SplineInstantiate.InstantiableItem customWall; 


    // Start is called before the first frame update
    void Start()
    {
        CreateDungeon();
    }

    private void CreateDungeon()
    {
        DungeonGenerator generator = new DungeonGenerator(DungeonWidth, DungeonLenght);

        var listOfRooms = generator.CalculateDungeon(maxIterations, roomWidthMin, roomHeightMin, roomBottomCornerModifier, roomTopCornerModifier, roomOffset, corridorWidth);

        List<GameObject> meshesToCombine = new List<GameObject>();


 
        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;



        for (int i = 0; i < listOfRooms.Count; i++)
        {
            meshesToCombine.Add(CreateMesh(listOfRooms[i].BottomLeftAreaCorner, listOfRooms[i].TopRightAreaCorner));
        }

        //draw line


        foreach (List<Vector3> loop in RoomWallLoop) 
        {
            GameObject lineRenderer = new GameObject();
            lineRenderer.AddComponent<LineRenderer>();

            SplineContainer splineContainer = gameObject.AddComponent<SplineContainer>();

            Spline spline = splineContainer.Spline;

            SplineInstantiate instantiate = new SplineInstantiate();
            instantiate.itemsToInstantiate = new SplineInstantiate.InstantiableItem[] { customWall };
            instantiate.Container = splineContainer;

            spline.splinemes


            spline.Clear();

            lineRenderer.GetComponent<LineRenderer>().positionCount = loop.Count;
            for (int l = 0;  l < loop.Count; l++) 
            {
                lineRenderer.GetComponent<LineRenderer>().SetPosition(l, loop[l]);
                spline.Add(new BezierKnot(loop[l], 0, 0));

            }

        }


        MeshFilter[] meshFilters = new MeshFilter[listOfRooms.Count];

        for (int k = 0; k < meshesToCombine.Count; k++) 
        {
            meshFilters[k] = meshesToCombine[k].GetComponent<MeshFilter>();
        }
  
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int j = 0;
        while (j < meshFilters.Length)
        {
            combine[j].mesh = meshFilters[j].sharedMesh;
            combine[j].transform = meshFilters[j].transform.localToWorldMatrix;
            meshFilters[j].gameObject.SetActive(false);

            j++;
        }

        Mesh DungeonFloorMesh = new Mesh();
        DungeonFloorMesh.CombineMeshes(combine);

        GameObject DungeonFloor = new GameObject("DungeonFloor");


        // Add a mesh filter component to the game object and set the combined mesh as its mesh
        DungeonFloor.AddComponent<MeshFilter>();
        DungeonFloor.GetComponent<MeshFilter>().mesh = DungeonFloorMesh;

        // Add a mesh renderer component to the game object to see the combined mesh
        DungeonFloor.AddComponent<MeshRenderer>();
        DungeonFloor.GetComponent<MeshRenderer>().material = material;
        DungeonFloor.AddComponent<NavMeshSurface>();
        DungeonFloor.GetComponent<NavMeshSurface>().BuildNavMesh();
        DungeonFloor.AddComponent<MeshCollider>();

    }

  

    // Update is called once per frame
    void Update()
    {

    }

    private GameObject CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner)
    {
        Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.y);
        Vector3 bottomRightV = new Vector3(topRightCorner.x, 0, bottomLeftCorner.y);
        Vector3 topLeftV = new Vector3(bottomLeftCorner.x, 0, topRightCorner.y);
        Vector3 topRightV = new Vector3(topRightCorner.x, 0, topRightCorner.y);
        Vector3[] vertices = new Vector3[]
        {
            topLeftV,
            topRightV,
            bottomLeftV,
            bottomRightV,
        };


        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        int[] triangles = new int[]
        {
            0,
            1,
            2,
            2,
            1,
            3
        };
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        GameObject dungeonFloor = new GameObject("Mesh" + bottomLeftCorner, typeof(MeshFilter), typeof(MeshRenderer));
        dungeonFloor.transform.position = Vector3.zero;
        dungeonFloor.transform.localScale = Vector3.one;
        dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;


        RoomWallLoop.Add(new List<Vector3> {
            topLeftV,topRightV,bottomRightV,bottomLeftV,topLeftV
        });



        return dungeonFloor;

    }

}