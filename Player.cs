using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using System.Linq;

public class Player : MonoBehaviour
{
  Rigidbody2D rb;

  private float accelInput = 0; // current acceleration input
  private float steerInput = 0; // current steering input
  private float carRotation = 90; // current rotation angle of the car
  private float velocityAlignment = 0; // how aligned the car's movement is with its facing direction

  // physics variables to control the car
  public float driftReduction;
  public float accelerationPower;
  public float steeringPower;
  public float maxSpeed;

  // ai stuff
  private int laps = 0;
  private float timeStandingStill = 0;
  private int inputs = 14;
  // private int outputs = 2;
  public Genome brain = new Genome(14, 2);
  public float fitness = 0;
  public bool isAlive = true;
  public List<float> vision;
  public List<float> decision;
  public GameObject playerPrefab;
  private float timeTaken = 0;
  private int checkpointsReached = 0;
  private bool collidedWithWall = false;
  private float speedAtDeath = 0;
  public GameManager gameManager;
  private float cornerSpeedBonus = 0;
  private int cornersPassed = 0;

  // for calculating distance
  private Vector2 lastPosition;
  private float distanceTravelled = 0;
  public Vector2 trackForwardDirection = Vector2.left;
  private float timeSinceLastProgress = 0;
  private float timeSinceLastCheckpoint = 0;

  // visualize rays
  private LineRenderer lineRenderer;
  public int numOfRays = 13; // used as inputs
  public float rayDistance = 12f;
  public LayerMask raycastMask;
  private bool visualizingRays = false;

  void Start() {
    rb = GetComponent<Rigidbody2D>();

    lineRenderer = gameObject.GetComponent<LineRenderer>();
    lineRenderer.positionCount = numOfRays * 2; // Each ray needs two points

    Reset();

    gameManager = FindObjectOfType<GameManager>();
  }

  public void Reset() {
    fitness = 0;
    timeStandingStill = 0;
    isAlive = true;
    gameObject.SetActive(true);
    distanceTravelled = 0;
    laps = 0;
    transform.position = new Vector2(1.29f, -4.3f);
    transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    lastPosition = transform.position;
    timeTaken = 0;
    trackForwardDirection = Vector2.left;
    timeSinceLastProgress = 0;
    checkpointsReached = 0;
    collidedWithWall = false;
    speedAtDeath = 0;
    cornerSpeedBonus = 0;
    cornersPassed = 0;
  }

  // handle inputs
  // void Update() {
  // 	UpdateInputs();
  // }

  // control/handle physics stuff
  public void UpdatePlayer() {
    if (rb == null) return;

    ControlEngine();
    ControlDrift();
    ControlSteering();
    CalculateDistanceTravelled();

    if (visualizingRays) {
      CastRays(false);
    }

    timeTaken += Time.deltaTime;
    timeSinceLastCheckpoint += Time.deltaTime;

    if (timeSinceLastCheckpoint > 10f) {
      isAlive = false;
      gameObject.SetActive(false);
      gameManager.aliveCounter--;
      gameManager.UpdateAlive();
    }
  }

  // void FixedUpdate() {
  // 	ControlEngine();
  // 	ControlDrift();
  // 	ControlSteering();
  //   if (visualizingRays) {
  //     CastRays(false);
  //   }
  // }

  private void OnCollisionEnter2D(Collision2D collision) {
    if (collision.gameObject.layer == LayerMask.NameToLayer("Road")) {
      isAlive = false;
      speedAtDeath = rb.velocity.magnitude;
      gameObject.SetActive(false);
      collidedWithWall = true;
      gameManager.aliveCounter--;
      gameManager.UpdateAlive();
    }
  }

  void OnTriggerEnter2D(Collider2D other)
  {
    TrackSegment segment = other.GetComponent<TrackSegment>();
    if (segment != null)
    {
      int segmentName = int.Parse(segment.gameObject.name);
      Vector2 newForwardDirection = segment.GetDirection();
      // Update your player's notion of forward direction
      trackForwardDirection = newForwardDirection;
      timeSinceLastCheckpoint = 0;
      if (checkpointsReached < segmentName) {
        checkpointsReached++;
      }

      if (segmentName == 10 || segmentName == 16 || segmentName == 19 || segmentName == 21 || segmentName == 25 || segmentName == 29) {
        cornerSpeedBonus += rb.velocity.magnitude;
        cornersPassed++;
      }
    } else {
      // this is a lap trigger

      if (distanceTravelled > 50f) {
        laps++;

        if (laps == 3) {
          isAlive = false;
          gameObject.SetActive(false);
          gameManager.aliveCounter--;
          gameManager.UpdateAlive();
        }
      }
    }
  }

  // void UpdateInputs() {
  //   steerInput = Input.GetAxis("Horizontal");
  //   accelInput = Input.GetAxis("Vertical");

  //   if (Input.GetKeyDown(KeyCode.V)) {
  //     visualizingRays = !visualizingRays;
  //   }
  // }

