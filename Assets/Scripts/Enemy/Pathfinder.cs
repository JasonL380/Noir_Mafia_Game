/*
 * Liam Kikin-Gil
 * Jason Leech
 * 
 * 12/7
 * 
 * An algorithm to take in a map/area, and create points on it in order to traverse the area as easily as possible.
 */
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

public class Pathfinder : MonoBehaviour
{
    public float speed; //the speed that this should move at, do not set this too high or it won't work

    [Tooltip("list of points for the AI follow")]
    public Vector2[] waypoints;
    private List<Vector2> pathfindingWaypoints = new List<Vector2>();

    [Tooltip("select all layers that contain objects which should be treated as walls")]
    public LayerMask wallLayers;

    //Current waypoint while chasing
    private int currentWaypoint = 0;
    private int currentPathWaypoint = 0;
    private Rigidbody2D myRB2D;
    private CircleCollider2D myCirc;
    public bool displayDebug = true;
    private Vector2 currentTarget;
    [Tooltip("The center of the pathfinding area")]
    public Vector2 boxCenter;

    [Tooltip("The size of the pathfinding area")]
    public Vector2 boxSize;

    [Tooltip(
        "The density of nodes in the pathfinding area, higher values will allow for more accurate pathfinding but will take longer to process")]
    public float nodeDensity;

    //The spacing between nodes in the graph
    private Vector2 gridSize = new Vector2();

    //the bottom left (negative, negative) corner of the pathfinding area
    private Vector2 gridStart = new Vector2();

    //stores 5 boolean values in the order right, down, left, up, isPoint
    public byte[,] graph;

    private Vector2Int graphDimensions = new Vector2Int();

    private float lastDensity;

    [Tooltip("The object that this should chase when within range, this will most likely be the player")]
    public GameObject target;

    [Tooltip("The radius that the player must remain within in order to continue being chased")]
    public float chaseRange;
    
    [Tooltip("The radius that the player must be within in order to be detected")]
    public float detectionRange;

    private bool chasing = false;

    private GameObject light;
    private Light lightComponent;
    private bool hasLight = false;

    private Quaternion targetAngle;

    private bool looking = false;
    private byte lookstep = 0;

    private float fov;
    
    private void Start()
    {
        //print("initializing pathfinder");
        myRB2D = GetComponent<Rigidbody2D>();
        myCirc = GetComponent<CircleCollider2D>();
        //target = waypoints[0];
        generateGraph();
        //print(graph[graphDimensions.x/2,graphDimensions.y/2]);
        pathfindingWaypoints = a_star_search(actualToGrid(transform.position),
           actualToGrid(waypoints[currentPathWaypoint]));

        lightComponent = GetComponentInChildren<Light>();
        if (lightComponent != null)
        {
            hasLight = true;
            light = lightComponent.gameObject;
        }

        fov = lightComponent.innerSpotAngle / 2;

        //print(pathfindingWaypoints.Count);
        //print(gridToActual(graphDimensions));
        //print(actualToGrid(transform.position));
        //print(gridToActual(actualToGrid(transform.position)));
        detectionRange = lightComponent.range * 0.9F;
        chaseRange = detectionRange;
    }

