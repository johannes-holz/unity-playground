using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ConstraintController : MonoBehaviour
{

    public ChainIKConstraint iKConstraint;
    public GameObject characterObject;

    public float maxWeight = 0.6f;
    private float weightOffset = 0.05f;
    public float weightChangeRate = 10f;
    public float maxAngle = 40f;
    bool isTracking;
    // Start is called before the first frame update
    void Start()
    {
        iKConstraint = GetComponent<ChainIKConstraint>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 a = characterObject.transform.forward;
        Vector3 b = iKConstraint.data.target.position - characterObject.transform.position;
        float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(a, b) / (a.magnitude * b.magnitude));

        isTracking = (angle < maxAngle) ? true : false;

        float targetWeight = isTracking ? maxWeight : 0;
        float curWeight = iKConstraint.weight;
        if (curWeight < targetWeight - weightOffset ||
            curWeight > targetWeight + weightOffset)
        {
            iKConstraint.weight = Mathf.Lerp(iKConstraint.weight, targetWeight, Time.deltaTime * weightChangeRate);
        }
        else
        {
            iKConstraint.weight = targetWeight;
        }

        iKConstraint.weight = 1;

        //headAimConstraint.weight = target;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.Log("aimControlSource: " + iKConstraint.data.target.gameObject + ", collided object: " + other.gameObject);
    //    if (iKConstraint.data.target.gameObject == other.gameObject)
    //    {
    //        Debug.Log(other.gameObject + ", IS AIMING");
    //        isTracking = true;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    //if (other.gameObject.tag == "Player")
    //    if (iKConstraint.data.target.gameObject == other.gameObject)
    //    {
    //        isTracking = false;
    //    }
    //}
}