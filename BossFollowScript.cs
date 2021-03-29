using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BossFollowScript : MonoBehaviour
{
    public Vector2Int gridSize;

    public GameObject circlePrefab;
    public bool portalSpawned;

    public float targetTime = 60.0f;

    public float timeToFindNextCircle;

    //public AnimationCurve speedCurve;

    float timeOfLastTryCircle;
    public float timePlayerHasToTryCircle = 5f;

    public Vector2 circleSize;
    public Transform gridStartPoint;

    CircleController[,] circleGrid;

    // public Transform testPoint;

    Vector2Int lastCircle;
    Vector2Int secondLastCircle;

    // public Transform circleParent;

    public GameObject playerBallExplode;

    Vector2 moveSpot;
    bool isMoving;

  
    public float transitionTimeBetweenCircles;
    float currentTransitionTime;
    public float minTimeToNextCircle;


    Vector2 movementStartPosition;

    Queue<Vector2Int> correctCircles = new Queue<Vector2Int>();

    private Transform player;

    private Transform orbPosition;

    Animator playerAnim;

 
    PlayerScript playerScript;

    AudioSource correctCircle;

    
    public CircleController circleControl;

    public bool theCorrectCircle = true;


    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(FindNextCircleCouroutine());
        StartCoroutine(TimeThatBallMoves());

        playerScript = GameObject.Find("Player").GetComponent<PlayerScript>();

        playerAnim = GameObject.Find("Player").GetComponent<Animator>();

        orbPosition = GameObject.Find("Orb").transform;

        correctCircle = GameObject.Find("circle").GetComponent<AudioSource>();
    }

    IEnumerator FindNextCircleCouroutine()
    {
        yield return new WaitForSeconds(5);
        while (true)
        {
            FindNextCircle();
            yield return new WaitForSeconds(timeToFindNextCircle);

            if (timeToFindNextCircle > minTimeToNextCircle)
            {
                timeToFindNextCircle *= 0.96f;
                transitionTimeBetweenCircles = timeToFindNextCircle * 0.9f;
            }
            else if (timeToFindNextCircle < minTimeToNextCircle)
            {
                timeToFindNextCircle = minTimeToNextCircle;
                transitionTimeBetweenCircles = timeToFindNextCircle * 0.9f;
            }

        }


    }

    //just for spawning all the circles, we start loop on y and x, and loop from zero to our gridsize
    private void Start()
    {
        //timeOfLastTryCircle = -5;

        gridStartPoint = GameObject.Find("SpawnCircles").transform;
        //circleParent = GameObject.Find("CircleParent").transform;


        circleGrid = new CircleController[gridSize.x, gridSize.y];

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector3 position = gridStartPoint.position + new Vector3(circleSize.x * x, circleSize.y * y);
                CircleController circle = Instantiate(circlePrefab, position, Quaternion.identity, gridStartPoint).GetComponent<CircleController>();
                circleGrid[x, y] = circle;
            }

        }

    }

    IEnumerator TimeThatBallMoves()
    {
        yield return new WaitForSeconds(60);
        CancelInvoke();
        StopAllCoroutines();
        yield return new WaitUntil(() => correctCircles.Count == 0);



        //spawn Portal
    }

    private void Update()
    {
        targetTime -= Time.deltaTime;

        if (targetTime <= 0.0f && correctCircles.Count == 0 && portalSpawned == false)
        {
            Debug.Log("portal should be spawned from followboss");
            //GameManager.portalFromFollowBossDisabled = false;
            portalSpawned = true;
        }

        if (isMoving)
        {
            currentTransitionTime += Time.deltaTime / transitionTimeBetweenCircles;
            if (currentTransitionTime >= 1)
            {
                if (TryGetCircle(transform.position, out CircleController circle))
                {
                    circle.ActivateCircle();
                }
                currentTransitionTime = 1;
                isMoving = false;
            }
            transform.position = Vector2.Lerp(movementStartPosition, moveSpot, currentTransitionTime);
        }



        Vector2 playerPosition = player.transform.position;
        Vector2Int point = GetIndexByPosition(playerPosition);

        bool inRange = IsInRange(point);
        if (inRange && correctCircles.Count > 0)
        {
            HandleInput();
           
        }

        else
        {
          
        }


        // maybe this needs to be used, edited out for calturin
       // PlayerScript.localPlayer.atFollowBoss = inRange;


    }

    private bool IsInRange(Vector2Int point)
    {
        return point.x >= 0 &&
            point.y >= 0 &&
            point.x < gridSize.x &&
            point.y < gridSize.y;
    }

    //to avoid errors, we check if point is inside the grid
    public bool TryGetCircle(Vector2 position, out CircleController circleController)
    {

        Vector2Int point = GetIndexByPosition(position);
        bool isInRange = IsInRange(point);


        if (isInRange)
        {
            circleController = circleGrid[point.x, point.y];
            return true;
        }

        circleController = null;
        return false;
    }

    //the movement of the ball
    
    void FindNextCircle()
    {
        Vector2Int currentPoint = GetIndexByPosition(transform.position);
        Vector2Int newPoint = currentPoint;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(1, -1),  // down right
            new Vector2Int(0, -1),  // down
            new Vector2Int(-1, -1), // down left
            new Vector2Int(-1, 0),  // left
            new Vector2Int(-1, 1),  // up left
            new Vector2Int(0, 1),   // up
            new Vector2Int(1, 1),   // up right
        };

        //check 10 times if ball is in range of circles, to check but prevent crash
        bool pointFound = false;
        for (int i = 0; i < 10; i++)
        {
            Vector2Int pointToCheck = currentPoint + directions[Random.Range(0, directions.Length)];
            if (pointToCheck != lastCircle && pointToCheck != secondLastCircle && IsInRange(pointToCheck))
            {
                newPoint = pointToCheck;
                pointFound = true;
                break;
            }
        }

        if (!pointFound) return;

        CircleController circleController = circleGrid[newPoint.x, newPoint.y];
        moveSpot = circleController.transform.position;

        //make it so that the second to last circle can't be chosen either
        //removing this fixes so u dont need to click twice
        secondLastCircle = lastCircle;

        //the ball start position
        lastCircle = newPoint;

        isMoving = true;
        movementStartPosition = transform.position;
        currentTransitionTime = 0;


        correctCircles.Enqueue(newPoint);
        RpcSetMoveSpot(moveSpot);
    }

  
    public void RpcSetMoveSpot(Vector2 movespot)
    {


        moveSpot = movespot;
        isMoving = true;
        movementStartPosition = transform.position;
        currentTransitionTime = 0;


        //we get indexbyposition, and if last position inside queue is not the same as our point, then add it to the queue, so we dont get same two points and need to click twice
        Vector2Int point = GetIndexByPosition(movespot);
        if (correctCircles.LastOrDefault() != point)
            correctCircles.Enqueue(point);
    }

    public bool IsCircleCorrect(Vector2 position)
    {
        // get circle index by player position
        Vector2Int point = GetIndexByPosition(position);
        if (!IsInRange(point) || correctCircles.Count == 0) return false;

        //take a look at first element in queue, if first element is same as point then that means we are at correct circle
        if (correctCircles.Peek() == point)
        {
            Debug.Log(correctCircles.Count);
            // if correct circle, we remove the circle we are currently standing on from queue
            correctCircles.Dequeue();



            theCorrectCircle = true;

            return true;


        }

        else
        {
            //if wrong circle, set player to die
            theCorrectCircle = false;
            return false;
        }
    }

    // translate world position to circle index, and then we round the position
    private Vector2Int GetIndexByPosition(Vector2 position)
    {
        Vector2 targetPos = position - (Vector2)gridStartPoint.position;

        int x = Mathf.RoundToInt(targetPos.x / circleSize.x);
        int y = Mathf.RoundToInt(targetPos.y / circleSize.y);
        return new Vector2Int(x, y);
    }

    void HandleInput()
    {
        Vector2 playerPosition = player.transform.position;

        if (Input.GetKeyDown(KeyCode.O) || Input.GetMouseButton(1))
        {
            Vector2Int point = GetIndexByPosition(playerPosition);
            if (IsInRange(point) == false)

            {
                return;
            }

            //we want particle effect to spawn at player orb
            if (IsCircleCorrect(playerPosition))
            {
                

                // get player shoot position, and sends that to other clients through server. Server takes position, calculates direction,
                // and then do 2 things, sends RpcShoot, so we send to all clients that they can shoot bullet.
                Vector3 position = orbPosition.position;
                position.x = position.x - 0.8f;

                Animator animator = playerAnim;

                //Vector2 ballDirection = new Vector2(animator.GetFloat("lastMoveX"), animator.GetFloat("lastMoveY"));

                if (animator.GetFloat("lastMoveX") > -0.1f)
                {
                    position = orbPosition.position;
                }

              

                correctCircle.Play();

                GameObject particle = Instantiate(playerBallExplode, position, Quaternion.identity);
                Destroy(particle, 0.33f);
            }

            else
            {
                // tried adding so only zaps player if he is alive, to prevent it taking more than one life.
                if (playerScript.currentHealth > 0)
                {
                    playerScript.TakeDamage(4);
                }
            }

            timeOfLastTryCircle = 0;


        }

        timeOfLastTryCircle += Time.deltaTime;
        if (timeOfLastTryCircle >= timePlayerHasToTryCircle)
        {
            timeOfLastTryCircle = 0;
            playerScript.TakeDamage(4);

        }
    }

    public void OnDestroy()
    {
        // remove all childs from transform
        for (int i = 0; i < gridStartPoint.childCount; i++)
        {
            Destroy(gridStartPoint.GetChild(i).gameObject);
        }

    }

    IEnumerator CorrectCircleTrue()
    {
        theCorrectCircle = true;

        yield return new WaitForSeconds(0.5f);

        theCorrectCircle = false;
    }

}
