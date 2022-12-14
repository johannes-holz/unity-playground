using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class MovingPlatformScriptedMovement : MonoBehaviour
{
    // relative to starting position
    private class Waypoint
    {
        public Vector3 position;
        public float yRotation;

        public Waypoint(float x, float y, float z, float rot)
        {
            position = new Vector3(x, y, z);
            yRotation = rot;
        }

        public Waypoint(Vector3 pos, float rot)
        {
            position = pos;
            yRotation = rot;
        }
    }

    List<Waypoint> waypoints = new List<Waypoint>();
    int index = 0;
    public float speed = 1f;

    public Vector3 oldPos;
    public Vector3 velocity;

    public ThirdPersonController player;
    bool playerEntered;

    private void AddWaypoint(float x, float y, float z, float rot) {
        if (waypoints.Count == 0)
        {
            waypoints.Add(new Waypoint(x, y, z, rot));
        } else
        {
            Waypoint last = waypoints[waypoints.Count - 1];
            waypoints.Add(new Waypoint(last.position.x + x, last.position.y + y, last.position.z + z, last.yRotation + rot));
        }
    }
    private void AddWaypoint(Vector3 pos, float rot)
    {
        AddWaypoint(pos.x, pos.y, pos.z, rot);
    }
    // Start is called before the first frame update
    void Start()
    {
        AddWaypoint(transform.position, transform.rotation.y);
        AddWaypoint(5, 5, 5, 0);
        oldPos = transform.position;

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (waypoints.Count == 0)
        {
            return;
        }
        Waypoint tar = waypoints[index];
        transform.position = Vector3.Lerp(transform.position, tar.position, Time.deltaTime * speed);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0.0f, tar.yRotation, 0.0f), Time.deltaTime * speed);

        if (Vector3.Magnitude(transform.position - tar.position) < 0.01)
        {
            index = (index + 1) % waypoints.Count;
        }

        velocity = (transform.position - oldPos) / Time.deltaTime;
        oldPos = transform.position;

        //if (playerEntered)
        //{
        //    player.SetExternalVelocity(velocity);
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player entered platform");
            //ThirdPersonController cc = other.gameObject.GetComponent<ThirdPersonController>();
            //if (cc != null)
            //{
            //    cc.SetExternalVelocity(velocity);
            //}

            player.SetExternalVelocity(velocity);
            playerEntered = true;

            player.ParentGhost(transform);

           // other.gameObject.transform.parent = transform;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player entered platform");
            //ThirdPersonController cc = other.gameObject.GetComponent<ThirdPersonController>();
            //if (cc != null)
            //{
            //    cc.SetExternalVelocity(velocity);
            //}

            player.SetExternalVelocity(velocity);

            //playerEntered = true;
            // other.gameObject.transform.parent = transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player exited platform");
            //ThirdPersonController cc = other.gameObject.GetComponent<ThirdPersonController>();
            //if (cc != null)
            //{
            //    cc.enabled = true;
            //}

            player.SetExternalVelocity(Vector3.zero);
            playerEntered = false;

            player.ParentGhost(null);
            //other.gameObject.transform.parent = null;
        }
    }
}
