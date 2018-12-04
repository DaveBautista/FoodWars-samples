////////////////////////////////////////////////////////////////////////////////////
////                                                                            ////
////        Name: Enemy Controller                                              ////
////                                                                            ////
////        Author: David Bautista      Date: 3/11/17                           ////
////                                                                            ////
////        Purpose: Controls the enemy AI's reactions and decisions            ////
////                 via player or object interaction. This was made to         ////
////                 centralise key code required to make the enemy AI          ////
////                 behave as intended. All code that has been made to         ////
////                 test the enemy's functions outside of this class           ////
////                 must be imported into this class for keeping the           ////
////                 number of scripts being used to a minimum.                 ////
////                                                                            ////
////////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class EnemyController : MonoBehaviour {

    private NavMeshAgent agent;                 // Enemy AI component
    private Transform   rigTransform;           // PlayerRig Location and Rotation

    // Enemy Body Values
    public float turnSpeed = 1f;                // Turn speed for the enemy towards where they want to face
    public float faceRange = 5f;                // +/- range of degrees for which the enemy 'is' facing where they want to face
    private Animator anim;                      // Animator component pointer
    private Rigidbody[] bodyParts_A;            // Rigidbody collection of bodyparts
    public GameObject bodyHand;                 // Enemy body 'hand' gameobject

    // Enemy Throwing values
    private GameObject[] aimPoints_A;           // Aim Towards these objects
    private bool  atDestination;                // Does the enemy face towards the player now?
    private float throwTimer = 0;               // Throwing timer
    private float throwDelay;                   // Throw Delay constant
    public float throwDelayMin = 5f, throwDelayMax = 10f;
    public float spawnDelay = 3.0f;
    private float rangePower;                   // 'Power' value for how incrementally faster the enemy throw's their projectiles
    public float powerMin = 6f, powerMax = 12f;
    public GameObject[]   projectiles_A;
    private GameObject chosenProjectile;
    private GameObject heldProjectile;

    // Enemy Navigation Values
    public Transform target;

    // Enemy Sound Data
    public AudioClip[] enemySounds;
    public AudioSource enemyAudio;
    public float enemyVolume = 0.5f;

    // Enemy Destruction Values
    private bool hasbeenHit = false;
    private float deleteTimer = 0;
    public float deleteDelay = 3.0f;
    public bool isHit;

    // Use this for initialization
    void Start () {
        isHit = false;
        atDestination = false;

        enemyAudio.volume = enemyVolume;
        rangePower = Random.Range(powerMin, powerMax);
        throwDelay = Random.Range(throwDelayMin, throwDelayMax);
        Debug.Log(rangePower.ToString());

        // Initialise Agent data and Set Agent Destination
        agent = GetComponent<NavMeshAgent>();
        if (target != null){
            agent.SetDestination(target.position);
        } else {
            agent.SetDestination(gameObject.transform.position);
        }

        // Retrieve body info
        anim = GetComponent<Animator>();
        bodyParts_A = GetComponentsInChildren<Rigidbody>();

        // Retrieve Aim Marker Objects
        aimPoints_A = GameObject.FindGameObjectsWithTag("AimMarker");

        foreach (Rigidbody part in bodyParts_A)
        {
            part.isKinematic = true;
        }

        rigTransform = GameObject.FindGameObjectWithTag("CameraRig").transform;

    }
	
	// Update is called once per frame
	void Update () {

        #region |------ Data Collection ------|

        Vector3 dirToFace = new Vector3(
            rigTransform.position.x - transform.position.x,
            0,
             rigTransform.position.z - transform.position.z
            );

        Vector3 currentDir = transform.rotation.eulerAngles;
        #endregion

        #region |------ Enemy_Shooting ------|
        bool facingPlayer = false;
        if (atDestination)
        {
            // Calculate goal rotation
            Quaternion targetLookRot = Quaternion.LookRotation(dirToFace.normalized);

            // Set rotation to a (smoothed) intermediate between current and target rotation
            gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation, targetLookRot, turnSpeed * Time.deltaTime);

            float currentRot = gameObject.transform.rotation.eulerAngles.y;
            float targetRot = targetLookRot.eulerAngles.y;

            if (currentRot <= targetRot + faceRange && currentRot >= targetRot - faceRange)
            {
                facingPlayer = true;
            }
        }

        if (atDestination && facingPlayer)
        {
            if (heldProjectile == null)
            {
                SpawnObject();
            }

            if (throwTimer > throwDelay && heldProjectile != null) {
                anim.SetTrigger("isThrowing");
                Debug.Log(gameObject.name + " is throwing.");
                throwTimer = 0;
            } else {
                throwTimer += Time.deltaTime;
            }
        }

        #endregion

        #region |------ AI_Movement ------|

        // Check if the NavAgent has been disabled or if it has reached its destination
        if (agent.enabled && agent.remainingDistance == 0)
        {
            anim.SetBool("isMoving", false);
            atDestination = true;
        }
        else
        {
            anim.SetBool("isMoving", true);
            atDestination = false;
        }

        #endregion

        #region |------ Enemy_Destruction ------|

        if (isHit)
        {
            if (!hasbeenHit)
            {
                hasbeenHit = true;
                enemyAudio.clip = enemySounds[Random.Range(0, enemySounds.Length)];
                enemyAudio.Play();
            }
            anim.enabled = false;
            ActivateRagdoll();
            agent.enabled = false;
            deleteTimer += Time.deltaTime;
        }

        if (deleteTimer > deleteDelay)
        {
            GameObject.FindGameObjectWithTag("GameManager").GetComponent<EnemySpawner>().EnemiesLeft -= 1;
            GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().PlayerScore += 1;

            Destroy(gameObject);
        }


        #endregion
    }

    /// <summary>
    /// Private function to activate the ragdoll for the enemy
    /// </summary>
    private void ActivateRagdoll()
    {
        foreach (Rigidbody part in bodyParts_A)
        {
            part.isKinematic = false;
        }
    }

    /// <summary>
    /// Public function to spawn the held object for the enemy
    /// </summary>
    public void SpawnObject()
    {
        GameObject m_projectile;
        m_projectile = Instantiate(chosenProjectile, bodyHand.transform.position, Quaternion.identity);
        m_projectile.transform.SetParent(bodyHand.transform);
        heldProjectile = m_projectile;
    }
    /// <summary>
    /// Public function to throw (For now used within the "Throw" animation
    /// </summary>
    public void ThrowObject()
    {
        // Choose a random target point
        int m_point = Random.Range(0, aimPoints_A.Length);

        ProjectileHandler objectData = heldProjectile.GetComponent<ProjectileHandler>();
        objectData.SetThrowingValues(bodyHand.transform.position, rangePower, aimPoints_A[m_point].transform.position);
        heldProjectile.transform.SetParent(null);
        heldProjectile = null;
        anim.SetTrigger("hasThrown");
        throwTimer = 0;
    }

    /// <summary>
    /// Get/Set function for the assigned weapon to this enemy
    /// </summary>
    public GameObject Weapon
    {
        get
        {
            return chosenProjectile;
        }
        set
        {
            chosenProjectile = value;
        }
    }
}