    private void Update()
    {
        light.transform.rotation = Quaternion.RotateTowards(light.transform.rotation, targetAngle, looking ? 1.125F : 5F);

        if (looking && Quaternion.Angle(targetAngle, light.transform.rotation) < 2)
        {
            if (lookstep == 0)
            {
                targetAngle = Quaternion.Inverse(targetAngle);
                lookstep = 1;
            }
            else
            {
                looking = false;
                lookstep = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            Vector2 toTarget = (target.transform.position - transform.position);
            //Quaternion.
            Quaternion angle = Quaternion.Euler(0,0,Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90);
           // print(fov + " angle: " + angle + " rot: " + light.transform.rotation.eulerAngles.z);
            if (Quaternion.Angle(angle,light.transform.rotation) < fov)
            {
                RaycastHit2D ray = Physics2D.Raycast(transform.position, target.transform.position, detectionRange, target.layer);

                if (ray.collider.gameObject == target)
                {
                    chasing = true;
                }

                if (chasing)
                {
                    if ((transform.position - target.transform.position).magnitude > chaseRange)
                    {
                        //the target is out of range, stop chasing and resume previous path
                
                        chasing = false;
                        pathfindingWaypoints = a_star_search(actualToGrid(transform.position), actualToGrid(waypoints[currentPathWaypoint]));
                    }
                    else
                    {
                        pathfindingWaypoints =
                            a_star_search(actualToGrid(transform.position), actualToGrid(target.transform.position));
                    }
                }
                Debug.DrawLine(transform.position, target.transform.position, Color.green);
            }
            else
            {
                Debug.DrawLine(transform.position, target.transform.position, Color.red);
            }
        }
        pace();
    }

    
    void generateGraph()
    {
        //calculate the grid size and dimensions from nodeDensity
        gridSize = new Vector2(1 / nodeDensity, 1 / nodeDensity); //boxSize / nodeDensity;
        gridStart = boxCenter - (boxSize / 2);
        //print(gridSize + ", " + gridStart);
        int sizeX = (int) (nodeDensity * boxSize.x);
        int sizeY = (int) (nodeDensity * boxSize.y);
        graphDimensions.x = sizeX;
        graphDimensions.y = sizeY;
        //print(sizeX + "," + sizeY);

        //initialize the graph array
        graph = new byte[sizeX, sizeY];

        //fill entire walkable space with points
        for (int x = 0; x < sizeX; ++x)
        {
            for (int y = 0; y < sizeY; ++y)
            {
                //the absolute position of this grid point
                Vector2 position = (gridSize * new Vector2(x, y)) + gridStart;

                //create a circle overlap with the same size as this object's collider to detect any nearby walls, aka determine if the object is able to exist at this position
                Collider2D collision = Physics2D.OverlapCircle(position, myCirc.radius, wallLayers);

                //if the circle didn't collide with anything add a node here
                if (collision == null)
                {
                    graph[x, y] = 1;

                    //draw the point on screen if enabled
                    if (displayDebug)
                    {
                        Debug.DrawLine(position, position - new Vector2(0, 0.1F), Color.red, 12000);
                    }
                }
                else
                {
                    graph[x, y] = 0;
                }
            }
        }

        //loop through the graph to fill in data about neighbors
        for (int x = 0; x < sizeX; ++x)
        {
            for (int y = 0; y < sizeY; ++y)
            {
                //if there isn't a point here do nothing
                if (graph[x, y] == 1)
                {
                    //right
                    if (x + 1 < sizeX && (graph[x + 1, y] & 1) == 1)
                    {
                        graph[x, y] = Convert.ToByte(1 << 4 | graph[x, y]);
                    }

                    //down
                    if (y - 1 > -1 && (graph[x, y - 1] & 1) == 1)
                    {
                        graph[x, y] = Convert.ToByte(1 << 3 | graph[x, y]);
                    }

                    //left
                    if (x - 1 > -1 && (graph[x - 1, y] & 1) == 1)
                    {
                        graph[x, y] = Convert.ToByte(1 << 2 | graph[x, y]);
                    }

                    //up
                    if (y + 1 < sizeY && (graph[x, y + 1] & 1) == 1)
                    {
                        graph[x, y] = Convert.ToByte(1 << 1 | graph[x, y]);
                    }
                }
            }
        }
    }

    

    //calculate the shortest path between start and end, return array of waypoints
    List<Vector2> a_star_search(Vector2Int start, Vector2Int goal)
    {
        PriorityQueue<Vector2Int, float> queue = new PriorityQueue<Vector2Int, float>();
        queue.Enqueue(start, 0);

        Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();

        Dictionary<Vector2Int, float> cost_so_far = new Dictionary<Vector2Int, float>();
        cameFrom[start] = null;
        cost_so_far[start] = 0;

        //print("running search " + queue.Count);
        while (queue.Count != 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goal)
            {
                break;
            }

            //print("Visiting " + current);
            int currentPoint = graph[current.x, current.y];
            for (int i = 1; i <= 4; ++i)
            {
                if ((currentPoint & (1 << i)) != 0)
                {
                    Vector2Int next = getNeighbor(current, i);
                    float new_cost = cost_so_far[current] + 1;
                    if (!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next])
                    {
                        cost_so_far[next] = new_cost;

                        queue.Enqueue(next, new_cost + heuristic(next, goal));
                        cameFrom[next] = current;
                        //print(((gridSize * next) - gridStart).ToString() + ", " + (gridSize * current) + gridStart);
                        if (displayDebug)
                        {
                            Debug.DrawLine((gridSize * next) + gridStart, (gridSize * current) + gridStart, Color.magenta, 30);
                        }
                    }
                }
            }
        }

