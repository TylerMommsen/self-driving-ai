using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

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
    public int numOfRays = 8;
    public float rayDistance = 10f;
    public LayerMask raycastMask;

    // visualize rays
    private LineRenderer lineRenderer;
    public GameObject[] trackObjects;
    public GameObject finishLineObjects;
    public GameObject[] squares;

    void Start() {
        rb = GetComponent<Rigidbody2D>();

        lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.positionCount = numOfRays * 2; // Each ray needs two points
    }

    // handle inputs
    void Update() {
        UpdateInputs();
    }

    // control/handle physics stuff
    void FixedUpdate() {
        ControlEngine();
        ControlDrift();
        ControlSteering();
        CastRays();
    }

    void CastRays() {
        float angleStep = 360f / numOfRays;
        float currentAngle = 0f;

        for (int i = 0; i < numOfRays; i++) {
            // Calculate ray direction
            Vector2 rayDirection = Quaternion.Euler(0, 0, currentAngle) * transform.up;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, rayDistance, raycastMask);

            int lineIndex = i * 2;
            lineRenderer.SetPosition(lineIndex, transform.position);

            if (hit.collider != null) {
                // There's an obstacle in the way
                lineRenderer.SetPosition(lineIndex + 1, hit.point);
            } else {
                // No obstacle detected
                lineRenderer.SetPosition(lineIndex + 1, transform.position + (Vector3)rayDirection * rayDistance);
            }

            currentAngle += angleStep;
        }
    }

    void UpdateInputs() {
        steerInput = Input.GetAxis("Horizontal");
        accelInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.V)) {
            foreach(GameObject trackObject in trackObjects) {
                trackObject.GetComponent<SpriteShapeRenderer>().enabled = false;
            }

            foreach(GameObject square in squares) {
                square.GetComponent<SpriteRenderer>().enabled = false;
            }

            foreach (Transform finishLineTransform in finishLineObjects.transform) {
                finishLineTransform.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
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
        float speedThreshold = Mathf.Clamp01(rb.velocity.magnitude / 4);
        float reverseFactor = (velocityAlignment < 0) ? -1 : 1;

        carRotation -= steerInput * steeringPower * speedThreshold * reverseFactor;

        rb.MoveRotation(carRotation);
    }
}
