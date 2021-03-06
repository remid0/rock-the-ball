﻿using UnityEngine;
using UnityEngine.Networking;

public class ServiceManager : NetworkBehaviour {

    private GameObject[] serviceZones;
    private GameObject currentServiceZone;
    [SyncVar (hook ="OnChangeCurrentServiceZoneIndex")]
    private int currentServiceZoneIndex = -1;

    private bool isServed;
    private Utility.Team servingPlayer;

    private GameManager gameManager;
    private SoundManager soundManager;

    public GameManager GameManager
    {
        get
        {
            return gameManager;
        }

        set
        {
            gameManager = value;
        }
    }

    public bool IsServed
    {
        get
        {
            return isServed;
        }

        set
        {
            isServed = value;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        serviceZones = GameObject.FindGameObjectsWithTag("ServiceZone");
        foreach (GameObject serviceZone in serviceZones)
        {
            serviceZone.GetComponent<ServiceZone>().ServiceManager = this;
        }
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        soundManager = GameObject.FindGameObjectWithTag("SoundManager").GetComponent<SoundManager>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        serviceZones = GameObject.FindGameObjectsWithTag("ServiceZone");
        foreach (GameObject serviceZone in serviceZones)
        {
            serviceZone.GetComponent<ServiceZone>().ServiceManager = this;
        }
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        soundManager = GameObject.FindGameObjectWithTag("SoundManager").GetComponent<SoundManager>();
    }

    public void HandleService(bool isIn)
    {
        if (!isServer || !IsServed)
        {
            return;
        }

        ResetServiceZone();

        IsServed = false;

        if (isIn)
        {
            soundManager.PlaySound("NiceShot");
        } else
        {
            soundManager.PlaySound("Out");
            if (!GameState.training)
            {
                GameManager.IncreasePlayerScore(Utility.Opp(servingPlayer));
            } else
            {
                GameManager.ReplaceBall();
            }
        }
    }


    public void SetNewServiceZone(Utility.Team sp)
    {
        servingPlayer = sp;
        IsServed = true;
        currentServiceZoneIndex = UnityEngine.Random.Range(0, serviceZones.Length);
        currentServiceZone = serviceZones[currentServiceZoneIndex];
        currentServiceZone.GetComponent<ServiceZone>().IsValid = true;
    }


    public void ResetServiceZone()
    {
        if (currentServiceZone != null)
        {
            currentServiceZone.GetComponent<ServiceZone>().IsValid = false;
            currentServiceZoneIndex = -1;
        }
    }

    private void OnChangeCurrentServiceZoneIndex(int newIndex)
    {
        if (newIndex == -1)
        {
            serviceZones[currentServiceZoneIndex].GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            // To set a new ServiceZone, the service zone are supposed to be reset first
            if (currentServiceZoneIndex != -1)
            {
                serviceZones[currentServiceZoneIndex].GetComponent<MeshRenderer>().enabled = false;
            }
            serviceZones[newIndex].GetComponent<MeshRenderer>().enabled = true;
            currentServiceZoneIndex = newIndex;
        }
    }
}