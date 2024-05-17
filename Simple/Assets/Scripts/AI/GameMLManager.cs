using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.SceneManagement;

public class GameMLManager : Agent
{
    public GameObject playerBasePrefab;
    public GameObject playerBarracksPrefab;
    public GameObject enemyBasePrefab;
    public GameObject enemyBarracksPrefab;

    private GameObject agentBase;
    private GameObject agentBarracks;
    private GameObject enemyBase;
    private GameObject enemyBarracks;

    private int lastTotalGold;
    private int lastWorkerCount;
    private int lastWarriorCount;
    private int lastArcherCount;
    private int lastEnemyWorkerCount;
    private int lastEnemyWarriorCount;
    private int lastEnemyArcherCount;
    private int lastEnemyBarracksCount;

    private float guardCooldownTimer = 0f;
    private const float guardCooldownDuration = 15f;  // 15 seconds cooldown

    private TrainingStepDisplay trainingStepDisplay;

    public override void Initialize()
    {
        if (GoldMLManager.Instance == null)
        {
            Debug.LogError("GoldMLManager instance is not initialized.");
            return;
        }
        if (UnitMLManager.Instance == null)
        {
            Debug.LogError("UnitMLManager instance is not initialized.");
            return;
        }
        if (EnemyUnitManager.Instance == null)
        {
            Debug.LogError("EnemyUnitManager instance is not initialized.");
            return;
        }

        enemyBase = GameObject.Find("EnemyBase");
        enemyBarracks = GameObject.Find("EnemyBarracks");
        agentBase = GameObject.Find("Base");
        agentBarracks = GameObject.Find("Barracks");
        lastEnemyBarracksCount = enemyBarracks != null ? 1 : 0;

        trainingStepDisplay = FindObjectOfType<TrainingStepDisplay>();
    }

