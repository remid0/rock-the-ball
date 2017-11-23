﻿using UnityEngine;
using UnityEngine.Networking;


public class Ball : NetworkBehaviour {

	private Rigidbody rb;
	public Vector3 testForce;
    public float maxSpeed;
    public GameManager gameManager;

    public bool debugMode;

    private bool isServed = false;
    private Utility.Team servingPlayer;

    public Utility.Team ServingPlayer
    {
        get
        {
            return servingPlayer;
        }

        set
        {
            servingPlayer = value;
        }
    }

    // Must be executed everywhere
    // Warning, maybe smarter to do in the OnStart methods
    void Start () {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        rb = GetComponent<Rigidbody>();
        //rb.AddForce(testForce);
        if (maxSpeed == 0)
        {
            maxSpeed = 10000f;
        }
        if (debugMode) {
            isServed = true;
        }
    }

    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //    gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    //}

    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    //}

    void FixedUpdate()
    {
        // Speed cap
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Server only for collision detection
        if (!isServer)
        {
            return;
        }

        // TODO if the opposite player hit the ball, he loses the point
        if (isServed)
        {
            if (!other.CompareTag("Racket"))
            {
                isServed = false;
                gameManager.ResetServiceZone();
                // Check to see if the service is good
                if (other.CompareTag("ServiceZone") && other.GetComponent<ServiceZone>().IsValid)
                {
                    Debug.Log("Service in");
                }
                else
                {
                    Debug.Log("Service out");
                    gameManager.IncreasePlayerScore(Utility.Opp(ServingPlayer));
                }
            }
            // Check for a potential goal
        } else if (other.CompareTag("Goal") && other.gameObject.GetComponent<Goal>().isActive)
        {
            gameManager.IncreasePlayerScore(Utility.Opp(other.gameObject.GetComponent<Goal>().team));
        } else if (other.CompareTag("Racket"))
        {
            Debug.Log("TEST player service");
            isServed = true;
        }
    }
}
