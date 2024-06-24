using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DungeonGeneratorTest : MonoBehaviour
{

    float lineSize = 1.0f;
    float WIDTH = 100;
    float HEIGHT = 100;


    public float minwidth = 25;
    public float minheight = 25;

    int forceSplits = 5; 

    List<Room> roomList = new List<Room>();
    Queue<Room> roomQueue = new Queue<Room>();

    public GameObject origin; 


    public class Room 
    {
        public float width, height;
        public Vector3 origin; 

        public Room(float _width, float _height, Vector3 _origin) 
        {
            width = _width; height = _height; origin = _origin;
        }

        public bool CanSplitWIDTH(float minwidth) 
        {
            return width > minwidth;
        }

        public bool CanSplitHEIGHT(float minheight)
        {
            return height > minheight;
        }
    }
  
    private void init(ref LineRenderer lineRenderer, float lineSize = 0.2f)
    {
        if (lineRenderer == null)
        {
            GameObject lineObj = new GameObject("LineObj");
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            //Particles/Additive
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

            this.lineSize = lineSize;
        }
    }
    public void DrawLineInGameView(Vector3 start, Vector3 end, Color color)
    {

        LineRenderer lineRenderer = new LineRenderer();    
        init(ref lineRenderer, 0.2f);
        
        //Set color
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        //Set width
        lineRenderer.startWidth = 2.0f;
        lineRenderer.endWidth = 2.0f;

        //Set line count which is 2
        lineRenderer.positionCount = 2;

        //Set the postion of both two lines
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
    // Start is called before the first frame update
    void Start()
    {
       // DrawLineInGameView(new Vector3(-50,0,0), new Vector3(50, 0, 0), Color.red);

        Room room = new Room (WIDTH, HEIGHT, origin.transform.position);
  
        roomQueue.Enqueue(room);

        while (roomQueue.Count > 0) 
        {
            Room current = roomQueue.Dequeue();

            float willBeSplit = Random.Range(0, 101);

            if (willBeSplit <= 50 || forceSplits >= 1)
            {
                float splitDirection = Random.Range(0, 101);

                if (splitDirection <= 50)
                {
                    AttemptVerticalSplit(current);
                }
                else
                {
                    AttemptHorizontalSplit(current);
                }
            }
            else 
            {
                roomList.Add(current);
            }

        }
        int i = 0; 
    }

    private void AttemptHorizontalSplit(Room room)
    {
        if (room.CanSplitWIDTH(minwidth))
        {
            Room firsthalf = new Room(room.width, room.height / 2, room.origin);
            roomQueue.Enqueue(firsthalf);
            Room secondhalf = new Room(room.width, room.height / 2, new Vector3(room.origin.x, 0, room.origin.z - (room.height / 2)));
            roomQueue.Enqueue(secondhalf);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = secondhalf.origin;


            DrawLineInGameView(secondhalf.origin, new Vector3(secondhalf.origin.x + 1000, 0 , secondhalf.origin.z), Color.red);

            forceSplits--; 
        }
        else 
        {
            roomList.Add(room);
        }
    }

    private void AttemptVerticalSplit(Room room)
    {
        if (room.CanSplitHEIGHT(minheight))
        {
            Room firsthalf = new Room(room.width / 2, room.height, room.origin);
            roomQueue.Enqueue(firsthalf);
            Room secondhalf = new Room(room.width / 2, room.height, new Vector3(room.origin.x + (room.width / 2), 0, room.origin.z));
            roomQueue.Enqueue(secondhalf);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = secondhalf.origin;

           DrawLineInGameView(secondhalf.origin, new Vector3(secondhalf.origin.x, 0, secondhalf.origin.z - 1000), Color.blue);


            forceSplits--; 
        }
        else
        {
            roomList.Add(room);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
