using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DistanceJoint2D)), RequireComponent(typeof(LineRenderer))]
public class Grapple : MonoBehaviour
{
    
    public Camera mainCamera;
    public Transform ropeOrigin;
    public LayerMask grapplableLayers;
    public float fireDuration;
    [HideInInspector]
    public PlayerMovement player;
    public bool returning { get; private set; }
    public bool attached { get; private set; }
    public bool buffering { get; private set; }
    public DistanceJoint2D rope { get; private set; }
    public LineRenderer ropeRenderer { get; private set; }
    private void Awake()
    {
        rope = gameObject.GetComponent<DistanceJoint2D>();
        ropeRenderer = gameObject.GetComponent<LineRenderer>();
        player = gameObject.GetComponent<PlayerMovement>();
        rope.enabled = false;
        ropeRenderer.enabled = false;
        returning = false;
        attached = false;
    }

    public void Update()
    {
        ropeRenderer.SetPosition(0, ropeOrigin.position);
        if (player.movementEnabled)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!returning)
                {
                    StartCoroutine("CR_FireRope");
                }
                else
                {
                    buffering = true;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (!returning)
                {
                    StartCoroutine("CR_ReturnRope");
                }
                else
                {
                    buffering = false;
                }
            }
            if (rope.enabled && ropeOrigin.position.y > rope.connectedAnchor.y)
            {
                //automatically retracts rope if you are above connecting point, with speed multiplied by the sine of the rope angle (closer to perpendicular, faster retract)
                //TWEAK THIS
                Extend(-player.extendAmount * 0.5f * (rope.attachedRigidbody.position.y - rope.connectedAnchor.y) / (rope.connectedAnchor - rope.attachedRigidbody.position).magnitude);
            }
        }
    }

   public void Extend(float extensionAmount) {
        extensionAmount *= Time.deltaTime;
        float oldDistance = rope.distance;
        float minDistance = 1;
        float newDistance = rope.distance + extensionAmount;

        //prevents using rope against force of gravity
        if (ropeOrigin.position.y > rope.connectedAnchor.y && Mathf.Sign(extensionAmount) == 1) {
            return;
        }

        //Check if new length would put player in object (Needs fixing)
        RaycastHit2D[] playerInObject = new RaycastHit2D[1];
        ContactFilter2D castValues = new ContactFilter2D();
        castValues.SetLayerMask(grapplableLayers);
        Vector2 direction = rope.connectedAnchor - rope.attachedRigidbody.position;
        rope.attachedRigidbody.Cast(direction, castValues, playerInObject, (-(Mathf.Abs(extensionAmount) + 0.5f) * Mathf.Sign(extensionAmount)));
        if (newDistance > minDistance && playerInObject[0].collider == null) { 
            rope.distance = newDistance;
        }
    }

    public IEnumerator CR_FireRope() {
        if (!attached)
        {
            Vector2 origin = ropeOrigin.position;
            Vector2 targetPoint = (Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(origin, (targetPoint-origin).normalized, (targetPoint-origin).magnitude * 1.1f, grapplableLayers);
            if (hit.collider != null)
            {
                targetPoint = hit.point;
            }
            ropeRenderer.enabled = true;
            for (float time = 0; time < fireDuration; time += Time.deltaTime) {
                ropeRenderer.SetPosition(1, Vector2.Lerp(origin, targetPoint, time/fireDuration));
                if (!Input.GetMouseButton(0)) {
                    StartCoroutine("CR_ReturnRope");
                    yield break;
                }
                yield return null;
            }
            ropeRenderer.SetPosition(1, targetPoint);
            if (hit.collider != null && player.movementEnabled)
            {
                rope.enabled = true;
                rope.connectedAnchor = targetPoint;
                attached = true;
            }
            else {
                StartCoroutine("CR_ReturnRope");
            }
        }
        else {
            yield return null;
        }
    }
    public IEnumerator CR_ReturnRope() {
        if (!returning)
        {
            returning = true;
            attached = false;
            rope.enabled = false;
            Vector2 origin = ropeOrigin.position;
            Vector2 startPoint = ropeRenderer.GetPosition(ropeRenderer.positionCount-1);
            for (float time = 0; time < fireDuration * 0.35f; time += Time.deltaTime)
            {
                ropeRenderer.SetPosition(1, Vector2.Lerp(startPoint, ropeOrigin.position, time / (0.35f *fireDuration)));
                yield return null;
            }
            ropeRenderer.enabled = false;
            returning = false;
            if (buffering) {
                buffering = false;
                StartCoroutine("CR_FireRope");
            }
        }
        else
        {
            yield return null;
        }
    }
}
