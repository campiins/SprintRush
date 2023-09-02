using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raceline : MonoBehaviour
{
    public Color lineColor;
    [Range(0,2)] public float sphereRadius;

    public List<Node> nodes;

    private void OnDrawGizmos()
    {
        Gizmos.color = lineColor;

        Node[] path = GetComponentsInChildren<Node>();

        nodes = new List<Node>();
        for (int i = 0; i < path.Length; i++)
        {
            nodes.Add(path[i]);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 currentWaypoint = nodes[i].transform.position;
            Vector3 previousWaypoint = Vector3.zero;

            if (i != 0) previousWaypoint = nodes[i - 1].transform.position;
            else if (i == 0) previousWaypoint = nodes[nodes.Count - 1].transform.position;

            Gizmos.DrawLine(previousWaypoint, currentWaypoint);
            Gizmos.DrawSphere(currentWaypoint, sphereRadius);
        }
    }

}
