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
using System.IO;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Path;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utils;

//[ExecuteInEditMode]
public class Pathfinder : MonoBehaviour
{

    //4 states that this pathfinder can be in
    public enum PathfinderState
    {
        Pacing, //moving along the path determined by waypoints
        Looking, //looking around
        Chasing, //chasing the target
        Searching, //searching for the target
        Paused //Stopped because target entered field of view
    }

    public float speed; //the speed that this should move at, do not set this too high or it won't work

    public Animator myAnim;
    
    [Tooltip("list of points for the AI follow")]
    public Vector2[] waypoints;

    private List<Vector2> pathfindingWaypoints = new List<Vector2>();

    [Tooltip("select all layers that contain objects which should be treated as walls")]
    public LayerMask wallLayers;

    //Current waypoint while chasing
    private int currentWaypoint = 0;
    private int currentPathWaypoint = 0;
    private Rigidbody2D myRB2D;
    public bool displayDebug = true;
    public Vector2 currentTarget;
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

    private GameObject light;
    private GameObject minimapArrow;
    private Light2D lightComponent;
    private bool hasLight = false;

    public Quaternion targetAngle;

    public PathfinderState State;

    public byte lookstep = 0;

    private PathfinderState lastState;
    
    private float fov;
    private float outerfov;

    public Vector2 ColliderSize;
    
    private Vector2 pausedPosition; //the position that the player stopped at
    public float pauseMovementThreshold; //the distance that the player needs to move in order to be detected while paused

    private PlayerMovement player;

    private void Start()
    {
        //UnityEditor.Editor.CreateEditor()_editor.controller = new EditablePathController();
        if (Application.isPlaying)
        {
            //PathUtility
            myAnim = GetComponent<Animator>();

            player = target.GetComponent<PlayerMovement>();
            //print("initializing pathfinder");
            myRB2D = GetComponent<Rigidbody2D>();
            //target = waypoints[0];
            generateGraph();
            //print(graph[graphDimensions.x/2,graphDimensions.y/2]);
            pathfindingWaypoints = a_star_search(actualToGrid(transform.position),
                actualToGrid(waypoints[currentPathWaypoint]));

            lightComponent = GetComponentInChildren<Light2D>();
            if (lightComponent != null)
            {
                hasLight = true;
                light = lightComponent.gameObject;
            }

            minimapArrow = GetComponentInChildren<Arrow>().gameObject;

            fov = lightComponent.pointLightInnerAngle / 2;
            outerfov = lightComponent.pointLightOuterAngle / 2;
            //print(pathfindingWaypoints.Count);
            //print(gridToActual(graphDimensions));
            //print(actualToGrid(transform.position));
            //print(gridToActual(actualToGrid(transform.position)));
            detectionRange = lightComponent.pointLightOuterRadius * 0.9F;
            chaseRange = detectionRange;
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if(State != PathfinderState.Paused)
            {
                light.transform.rotation = Quaternion.RotateTowards(light.transform.rotation, targetAngle,
                    State == PathfinderState.Looking ? 1.125F : 5F);
                minimapArrow.transform.rotation = light.transform.rotation;
            }

            //if(displayDebug) Debug.DrawLine(transform.position, );

            if (State == PathfinderState.Looking && Quaternion.Angle(targetAngle, light.transform.rotation) < 2)
            {
                if (lookstep == 0)
                {
                    targetAngle = Quaternion.Euler(0, 0, targetAngle.eulerAngles.z + 180);
                    lookstep = 1;
                }
                else
                {
//                print("done looking " + lookstep);
                    myAnim.SetBool("walking", true);
                    State = PathfinderState.Pacing;
                    lookstep = 0;
                }
            }
        }
            
    }

