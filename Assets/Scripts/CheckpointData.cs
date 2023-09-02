using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointData : MonoBehaviour
{
    public int checkpointIndex;

    // Start is called before the first frame update
    void Awake()
    {
        checkpointIndex = transform.GetSiblingIndex();
    }
}
