using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class MovingPlatformVelocity : MonoBehaviour
{
    public float resetInterval = 3;
    public float resetDelta;

    public float velocity = 3;
    public Vector3 direction = Vector3.left;
    public Vector3 resetPos;

    private Rigidbody rb;

    public ThirdPersonController player;
    public bool playerEntered;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        resetPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerEntered)
        {
            player.SetExternalVelocity(rb.velocity);
        }

        resetDelta -= Time.deltaTime;
        if (resetDelta < 0)
        {
            rb.velocity = velocity * direction;
            rb.angularVelocity = Vector3.zero;
            transform.position = resetPos;
            transform.rotation = Quaternion.identity;
            resetDelta = resetInterval;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity += Time.fixedDeltaTime * direction;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player entered platform");
            //ThirdPersonController cc = other.gameObject.GetComponent<ThirdPersonController>();
            //if (cc != null)
            //{
            //    cc.enabled = false;
            //}
            //other.gameObject.transform.parent = transform;

            playerEntered = true;
            player.SetExternalVelocity(rb.velocity);

            //player.ParentGhost(transform);
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
            //other.gameObject.transform.parent = null;

            playerEntered = false;
            player.SetExternalVelocity(Vector3.zero);

            //player.ParentGhost(null);
        }
    }
}