  List<float> CastRays(bool isForInput) {
    List<float> rayInputs = new List<float>();
    float[] angles = {-90f, -60f, -45f, -30f, -15f, -7.5f, 0f, 7.5f, 15f, 30f, 45f, 60f, 90f};

    for (int i = 0; i < numOfRays; i++) {
      // Calculate ray direction
      Vector2 rayDirection = Quaternion.Euler(0, 0, angles[i]) * transform.up;
      RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, rayDistance, raycastMask);

      if (isForInput) {
        float distance = hit.collider != null ? hit.distance : rayDistance;
        rayInputs.Add(distance);
      } else {
        int lineIndex = i * 2;
        lineRenderer.SetPosition(lineIndex, transform.position);

        if (hit.collider != null) {
          // There's an obstacle in the way
          lineRenderer.SetPosition(lineIndex + 1, hit.point);
        } else {
          // No obstacle detected
          lineRenderer.SetPosition(lineIndex + 1, transform.position + (Vector3)rayDirection * rayDistance);
        }
      }
    }

    return rayInputs;
  }

  void ControlEngine() {
    velocityAlignment = Vector2.Dot(transform.up, rb.velocity.normalized);

    if ((Mathf.Abs(velocityAlignment) > maxSpeed && accelInput > 0) ||
      (velocityAlignment < -maxSpeed && accelInput < 0)) {
      return;
    }

    if (accelInput == 0) {
      rb.drag = Mathf.Lerp(rb.drag, 3.0f, Time.fixedDeltaTime * 3);
    } else {
      rb.drag = 0;
    }

    Vector2 propulsionForce = transform.up * accelInput * accelerationPower;
    rb.AddForce(propulsionForce, ForceMode2D.Force);
  }

  void ControlDrift() {
    Vector2 directionalVelocity = transform.up * Vector2.Dot(rb.velocity, transform.up);
    Vector2 lateralVelocity = transform.right * Vector2.Dot(rb.velocity, transform.right);

    rb.velocity = directionalVelocity + lateralVelocity * driftReduction;
  }

  void ControlSteering() {
    float speedThreshold = Mathf.Clamp01(rb.velocity.magnitude / 2);
    float reverseFactor = (velocityAlignment < 0) ? -1 : 1;

    carRotation -= steerInput * steeringPower * speedThreshold * reverseFactor;

    rb.MoveRotation(carRotation);
  }

  // ai stuff

  void CalculateDistanceTravelled() {
    Vector2 currentPosition = transform.position;
    Vector2 movementVector = currentPosition - lastPosition;

    // Check if the movement is forward relative to the track's forward direction
    if (Vector2.Dot(movementVector, trackForwardDirection) > 0) {
      float distance = movementVector.magnitude;

      if (distance > 0) {
        distanceTravelled += distance;
        timeSinceLastProgress = 0;  // Reset since there's forward progress
      }
    } else {
      // No forward progress, increment the time since last progress
      timeSinceLastProgress += Time.deltaTime;
    }

    // Check if the player has been inactive for too long
    if (timeSinceLastProgress >= 0.1f) {
      isAlive = false;
      gameObject.SetActive(false); 
      gameManager.aliveCounter--;
      gameManager.UpdateAlive();
    }

    lastPosition = currentPosition;  // Ensure lastPosition is updated here
  }

  public void Look() {
    if (rb == null) return;

    vision = new List<float>(inputs);

    List<float> rayInputs = CastRays(true);
    vision.AddRange(rayInputs);
    vision.Add(rb.velocity.magnitude);
  }

  public void Think() {
    if (rb == null) return;

    decision = brain.FeedForward(vision);

    accelInput = decision[0];  // Now directly maps to forward and reverse
    steerInput = decision[1];  // Now directly maps to right and left
  }

  public void CalculateFitness() {
    float avgSpeed = distanceTravelled / timeTaken;
    float avgCornerSpeedBonus = 0;
    if (cornersPassed > 0) {
      avgCornerSpeedBonus = cornerSpeedBonus / cornersPassed;
    }
    Debug.Log(avgCornerSpeedBonus);

    if (timeTaken > 0 && distanceTravelled > 0) {
      fitness = (distanceTravelled * distanceTravelled) + (avgSpeed * 1.5f);

      if (laps > 0) {
        fitness += Mathf.Pow(1.1f, laps + 1);
      } else {
        // if (collidedWithWall) {
        //   if (speedAtDeath >= maxSpeed - 2f) {
        //     fitness *= 0.75f;
        //   }
        // }
      }
    } else {
      fitness = 0;
    }

    // fitness = fitness * fitness;

    // Debug.Log("Distance: " + distanceTravelled.ToString() + " Time Taken: " + timeTaken + " Avg Speed: " + avgSpeed.ToString() + " Fitness: " + fitness + " " + gameObject.name);
  }

  public Player Clone(int name, GameObject parentContainer) {
    GameObject cloneObject = Instantiate(playerPrefab, parentContainer.transform);
    cloneObject.SetActive(false);
    cloneObject.name = name.ToString();
    Player clone = cloneObject.GetComponent<Player>();
    clone.rb = cloneObject.GetComponent<Rigidbody2D>();
    clone.brain = brain.Clone();
    clone.brain.GenerateNetwork();
    return clone;
  }

  public Player Crossover(Player parent2, int name, GameObject parentContainer) {
    GameObject childObject = Instantiate(playerPrefab, parentContainer.transform);
    childObject.SetActive(false);
    childObject.name = name.ToString();
    Player child = childObject.GetComponent<Player>();
    child.rb = childObject.GetComponent<Rigidbody2D>();
    child.brain = brain.Crossover(parent2.brain);
    child.brain.GenerateNetwork();
    return child;
  }
}
