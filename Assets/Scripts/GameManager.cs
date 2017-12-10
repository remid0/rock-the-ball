﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class GameManager : NetworkBehaviour
{

    // Ball spawns
    public GameObject redPlayerBallSpawn;
    public GameObject bluePlayerBallSpawn;
    private Dictionary<Utility.Team, GameObject> ballSpawnPoints;
    // Ball prefab
    public GameObject ballPrefab;
    // Current ball
    public GameObject ball;
    
    // Managers
    public UIManager uiManager;
    private ServiceManager serviceManager;
    
    // Score
    private Dictionary<Utility.Team, int> score;

    // Wait for players between points
    private Dictionary<Utility.Team, bool> playersReady;
    private bool isWaitingForPlayers;
    private bool mustStartNewPoint;

    // Training only
    private bool canAccessNextStep;
    private Utility.TrainingStep trainingStep;

    [SyncVar]
    private bool triggerGameWin;
    [SyncVar]
    private Utility.Team server;
    [SyncVar(hook = "OnChangeTriggerPointWin")]
    private bool triggerPointWin;
    [SyncVar(hook = "OnChangeTriggerNewBall")]
    private bool triggerNewBall;

    // Debug only, allow to choose the service side
    public Utility.Team startSide;

    public bool IsWaitingForPlayers
    {
        get
        {
            return isWaitingForPlayers;
        }

        set
        {
            isWaitingForPlayers = value;
        }
    }

    public Dictionary<Utility.Team, bool> PlayersReady
    {
        get
        {
            return playersReady;
        }

        set
        {
            // TODO check if it works
            playersReady = value;
            if (!PlayersReady.ContainsValue(false))
            {
                Debug.Log("All players ready");
                isWaitingForPlayers = false; 
            } else
            {
                isWaitingForPlayers = true;
            }
        }
    }

    public bool MustStartNewPoint
    {
        get
        {
            return mustStartNewPoint;
        }

        set
        {
            mustStartNewPoint = value;
        }
    }

    public GameObject Ball
    {
        get
        {
            return ball;
        }

        set
        {
            ball = value;
        }
    }

    public Dictionary<Utility.Team, int> Score
    {
        get
        {
            return score;
        }

        set
        {
            score = value;
        }
    }

    public bool CanAccessNextStep
    {
        get
        {
            return canAccessNextStep;
        }

        set
        {
            canAccessNextStep = value;
        }
    }

    public Utility.TrainingStep TrainingStep
    {
        get
        {
            return trainingStep;
        }

        set
        {
            trainingStep = value;
        }
    }

    private void Start()
    {
        Score = new Dictionary<Utility.Team, int>
        {
            { Utility.Team.BLUE, 0 },
            { Utility.Team.RED, 0 }
        };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        serviceManager = GameObject.FindGameObjectWithTag("ServiceManager").GetComponent<ServiceManager>();
        uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
        Ball = GameObject.FindGameObjectWithTag("Ball");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        PlayersReady = new Dictionary<Utility.Team, bool>
        {
            { Utility.Team.BLUE, true},
            { Utility.Team.RED, true}
        };

        ballSpawnPoints = new Dictionary<Utility.Team, GameObject>
        {
            { Utility.Team.BLUE, bluePlayerBallSpawn },
            { Utility.Team.RED, redPlayerBallSpawn }
        };
        serviceManager = GameObject.FindGameObjectWithTag("ServiceManager").GetComponent<ServiceManager>();
        uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
        server = startSide; 
        
        // launch the training sequence
        if (GameState.training)
        {
            TrainingStep = Utility.TrainingStep.INITIAL;
            GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");

            // Disable goals and replace by a bouncing wall
            foreach (GameObject goal in goals)
            {
                goal.GetComponent<Goal>().isActive = false;
                goal.GetComponent<BouncingWall>().isActive = true;
            }
            StartCoroutine(WaitForInitialization(0.1f));
            // TODO add a vocal message to tell the player to hit the ball
            // When the ball was hit once, enable next step on trigger input
        }
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        //if (MustStartNewPoint && !IsWaitingForPlayers && Input.GetKeyDown(KeyCode.Return))
        if (Input.GetKeyDown(KeyCode.Return) && !GameState.training)
        {
            //MustStartNewPoint = false;
            StartNewPoint();
        }

        // TODO refactor all this logic in another class
        // TODO refactor the playerController code
        // Maybe a class to play without the network
        // Something with inheritance
        if (GameState.training)
        {
            if (CanAccessNextStep && Input.GetKeyDown(KeyCode.Return))
            {
                TrainingStep++;
                CanAccessNextStep = false;
                StartNewTrainingPoint();
            }
        }
    }

    private void StartNewTrainingPoint()
    {
        switch (TrainingStep)
        {
            case Utility.TrainingStep.INITIAL:
                Ball = (GameObject)Instantiate(ballPrefab, ballSpawnPoints[GameState.trainingTeam].transform.position, Quaternion.identity);
                NetworkServer.Spawn(Ball);
                break;

            case Utility.TrainingStep.GOAL:
                Network.Destroy(Ball);
                Ball = (GameObject)Instantiate(ballPrefab, ballSpawnPoints[GameState.trainingTeam].transform.position, Quaternion.identity);
                NetworkServer.Spawn(Ball);
                GoalActivationSwitch(true);
                break;

            // TODO when a service is failed, reinstantiate a ball
            case Utility.TrainingStep.SERVICE:
                // TODO add some textual and audio advice for the player about the service
                StartNewPoint();
                break;
            // TODO disable the ball and the goal
            case Utility.TrainingStep.FREE:
                // TODO add some textual and audio advice for the player about the free training
                Ball = (GameObject)Instantiate(ballPrefab, ballSpawnPoints[server].transform.position, Quaternion.identity);
                NetworkServer.Spawn(Ball);
                GoalActivationSwitch(false);
                break;

            default:
                Debug.Log("GameManager: error, unsupported training step reached");
                break;
        }
    }

    public void ReplaceBall()
    {
        Network.Destroy(Ball);
        StartNewPoint();
    }

    private void StartNewPoint()
    {
        Ball = (GameObject)Instantiate(ballPrefab, ballSpawnPoints[server].transform.position, Quaternion.identity);
        NetworkServer.Spawn(Ball);
        serviceManager.SetNewServiceZone(server);
        if (!GameState.training)
        {
            triggerNewBall = !triggerNewBall;
        }
    }

    public void IncreasePlayerScore(Utility.Team team)
    {
        if (!GameState.training)
        {
            server = Utility.Opp(team);
        }
        else
        {
            server = GameState.trainingTeam;
        }
        Score[team]++;
        IncrementScore(team);
        Network.Destroy(Ball);
        if (Score[team] == Utility.winningScore)
        {
            //uiScores.DisplayWinText(team);
            triggerGameWin = !triggerGameWin;
            // TODO End of the game
        }
        else
        {   
            triggerPointWin = !triggerPointWin;
        }
    }

    public void RelocateBall(Vector3 newPosition)
    {
        if (Ball == null)
        {
            UpdateBall();
        }
        Ball.transform.position = newPosition;
    }

    private void OnChangeTriggerPointWin(bool newVal)
    {        
        foreach (Utility.Team team in Enum.GetValues(typeof(Utility.Team)))
        {
            // TODO better client side handling of this shit
            //PlayersReady[team] = false;
        }
        //IsWaitingForPlayers = true;
        IsWaitingForPlayers = false;
        MustStartNewPoint = true;
    }

    private void OnChangeTriggerNewBall(bool newVal)
    {
        UpdateBall();
    }

    public void UpdateBall()
    {
        Ball = GameObject.FindGameObjectWithTag("Ball");
    }

    private void IncrementScore(Utility.Team team)
    {
        score[team]++;
        uiManager.TeamScoreTrigger = team;
    }

    private void GoalActivationSwitch(bool isEnable)
    {
        GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");
        foreach (GameObject goal in goals)
        {
            Goal goalScript = goal.GetComponent<Goal>();
            if (goalScript.team == Utility.Opp(GameState.trainingTeam))
            {
                goalScript.isActive = isEnable;
                goal.GetComponent<BouncingWall>().isActive = !isEnable;
            }
        }
    }

    IEnumerator WaitForInitialization(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        CanAccessNextStep = false;
        StartNewTrainingPoint();
    }
}
