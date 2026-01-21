using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacingUI : MonoBehaviour
{
    [Header("References")]
    public Transform head; 

    [Header("Placement")]
    public float distance = 1.8f;     
    public float heightOffset = 1f; 

    void Start()
    {
        if (!head)
        {
            Debug.LogError("PlaceUIInFrontOfHead: head (Main Camera) is not assigned.");
            return;
        }

        Vector3 forwardFlat = new Vector3(head.forward.x, 0f, head.forward.z);
        if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = Vector3.forward;
        forwardFlat.Normalize();

        transform.position = head.position + forwardFlat * distance + Vector3.up * heightOffset;

        Vector3 toUI = transform.position - head.position;
        toUI.y = 0f;
        transform.rotation = Quaternion.LookRotation(toUI);
    }
}