        return reconstruct_path(cameFrom, start, goal);
    }

    float heuristic(Vector2Int pos1, Vector2Int pos2)
    {
        return (Math.Abs(pos1.x - pos2.x) + Math.Abs(pos1.y - pos2.y));
    }
    
    List<Vector2> reconstruct_path(Dictionary<Vector2Int, Vector2Int?> cameFrom, Vector2Int start, Vector2Int goal)
    {
        Vector2Int current = goal;
        List<Vector2> path = new List<Vector2>();
        if (!cameFrom.ContainsKey(goal))
        {
            print("No path found");
            return path;
        }

        while (current != start)
        {
            path.Add(gridToActual(current));
            if (cameFrom[current] != null)
            {
                Vector2 lastpath = current;
                current = cameFrom[current].Value;
                if (displayDebug)
                {
                    Debug.DrawLine(gridToActual(lastpath), gridToActual(current), Color.blue, 12000);
                }
            }
        }

        path.Reverse(0, path.Count);
//	    print(path.Count);
        currentWaypoint = 0;
        return path;
    }

    Vector2Int getNeighbor(Vector2Int current, int direction)
    {
        switch (direction)
        {
            //up
            case 1:
                return new Vector2Int(current.x, current.y + 1);
            //left
            case 2:
                return new Vector2Int(current.x - 1, current.y);
            //down
            case 3:
                return new Vector2Int(current.x, current.y - 1);
            //right
            case 4:
                return new Vector2Int(current.x + 1, current.y);
        }

        return current;
    }

    Vector2 gridToActual(Vector2 gridCoord)
    {
        return (gridSize * gridCoord) + gridStart;
    }

    Vector2Int actualToGrid(Vector2 actual)
    {
        Vector2 grid = (actual - (boxCenter - (boxSize / 2))) / gridSize;
        grid.x = Math.Min(Math.Max(grid.x, 0), graphDimensions.x - 1);
        grid.y = Math.Min(Math.Max(grid.y, 0), graphDimensions.y - 1);
        return new Vector2Int((int) grid.x, (int) grid.y);
    }

    bool hasNodes(byte[,] list)
    {
        for (int x = 0; x < graphDimensions.x; ++x)
        {
            for (int y = 0; y < graphDimensions.y; ++y)
            {
                if ((list[x, y] & 1) == 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void pace()
    {
        if (!looking)
        {
            Vector3 direction;
            if (!chasing)
            {
                direction = pathfindingWaypoints[currentWaypoint] - (Vector2) transform.position;
            }
            else
            {
                direction = target.transform.position - transform.position;
            }

            if (hasLight)
            {
                targetAngle = Quaternion.Euler(0,0,Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
            }

            Vector2 currentPos;
            currentPos.x = transform.position.x;
            currentPos.y = transform.position.y;
            //check if close to target, if so go to next one if possible
            if (direction.magnitude <= 0.5)
            {
                ++currentWaypoint;
                if (currentWaypoint >= pathfindingWaypoints.Count - 1 && !chasing)
                {
                    currentPathWaypoint++;
                    if (currentPathWaypoint >= waypoints.Length)
                    {
                        currentPathWaypoint = 0;
                    }

                    looking = true;
                    targetAngle = Quaternion.Euler(0, 0, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + 90);
                    myRB2D.velocity = Vector2.zero;
                    pathfindingWaypoints = a_star_search(actualToGrid(currentPos), actualToGrid(waypoints[currentPathWaypoint]));
                    currentWaypoint = 0;
                    currentTarget = pathfindingWaypoints[currentWaypoint];
                    return;
                }
                currentTarget = pathfindingWaypoints[currentWaypoint];
                //a_star_search(actualToGrid(currentPos), actualToGrid(waypoints[currentWaypoint]));
            }
        
            myRB2D.velocity = direction.normalized * speed;
        }
    }
}