    private void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            if (State != PathfinderState.Paused)
            {
                SearchForTarget();
                if (State != PathfinderState.Paused) pace();
            }
            else
            {
                Vector2 toTarget = (target.transform.position - transform.position);
                Quaternion angle = Quaternion.Euler(0,0,Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90);
                // print(fov + " angle: " + angle + " rot: " + light.transform.rotation.eulerAngles.z);
                if (Quaternion.Angle(angle, light.transform.rotation) < fov * (State == PathfinderState.Chasing ? 2 : 1) &&
                    toTarget.sqrMagnitude < detectionRange * detectionRange)
                {
                    if (player.power > 0)
                    {
                        RaycastHit2D ray = Physics2D.Linecast(transform.position, target.transform.position, wallLayers);

                        if (ray.collider == null)
                        {
                            player.visible = true;
                        }
                        else
                        {
                            player.visible = false;
                            State = lastState;
                        }
                    }
                    else
                    {
                        print("Start chasing");
                        State = PathfinderState.Chasing;
                        myAnim.SetBool("walking", true);
                        targetAngle = Quaternion.Euler(0, 0, (Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg) - 90);
                        //State = lastState;
                    }
                }
                else
                {
                    player.visible = false;
                    State = lastState;
                }
            }
        }
        
    }


    private void SearchForTarget()
    {
        if (target != null)
        {
            Vector2 toTarget = (target.transform.position - transform.position);
            //Quaternion.
            Quaternion angle = Quaternion.Euler(0,0,Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90);
           // print(fov + " angle: " + angle + " rot: " + light.transform.rotation.eulerAngles.z);
            if (Quaternion.Angle(angle,light.transform.rotation) < fov * (State == PathfinderState.Chasing ? 2 : 1) && toTarget.sqrMagnitude < detectionRange*detectionRange)
            {
                RaycastHit2D ray = Physics2D.Linecast(transform.position, target.transform.position, wallLayers);

                if (ray.collider == null)
                {
                    if (State != PathfinderState.Chasing)
                    {
                        print("saw target, waiting");
                        State = PathfinderState.Paused;
                        pausedPosition = target.transform.position;
                        myRB2D.velocity = Vector2.zero;
                        player.visible = true;
                        //if(displayDebug) Debug.DrawLine(transform.position, target.transform.position, Color.cyan, pauseTime);
                    }
                    else
                    {
                        if(displayDebug) Debug.DrawLine(transform.position, target.transform.position, Color.green);
                    }
                    
                    //print("start chasing, stop looking");

                }
                else
                {
                    player.visible = false;
                    if (State == PathfinderState.Chasing)
                    {
                        State = PathfinderState.Searching;
                        pathfindingWaypoints =
                            a_star_search(actualToGrid(transform.position), actualToGrid(target.transform.position));
                        //print("lost line of sight with the target, going to last known position to look for it");
                    }
                    else if (State != PathfinderState.Searching)
                    {
                        State = PathfinderState.Pacing;
                        pathfindingWaypoints = a_star_search(actualToGrid(transform.position), actualToGrid(waypoints[currentPathWaypoint]));
                        if(displayDebug) Debug.DrawLine(transform.position, target.transform.position, Color.yellow);
                    }
                }

                if (State == PathfinderState.Chasing)
                {
                    pathfindingWaypoints =
                        a_star_search(actualToGrid(transform.position), actualToGrid(target.transform.position));
                }
                
            }
            /*else if (Quaternion.Angle(angle,light.transform.rotation) < outerfov * (State == PathfinderState.Chasing ? 2 : 1) && toTarget.sqrMagnitude < detectionRange*detectionRange)
            {
                RaycastHit2D ray = Physics2D.Linecast(transform.position, target.transform.position, wallLayers);

                if (ray.collider == null)
                {
                    State = PathfinderState.Paused;
                    pausedPosition = target.transform.position;
                    pauseTimer = pauseTime;
                    //print("start chasing, stop looking");

                }
            }*/
            else
            {
                player.visible = false;
                if (State == PathfinderState.Chasing)
                {
                    State = PathfinderState.Pacing;
                    pathfindingWaypoints = a_star_search(actualToGrid(transform.position), actualToGrid(waypoints[currentPathWaypoint]));
                }
                if(displayDebug) Debug.DrawLine(transform.position, target.transform.position, Color.red);
            }
        }
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
                //Collider2D collision = Physics2D.OverlapCircle(position, myCirc.radius, wallLayers);
                Collider2D collision = Physics2D.OverlapBox(position, ColliderSize, 0, wallLayers);
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
       /* for (int x = 0; x < sizeX; ++x)
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
            }*/
        //}
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
        float sqrt2 = Mathf.Sqrt(2);
        //print("running search " + queue.Count);
        while (queue.Count != 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goal)
            {
                break;
            }

            //print("Visiting " + current);
            int currentPoint;
            try
            {
                currentPoint = graph[current.x, current.y];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            
            
            for (int i = 1; i <= (State == PathfinderState.Chasing ? 8 : 4); ++i)
            {
                Vector2Int next = getNeighbor(current, i);
                if (next.x >= 0 && next.x <= graphDimensions.x - 1 && next.y >= 0 &&
                    next.y <= graphDimensions.y - 1 && graph[next.x, next.y] != 0)
                {
                    float new_cost;
                    if (i > 4)
                    {
                        new_cost = cost_so_far[current] + sqrt2;
                    }
                    else
                    {
                        new_cost = cost_so_far[current] + 1;
                    }
                    
                    if (!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next])
                    {
                        cost_so_far[next] = new_cost;

                        queue.Enqueue(next, new_cost + heuristic(next, goal));
                        cameFrom[next] = current;
                        //print(((gridSize * next) - gridStart).ToString() + ", " + (gridSize * current) + gridStart);
                        if (displayDebug)
                        {
                            //Debug.DrawLine((gridSize * next) + gridStart, (gridSize * current) + gridStart,
                                //Color.magenta, 30);
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
        /*List<Vector2> simplifiedPath = new List<Vector2>();
        simplifiedPath.Add(path[0]);

        Vector2 currentStart = path[0];
        //try to simplify the path
        for (int i = 1; i < path.Count; ++i)
        {
            RaycastHit2D hit = Physics2D.CircleCast(currentStart, ColliderSize.x/2, (path[i] - currentStart).normalized,
                (path[i] - currentStart).magnitude, wallLayers);

            if (hit.collider != null)
            {
                Debug.DrawLine(simplifiedPath[simplifiedPath.Count - 1], path[i-1], Color.magenta, 30);
                simplifiedPath.Add(path[i-1]);
                currentStart = path[i - 1];
            }
        }
        Debug.DrawLine(simplifiedPath[simplifiedPath.Count - 1], path[path.Count-1], Color.magenta, 30);
        simplifiedPath.Add(path[path.Count - 1]);
        print(path.Count + " " + simplifiedPath.Count);*/
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
            //up left
            case 5:
                return new Vector2Int(current.x - 1, current.y + 1);
            //up right
            case 6:
                return new Vector2Int(current.x + 1, current.y + 1);
            //down left
            case 7:
                return new Vector2Int(current.x - 1, current.y - 1);
            //down right
            case 8:
                return new Vector2Int(current.x + 1, current.y - 1);
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
        
        
        
        Vector2Int bestPoint = new Vector2Int(Mathf.RoundToInt(grid.x), Mathf.RoundToInt(grid.y));
        /*Vector2Int bestPoint = gridint;
        if ((graph[gridint.x, gridint.y] & 1) != 1)
        {
            int bestDiff = 1024;
            for (int x = (int) nodeDensity * -2; x < nodeDensity * 2; ++x)
            {
                for (int y = (int) nodeDensity * -2; y < nodeDensity * 2; ++y)
                {
                    int cx = Math.Min(Math.Max(gridint.x + x, 0), graphDimensions.x - 1);
                    int cy = Math.Min(Math.Max(gridint.y + y, 0), graphDimensions.y - 1);
                    if (graph[cx, cy] != 0 && Mathf.Abs(x) + Mathf.Abs(y) < bestDiff)
                    {
                        bestDiff = Mathf.Abs(x) + Mathf.Abs(y);
                        bestPoint = new Vector2Int(cx, cy);
                    }
                }
            }
        }*/

        return bestPoint;
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
       /* if (waypoints.Length == 0)
        {
            if(chasing) {
                pathfindingWaypoints = a_star_search(actualToGrid(transform.position), actualToGrid(target.transform.position));
            }
            else
            {
                pathfindingWaypoints = a_star_search(actualToGrid(transform.position), actualToGrid(waypoints[currentPathWaypoint]));
            }
        }*/
        
        if (State != PathfinderState.Looking)
        {
            Vector3 direction;
            if (State != PathfinderState.Chasing)
            {
                direction = pathfindingWaypoints[currentWaypoint] - (Vector2) transform.position;
                if (hasLight)
                {
                    targetAngle = Quaternion.Euler(0,0,(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90);
                }
            }
            else
            {
                direction = pathfindingWaypoints[currentWaypoint] - (Vector2) transform.position;
                if (hasLight)
                {
                    Vector2 toTarget = target.transform.position - transform.position;
                    targetAngle = Quaternion.Euler(0,0,(Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg) - 90);
                }
            }

            

            Vector2 currentPos;
            currentPos.x = transform.position.x;
            currentPos.y = transform.position.y;
            //check if close to target, if so go to next one if possible
            if (direction.magnitude <= 0.5)
            {
                ++currentWaypoint;
                if (currentWaypoint >= pathfindingWaypoints.Count - 1 && State != PathfinderState.Chasing)
                {
                    State = PathfinderState.Looking;
                    myAnim.SetBool("walking", false);
                    myRB2D.velocity = Vector2.zero;
                    currentWaypoint = 0;
                    if (State != PathfinderState.Searching)
                    {
                        currentPathWaypoint++;
                    }
                    
                    if (currentPathWaypoint >= waypoints.Length)
                    {
                        currentPathWaypoint = 0;
                    }
                    
                    targetAngle = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180);
                    pathfindingWaypoints = a_star_search(actualToGrid(currentPos), actualToGrid(waypoints[currentPathWaypoint]));
                    currentTarget = pathfindingWaypoints[currentWaypoint];
                    return;
                }
                currentTarget = pathfindingWaypoints[currentWaypoint];
                //a_star_search(actualToGrid(currentPos), actualToGrid(waypoints[currentWaypoint]));
            }
            
            myAnim.SetFloat("X", direction.normalized.x);
            myAnim.SetFloat("Y", direction.normalized.y);
            
            
            myRB2D.velocity = direction.normalized * speed;
        }
    }
}