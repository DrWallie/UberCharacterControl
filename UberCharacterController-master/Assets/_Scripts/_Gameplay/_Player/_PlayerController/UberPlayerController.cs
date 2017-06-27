using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MovSettings
{
    public float movSpd = 6f; //public Vector2 movSpd = new Vector2(4, 6);
    public float runMult = 2.0f;
    public float maxVelocityChange = 10f;

    [Space(10)]
    public float gravity = -20f;
    public float maxJumpApex = 4f;
    public float minJumpApex = 3f;
    public float secToApex = .5f;

    [Space(10)]
    public float jumpPauzeDur = .1f;
    public LayerMask ignore;

    [Space(10)]
    public float maxAirTimeUnharmed = .25f;
    public float damgPerSecAirTime = 25f;
}

[System.Serializable]
public class CamSettings
{
    public bool useCamera = true;
    public Transform myCamera;
    public Vector2 sensitivity;

    public bool clampRot = true;
    [Range(0f, 360f)]
    public float clampΔ = 170f;
    [Space(10)]
    public bool headBobEnabled = true;
    public float headBobSpd = 0.15f;
    public float headBobRotMult = 0.25f;
}

[System.Serializable]
public class BasicGround
{
    public RaycastHit hit;
    public RaycastHit backHit;
    public RaycastHit frontHit;

    public BasicGround(RaycastHit _hit, RaycastHit _backHit, RaycastHit _frontHit)
    {
        hit = _hit;
        backHit = _backHit;
        frontHit = _frontHit;
    }
}

[System.Serializable]
public class CollisionSphere
{
    public float Offset;
    public bool IsFeet;
    public bool IsHead;

    public CollisionSphere(float offset, bool isFeet, bool isHead)
    {
        Offset = offset;
        IsFeet = isFeet;
        IsHead = isHead;
    }
}

[RequireComponent(typeof(Rigidbody))]
public class UberPlayerController : MonoBehaviour
{
    public Rigidbody rigid;

    [Space(10)]
    public MovSettings movementSettings;
    [Space(10)]
    public CamSettings cameraSettings;

    [Space(10)]
    [SerializeField]
    CollisionSphere[] spheres = new CollisionSphere[2] { new CollisionSphere(0.5f, true, false), new CollisionSphere(1.5f, false, true) };

    float minJumpVel, maxJumpVel;
    private Vector3 camOriginal;

    private float jumpTimer, airTime;
    private float spd;

    float xRot, zRot, bobTimer;


    private const float tolerance = 0.05f;

    public static UberPlayerController instance;

    public BasicGround currentBasicGround
    {
        get; private set;
    }

    float radius = 0.5f;
    private bool contact;

    void Start()
    {
        if (cameraSettings.myCamera == null && cameraSettings.useCamera) //In case you forget to assign the Camera or the Rigidbody
        {
            cameraSettings.myCamera = GetComponentInChildren<Camera>().transform;
            camOriginal = GetComponentInChildren<Camera>().transform.localPosition;
        }
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody>();
        }

        CursorLock.SetPlayerScripts(this);//assigned so that the player-controller will be disabled on pauze.

        rigid.freezeRotation = true;
        rigid.useGravity = false;