    void Update()
    {
        if (enemyBase == null || !enemyBase.gameObject.activeInHierarchy)
        {
            WinGame();
        }
        if (agentBase == null || !agentBase.gameObject.activeInHierarchy)
        {
            LoseGame();
        }

        if (trainingStepDisplay != null)
        {
            trainingStepDisplay.UpdateStepText(StepCount);
        }

        // Update the guard cooldown timer
        if (guardCooldownTimer > 0)
        {
            guardCooldownTimer -= Time.deltaTime;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(GoldMLManager.Instance.TotalGold);
        sensor.AddObservation(UnitMLManager.Instance.workerCount);
        sensor.AddObservation(UnitMLManager.Instance.warriorCount);
        sensor.AddObservation(UnitMLManager.Instance.archerCount);
        sensor.AddObservation(EnemyUnitManager.Instance.workerCount);
        sensor.AddObservation(EnemyUnitManager.Instance.warriorCount);
        sensor.AddObservation(EnemyUnitManager.Instance.archerCount);
        sensor.AddObservation(enemyBarracks != null ? 1 : 0);
        sensor.AddObservation(enemyBase != null);
        sensor.AddObservation(guardCooldownTimer);  // Include the guard cooldown timer as an observation
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;

        if (discreteActions.Length < 5)
        {
            Debug.LogError("Expected at least 5 discrete actions but received " + discreteActions.Length);
            return;
        }

        int workerCount = UnitMLManager.Instance.workerCount;
        int warriorCount = UnitMLManager.Instance.warriorCount;
        int archerCount = UnitMLManager.Instance.archerCount;
        int soldiers = warriorCount + archerCount;

        if (discreteActions[4] == 1)
        {
            UnitMLManager.Instance.AssignAttackingTasks();
            // Check if the agent should go into attack mode
            if (soldiers >= 16 && guardCooldownTimer <= 0)
            {
                UnitMLManager.Instance.AssignAttackingTasks();
                guardCooldownTimer = guardCooldownDuration;  // Reset the guard cooldown timer

            }
        }
        else
        {
            if (UnitMLManager.Instance.totalUnits < UnitMLManager.Instance.maxUnits)
            {
                if (discreteActions[0] == 1 && workerCount <= 10 && UnitMLManager.Instance.baseSpawnPoint != null && UnitMLManager.Instance.baseSpawnPoint.gameObject.activeInHierarchy)
                {
                    UnitMLManager.Instance.SpawnWorker();
                }
                if (discreteActions[1] == 1 && UnitMLManager.Instance.barracksSpawnPoint != null && UnitMLManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
                {
                    UnitMLManager.Instance.SpawnWarrior();
                }
                if (discreteActions[2] == 1 && UnitMLManager.Instance.barracksSpawnPoint != null && UnitMLManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
                {
                    UnitMLManager.Instance.SpawnArcher();
                }
            }

            if (discreteActions[3] == 1 && guardCooldownTimer <= 0)
            {
                UnitMLManager.Instance.AssignGuardTasks();
            }
        }

        EvaluateRewards();

        if (StepCount >= 200000)
        {
            AddReward(-15.0f);
            EndEpisode();
        }

        if (trainingStepDisplay != null)
        {
            trainingStepDisplay.UpdateStepText(StepCount);
        }
    }

    private void EvaluateRewards()
    {
        int currentGold = GoldMLManager.Instance.TotalGold;
        RewardForResourceChanges(currentGold);
        RewardForUnitManagement();
    }

    private void RewardForResourceChanges(int currentGold)
    {
        if (currentGold > lastTotalGold)
        {
            AddReward((currentGold - lastTotalGold) * 0.01f);
        }
        lastTotalGold = currentGold;
    }

    private void RewardForUnitManagement()
    {
        int currentWorkerCount = UnitMLManager.Instance.workerCount;
        int currentWarriorCount = UnitMLManager.Instance.warriorCount;
        int currentArcherCount = UnitMLManager.Instance.archerCount;

        RewardForCreatingUnits(currentWorkerCount, currentWarriorCount, currentArcherCount);
        PenalizeForLosingUnits(currentWorkerCount, currentWarriorCount, currentArcherCount);

        lastWorkerCount = currentWorkerCount;
        lastWarriorCount = currentWarriorCount;
        lastArcherCount = currentArcherCount;
    }

    private void RewardForCreatingUnits(int workers, int warriors, int archers)
    {
        if (workers > lastWorkerCount)
            AddReward(1f);
        if (warriors > lastWarriorCount)
        {
            if (UnitMLManager.Instance.warriorCount > 11)
            {
                AddReward(-0.1f);
            }
            else
            {
                AddReward(1.5f);
            }
        }
        if (archers > lastArcherCount)
        {
            if (UnitMLManager.Instance.archerCount > 8)
            {
                AddReward(-0.2f);
            }
            else
            {
                AddReward(1.2f);
            }
        }

        if (enemyBarracks == null || !enemyBarracks.gameObject.activeInHierarchy)
        {
            AddReward(7.0f);
        }
        if (agentBarracks == null || !agentBarracks.gameObject.activeInHierarchy)
        {
            AddReward(-6.0f);
        }
    }

    private void PenalizeForLosingUnits(int workers, int warriors, int archers)
    {
        if (workers < lastWorkerCount)
            AddReward(-1.0f);
        if (warriors < lastWarriorCount)
            AddReward(-0.7f);
        if (archers < lastArcherCount)
            AddReward(-0.8f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.E))
            discreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.R))
            discreteActionsOut[2] = 1;
        if (Input.GetKey(KeyCode.G))
            discreteActionsOut[3] = 1;
        if (Input.GetKey(KeyCode.T))
            discreteActionsOut[4] = 1;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode Begin: Resetting environment.");

        // Reset gold and units
        GoldMLManager.Instance.ResetGold();
        UnitMLManager.Instance.ResetUnits();
        EnemyUnitManager.Instance.ResetUnits();
        EnemyGameManager.Instance.ResetGame();

        // Stop any ongoing spawning processes
        UnitMLManager.Instance.StopAllSpawning();
        EnemyUnitManager.Instance.StopAllSpawning();

        // Ensure structures are properly reset
        ResetStructures();

        // Initialize last counts
        lastTotalGold = GoldMLManager.Instance.TotalGold;
        lastWorkerCount = UnitMLManager.Instance.workerCount;
        lastWarriorCount = UnitMLManager.Instance.warriorCount;
        lastArcherCount = UnitMLManager.Instance.archerCount;

        enemyBase = GameObject.Find("EnemyBase");
        enemyBarracks = GameObject.Find("EnemyBarracks");
        agentBase = GameObject.Find("Base");
        agentBarracks = GameObject.Find("Barracks");
        lastEnemyBarracksCount = enemyBarracks != null ? 1 : 0;

        Debug.Log("Environment reset complete.");
    }

    private void ResetStructures()
    {
        if (agentBase == null || !agentBase.gameObject.activeInHierarchy)
        {
            agentBase.GetComponent<Base>().ResetBase();
        }

        if (agentBarracks == null || !agentBarracks.gameObject.activeInHierarchy)
        {
            agentBarracks.GetComponent<Barracks>().ResetBarracks();
        }

        if (enemyBase == null || !enemyBase.gameObject.activeInHierarchy)
        {
            enemyBase.GetComponent<EnemyBase>().ResetEnemyBase();
        }

        if (enemyBarracks == null || !enemyBarracks.gameObject.activeInHierarchy)
        {
            enemyBarracks.GetComponent<EnemyBarracks>().ResetEnemyBarracks();
        }
    }

    public void WinGame()
    {
        AddReward(20.0f);
        EndEpisode();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoseGame()
    {
        AddReward(-10.0f);
        EndEpisode();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
