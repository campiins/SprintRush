using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Node previousNode;
    public Node nextNode;
    public int nodeIndex;
    public bool isStartingNode; // Identificar si es uno de los dos primeros nodos

    private Raceline raceline;

    private void Awake()
    {
        // Obtener la referencia al componente Raceline del padre
        raceline = transform.parent.GetComponent<Raceline>();
        if (raceline == null)
        {
            Debug.LogError("El padre de este objeto no tiene el componente Raceline.");
            return;
        }

        // Obtener la lista de hijos con el componente Node de Raceline
        Node[] childNodes = raceline.GetComponentsInChildren<Node>();

        // Encontrar el índice del nodo actual en la lista de hijos
        nodeIndex = -1;
        for (int i = 0; i < childNodes.Length; i++)
        {
            if (childNodes[i] == this)
            {
                nodeIndex = i;
                break;
            }
        }

        if (nodeIndex == -1)
        {
            Debug.LogError("Este objeto no es un hijo válido de Raceline.");
            return;
        }

        // Asignar nextNode y previousNode según las condiciones especificadas (con manejo de bordes)
        int nextIndex = (nodeIndex + 1) % childNodes.Length;
        int prevIndex = (nodeIndex - 1 + childNodes.Length) % childNodes.Length;

        nextNode = childNodes[nextIndex];
        previousNode = childNodes[prevIndex];

        // Manejo de casos en los bordes
        if (isStartingNode)
        {
            previousNode = childNodes[childNodes.Length - 1];
            nextNode = childNodes[2];
        }
        else if (nodeIndex == childNodes.Length - 1)
        {
            nextNode = childNodes[0];
        }
    }
}