        instance = this;
    }

    void Update()
    {
        #region Camera
        if (cameraSettings.useCamera)
        {
            LookRot();
        }
        #endregion

        #region Movement

        spd = (Input.GetButton("Sprint") ? movementSettings.movSpd * movementSettings.runMult : movementSettings.movSpd); //Sets speed according to runMult if you are pressing the Sprint Button.

        Vector3 targVel = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")); //The axes of your input will be stored in move direction
        targVel = transform.TransformDirection(targVel); //Axes gets converted to a direction
        targVel *= spd; //Move direction gets multplied by "speed" before jumping and Gravity so those won't be boosted.

        Vector3 currentVel = rigid.velocity;
        Vector3 velocityChange = (targVel - currentVel);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -movementSettings.maxVelocityChange, movementSettings.maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -movementSettings.maxVelocityChange, movementSettings.maxVelocityChange);
        velocityChange.y = 0;

        #endregion

        #region Jumps

        maxJumpVel = Mathf.Sqrt(2 * Mathf.Abs(movementSettings.gravity) * movementSettings.maxJumpApex);
        minJumpVel = Mathf.Sqrt(2 * Mathf.Abs(movementSettings.gravity) * movementSettings.minJumpApex);

        if (Input.GetButtonDown("Jump"))// && isGrnded)//Als jump word gepressed..
        {
            rigid.velocity = new Vector3(velocityChange.x, maxJumpVel, velocityChange.z);//word de velocity naar max Jump Velocity gezet.
        }
        if (Input.GetButtonUp("Jump"))// && !isGrnded)//Als jump word losgelaten terwijl de player NIET op de grond is..
        {
            if (rigid.velocity.y > minJumpVel) //En de current velocity hoger is dan t minimum..
            {
                rigid.velocity = new Vector3(velocityChange.x, minJumpVel, velocityChange.z);//Word de current velocity naar het minimum gezet.
            }
        }
        #endregion

        #region CollisionCheck

        contact = false;

        foreach (Collider col in Physics.OverlapSphere(transform.position, radius))
        {
            Vector3 pushBack = transform.position - col.ClosestPointOnBounds(transform.position);

            transform.position += Vector3.ClampMagnitude(pushBack, Mathf.Clamp(radius - pushBack.magnitude, 0, radius));//

            contact = true;
        }

        #endregion

        rigid.AddForce(velocityChange, ForceMode.VelocityChange);

        rigid.AddForce(new Vector3(0, -movementSettings.gravity, 0));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = contact ? Color.green : Color.red;
        Gizmos.DrawWireSphere(OffsetPosition(spheres[0].Offset), radius);
    }

    void CheckGround()
    {
        Vector3 offsetPosition = OffsetPosition(spheres[0].Offset) + (transform.up * tolerance);

        RaycastHit hit;

        if (Physics.SphereCast(offsetPosition, radius, -transform.up, out hit, -movementSettings.ignore))
        {
            // Remove the tolerance from the distance travelled
            hit.distance -= tolerance;

            //RayCasts downwards behind and in front of our hit, avoids problem of SphereCasts interpolating hit.normals when it collides with the edge of a surface
            Vector3 backDirection = Math3D.ProjectVectorOnPlane(hit.normal, hit.point - transform.position).normalized * tolerance;
            backDirection += transform.up * tolerance;
            Vector3 backPoint = hit.point + backDirection;

            Vector3 frontDirection = Math3D.ProjectVectorOnPlane(transform.up, hit.point - transform.position).normalized * tolerance;
            Vector3 frontPoint = hit.point - frontDirection;

            RaycastHit backHit;
            RaycastHit frontHit;

            Physics.Raycast(backPoint, -transform.up, out backHit, -movementSettings.ignore);
            Physics.Raycast(frontPoint, -transform.up, out frontHit, -movementSettings.ignore);

            currentBasicGround = new BasicGround(hit, backHit, frontHit);
        }
    }

    Vector3 OffsetPosition(float y)
    {
        Vector3 p;

        p = transform.position;

        p.y += y;

        return p;
    }

    /*public bool IsGrnded()
    {
        RaycastHit hit;
        //return (Physics.SphereCast(transform.position + coll.center, coll.radius, -transform.up, out hit, (coll.height / 2) * 1.1f));
        return false;
    }*/

    #region cameraPhysics
    public void LookRot()
    {
        xRot += Input.GetAxis("Mouse Y") * cameraSettings.sensitivity.x;
        zRot = (cameraSettings.headBobEnabled ? HeadBob() : 0f);

        if (cameraSettings.clampRot) { xRot = Mathf.Clamp(xRot, -(cameraSettings.clampΔ / 2), (cameraSettings.clampΔ / 2)); }

        cameraSettings.myCamera.transform.localEulerAngles = new Vector3(-xRot, 0, zRot);
        transform.Rotate(0, Input.GetAxis("Mouse X") * cameraSettings.sensitivity.y, 0);
    }

    public float HeadBob()
    {
        float waveSlice = 0.0f;
        if (Mathf.Abs(Input.GetAxis("Horizontal")) == 0 && Mathf.Abs(Input.GetAxis("Vertical")) == 0)
        {
            bobTimer = 0f;
        }
        else
        {
            waveSlice = Mathf.Sin(bobTimer);
            bobTimer = bobTimer + (cameraSettings.headBobSpd * (Input.GetButtonDown("Sprint") ? movementSettings.runMult : 1));

            if (bobTimer > Mathf.PI * 2)
            {
                bobTimer = bobTimer - (Mathf.PI * 2);
            }
        }

        if (waveSlice != 0)
        {
            float change = waveSlice * cameraSettings.headBobRotMult;
            float inputAxes = Mathf.Abs(Input.GetAxis("Horizontal") + Mathf.Abs(Input.GetAxis("Vertical")));
            inputAxes = Mathf.Clamp(inputAxes, 0f, 1f);
            change = inputAxes * change;

            return (change);
        }
        else
        {
            return (0f);
        }
    }

    public static void Shake(float duration, float amount)//ought to be called if you wish to start screen shake
    {
        instance.StartCoroutine(instance.scrShake(duration, amount));
    }

    public IEnumerator scrShake(float duration, float amount)
    {
        while (duration > 0)
        {
            cameraSettings.myCamera.localPosition = camOriginal + Random.insideUnitSphere * amount;

            duration -= Time.deltaTime;

            yield return null;
        }

        cameraSettings.myCamera.localPosition = camOriginal;
    }

    #endregion
}