using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    void OnDrawGizmos()
    {
        if (startPoint != null && endPoint != null)
        {
            // Set the color of the Gizmo line
            Gizmos.color = Color.red;

            // Draw the line from start to end point
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
    }

    public Vector2 GetDirection()
    {
        return (endPoint.position - startPoint.position).normalized;
    }
}

