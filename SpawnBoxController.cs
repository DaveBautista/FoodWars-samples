using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBoxController : MonoBehaviour {

    public GameObject spawnObject;
    public bool randOBJ = true;
    public GameObject[] weaponList;
    private GameObject[] projectileObjects;

    private float changeTimer = 0;
    public float changeDelay = 0.5f;
	// Use this for initialization
	void Start () {
        projectileObjects = Resources.LoadAll<GameObject>("PlayerObjects");
    }
	
	// Update is called once per frame
	void Update () {

        changeTimer += Time.deltaTime;
        if (changeTimer > changeDelay)
        {
            GameObject [] usedTable = (randOBJ) ? projectileObjects : weaponList;
            spawnObject = usedTable[Random.Range(0, usedTable.Length - 1)];
            changeTimer = 0;
        }
	}
}
