using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TagetTrackingScript : MonoBehaviour
{

    public MultiAimConstraint headAimConstraint;

    public float maxWeight = 0.6f;
    private float weightOffset = 0.05f;
    public float weightChangeRate = 10f;
    bool isTracking;
    // Start is called before the first frame update
    void Start()
    {
        headAimConstraint = GetComponent<MultiAimConstraint>();
    }

    // Update is called once per frame
    void Update()
    {
        float targetWeight = isTracking ? maxWeight : 0;
        float curWeight = headAimConstraint.weight;
        if (curWeight < targetWeight - weightOffset ||
            curWeight > targetWeight + weightOffset)
        {
            headAimConstraint.weight = Mathf.Lerp(headAimConstraint.weight, targetWeight, Time.deltaTime * weightChangeRate);
        } else
        {
            headAimConstraint.weight = targetWeight;
        }
        
        //headAimConstraint.weight = target;
    }

    // Use collider with layer on target object or calculate pitch+yaw+dist every update?
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("aimControlSource: " + headAimConstraint.data.sourceObjects[0].transform.gameObject + ", collided object: " + other.gameObject);
        if (headAimConstraint.data.sourceObjects[0].transform.gameObject == other.gameObject) 
        {
            //Debug.Log(other.gameObject + ", IS TRCACKING");
            isTracking = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //if (other.gameObject.tag == "Player")
        if (headAimConstraint.data.sourceObjects[0].transform.gameObject == other.gameObject)
        {
            isTracking = false;
        }
    }
}
