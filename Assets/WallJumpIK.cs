using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class WallJumpIK : MonoBehaviour
{
    ChainIKConstraint chainIK;
    TwoBoneIKConstraint twoBoneIK;

    float sphereCastRad = 0.1f;
    float sphereCastDist = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        chainIK = GetComponent<ChainIKConstraint>();
        twoBoneIK = GetComponent<TwoBoneIKConstraint>();
    }

    // Update is called once per frame
    void Update()
    {
        chainIK.weight -= Time.deltaTime * 1f;
        twoBoneIK.weight -= Time.deltaTime * 2f;
    }

    public void StartWallJump(Vector3 position, Vector3 direction, LayerMask GroundLayers)
    {
        Vector3 footPos = chainIK.data.tip.position;

        Physics.SphereCast(footPos, sphereCastRad, direction, out RaycastHit raycastHit, sphereCastDist, GroundLayers);

        //transform.position = raycastHit.point;

        twoBoneIK.weight = 0.7f;
    }
}
