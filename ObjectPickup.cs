using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPickup : MonoBehaviour {

    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private Valve.VR.EVRButtonId touchButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
    private Valve.VR.EVRButtonId menuButton = Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedObj.index); } }
    private SteamVR_TrackedObject trackedObj;

    public GameObject throwObject;
    public ObjectPickup otherController;

    [SerializeField]
    private GameObject obj;
    private FixedJoint fJoint;
    private GameManager gameOverseer;
    private Material heldObjMat;
    private bool isHit = false;
    public float rumbleDelay = 0.5f;
    private float rumbleTimer = 0;

    public bool squeezing = false;
    private bool throwing;
    private new Rigidbody rigidbody;

	// Use this for initialization
	void Start () {
        gameOverseer = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        fJoint = GetComponent<FixedJoint>();
	}
	
	// Update is called once per frame
	void Update () {

        if (isHit)
        {
            // If the player has been hit, rumble the controller for the designated time threshold
            controller.TriggerHapticPulse(2000);
            rumbleTimer += Time.deltaTime;
            if (rumbleTimer > rumbleDelay)
            {
                isHit = false;
                rumbleTimer = 0;
            }
            
        }

        // Check if the controller has been intialised
        if (controller == null)
        {
            Debug.Log("Controller not initialized.");
            return;
        }

        // Check if the menu button has been pressed
        if(controller.GetPressDown(menuButton))
        {
            GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().isPausingNext = true;
        }

        // If the button is pressed, pick up the object
        if (controller.GetPressDown(triggerButton))
        {
            if (obj.tag == "SpawnBox")
            {
                GameObject objectClone;
                GameObject spawnClone = obj.GetComponent<SpawnBoxController>().spawnObject;
                objectClone = Instantiate(spawnClone, transform.position, transform.rotation);
                objectClone.GetComponent<Rigidbody>().isKinematic = false;
                obj = objectClone;
                fJoint.connectedBody = objectClone.GetComponent<Rigidbody>();
            }
            print("triggered!");
            PickUpObj();
        }

        // If the button is not pressed, drop the object
        if (controller.GetPressUp(triggerButton))
            DropObj();

        if (obj != null && obj.tag != "SpawnBox")
        {
            // For melee weapons, if the held/detected object is a certain colour, change all the rest of its pieces to that colour
            if (obj.GetComponentInParent<MeleeHandler>() != null)
            {
                if (obj.transform.GetChild(0).GetComponent<MeshRenderer>().material != obj.GetComponentInParent<MeleeHandler>().OutlineMaterial)
                {
                    obj.GetComponentInParent<MeleeHandler>().OutlineMaterial = obj.transform.GetChild(0).GetComponent<MeshRenderer>().material;
                }
            }

            // EASTER EGG: if the object contains a hidden sound, change the BGM to it
            if (obj.GetComponent<ProjectileHandler>().hiddenSound != null)
            {
                if (gameOverseer.Music != obj.GetComponent<ProjectileHandler>().hiddenSound)
                    gameOverseer.Music = obj.GetComponent<ProjectileHandler>().hiddenSound;

            // Otherwise, change it back to the ordinary music
            } else if (gameOverseer.bgmObj.clip != gameOverseer.normalBGM) {
                gameOverseer.Music = gameOverseer.normalBGM;
            }
        }
    }

    void FixedUpdate()
    {
        if (throwing)
        {
            Transform origin;
            if (trackedObj.origin != null)
            {
                origin = trackedObj.origin;
            }
            else
            {
                origin = trackedObj.transform.parent;
            }

            if (origin !=  null)
            {
                rigidbody.velocity = origin.TransformVector(controller.velocity);
                rigidbody.angularVelocity = origin.TransformVector(controller.angularVelocity * 0.25f);
            }
            else
            {
                rigidbody.velocity = controller.velocity;
                rigidbody.angularVelocity = controller.angularVelocity * 0.25f;
            }

            rigidbody.maxAngularVelocity = rigidbody.angularVelocity.magnitude;

            GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().foodThrown += 1;
            throwing = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check an object when it enters detection range
        if (other.tag == "Pickupable" || other.tag == "SpawnBox" || other.GetComponent<ProjectileHandler>() != null)
        {
            obj = other.gameObject;

            // Colour the outline green to show it is available
            if (other.tag == "Pickupable" || other.GetComponent<ProjectileHandler>() != null)
                gameOverseer.OutlineColour(other.transform.GetChild(0).GetComponent<MeshRenderer>(), gameOverseer.outlineCollection[2]);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Uncheck an object when it leaves the detection range
        if (other.tag == "Pickupable" || other.tag == "SpawnBox" || other.GetComponent<ProjectileHandler>() != null)
        {
            if (other.tag != "SpawnBox")
            {
                // Colour it red if player fails to 'catch' the object, otherwise white if they did
                int colour = (other.GetComponentInParent<ProjectileHandler>().enemyThrown) ? 2 : 0;
                gameOverseer.OutlineColour(other.transform.GetChild(0).GetComponent<MeshRenderer>(), gameOverseer.outlineCollection[colour]);
            }
            obj = null;
        }
    }

    /// <summary>
    /// Called for attaching the detected obj to the controller for player interactions
    /// </summary>
    void PickUpObj()
    {
        if (obj != null) {

            fJoint.connectedBody = obj.GetComponent<Rigidbody>();
            obj.GetComponent<Rigidbody>().isKinematic = false;
            throwing = false;

            if (obj.GetComponent<ProjectileHandler>() != null)
            {
                obj.GetComponent<ProjectileHandler>().enemyThrown = false;
                obj.GetComponent<ProjectileHandler>().playerThrown = true;
            }
        } else {
            fJoint.connectedBody = null;
        }
    }

    /// <summary>
    /// Called for letting go of the detected obj for player interactions
    /// </summary>
    void DropObj()
    {
        if (fJoint.connectedBody != null)
        {
            rigidbody = fJoint.connectedBody;
            fJoint.connectedBody = null;
            throwing = true;

            if (obj.GetComponent<ProjectileHandler>().hiddenSound != null)
            {
                int choice = (gameOverseer.isGameActive) ? 0 : 1;
                gameOverseer.Music = gameOverseer.soundFiles[choice];
            }

            obj = null;
        }
    }

    /// <summary>
    /// Get/Set Function for checking if the player has been hit and applying the 'Rumble' effect
    /// </summary>
    public bool CheckHit
    {
        get
        {
            return isHit;
        }
        set
        {
            if (isHit)
                rumbleTimer = 0;
            else
                isHit = value;
        }
    }
}
