﻿using UnityEngine;


public class RacketController : MonoBehaviour {

    public PlayerController playerController;
    public SoundManager soundManager;

    void Start()
    {
        soundManager = GameObject.FindGameObjectWithTag("SoundManager").GetComponent<SoundManager>();
    }

    
	void OnTriggerEnter (Collider collider) {
        if (collider.CompareTag("Ball"))
        {
            Debug.Log("Racket Controller : collision detected");
            playerController.BallHit();
            soundManager.PlaySound("RacketHit");
        }
	}
}
