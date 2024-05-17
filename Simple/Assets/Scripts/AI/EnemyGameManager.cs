using UnityEngine;

public class EnemyGameManager : MonoBehaviour
{
    public static EnemyGameManager Instance { get; set; }
    public int TotalGold { get; private set; }
    public GameState CurrentState { get; private set; }

    public int targetWorkersResource = 3;
    public int targetTroopsResource = 6;  // Increased target
    public int targetWorkersGuard = 9;
    public int targetTroopsGuard = 19;
    public int targetGold = 150;

    public delegate void EnemyGoldChanged(int goldAmount);
    public static event EnemyGoldChanged OnEnemyGoldChanged;

    public enum GameState
    {
        ResourceGatheringPhase,
        GuardPhase,
        AttackPhase,
        EconomicPhase,
        CombatPhase
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetGameState(GameState.ResourceGatheringPhase);
        TotalGold = 40;
    }

    void Update()
    {
        HandleCurrentState();
        HandleAIPhases();
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChange(newState);
    }

    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.ResourceGatheringPhase:
                HandleResourceGatheringPhase();
                break;
            case GameState.GuardPhase:
                HandleGuardPhase();
                break;
            case GameState.AttackPhase:
                HandleAttackPhase();
                break;
            case GameState.CombatPhase:
                HandleCombatPhase();
                break;
            case GameState.EconomicPhase:
                HandleEconomicPhase();
                break;
        }
    }

    private void HandleResourceGatheringPhase()
    {
        float randomValue = Random.Range(0f, 1f);

        if (EnemyUnitManager.Instance.workerCount < targetWorkersResource && randomValue < 0.4f && EnemyUnitManager.Instance.baseSpawnPoint != null && EnemyUnitManager.Instance.baseSpawnPoint.gameObject.activeInHierarchy)
        {
            EnemyUnitManager.Instance.SpawnWorker();
        }
        else if (EnemyUnitManager.Instance.totalSoldiers < targetTroopsResource && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
        {
            if (randomValue < 0.5f)
            {
                EnemyUnitManager.Instance.SpawnWarrior();
            }
            else if (randomValue > 0.5f && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnArcher();
            }
        }
    }

    private void HandleGuardPhase()
    {
        float randomValue = Random.Range(0f, 1f);
        if (EnemyUnitManager.Instance.totalSoldiers < 20)
        {
            if (EnemyUnitManager.Instance.workerCount <= targetWorkersGuard && randomValue < 0.1f && EnemyUnitManager.Instance.baseSpawnPoint != null && EnemyUnitManager.Instance.baseSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnWorker();
            }
            else if (randomValue < 0.5f && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnArcher();
            }
            else if (randomValue > 0.5f && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnWarrior();
            }
        }
    }

    private void HandleAttackPhase()
    {
        if (EnemyUnitManager.Instance.totalSoldiers < targetTroopsGuard && TotalGold >= 125)
        {
            float randomValue = Random.Range(0f, 1f);
            if (randomValue < 0.5f && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnArcher();
            }
            else if (randomValue > 0.5f && EnemyUnitManager.Instance.barracksSpawnPoint != null && EnemyUnitManager.Instance.barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                EnemyUnitManager.Instance.SpawnWarrior();
            }
        }
    }

    private void HandleCombatPhase()
    {
        // Combat phase logic
    }

    private void HandleEconomicPhase()
    {
        // Economic phase logic
    }

    public void AddGold(int amount)
    {
        TotalGold += amount;
        OnEnemyGoldChanged?.Invoke(TotalGold);
    }

    private void HandleAIPhases()
    {
        switch (CurrentState)
        {
            case GameState.ResourceGatheringPhase:
                if (targetWorkersResource <= EnemyUnitManager.Instance.totalUnits)
                {
                    EnemyUnitManager.Instance.AssignGuardTasks();
                    SetGameState(GameState.GuardPhase);
                }
                break;
            case GameState.GuardPhase:
                if (EnemyUnitManager.Instance.totalSoldiers >= Random.Range(13, 19))
                {
                    EnemyUnitManager.Instance.AssignAttackingTasks();
                    SetGameState(GameState.AttackPhase);
                }
                break;
            case GameState.AttackPhase:
                if (EnemyUnitManager.Instance.totalSoldiers <= 4)
                {
                    EnemyUnitManager.Instance.AssignGuardTasks();
                    SetGameState(GameState.GuardPhase);
                }
                break;
        }
    }

    private void HandleCurrentState()
    {
        switch (CurrentState)
        {
            case GameState.ResourceGatheringPhase:
                HandleResourceGatheringPhase();
                break;
            case GameState.GuardPhase:
                HandleGuardPhase();
                break;
            case GameState.AttackPhase:
                HandleAttackPhase();
                break;
            case GameState.CombatPhase:
                HandleCombatPhase();
                break;
            case GameState.EconomicPhase:
                HandleEconomicPhase();
                break;
        }
    }

    public void ResetGame()
    {
        TotalGold = 40;
        OnEnemyGoldChanged?.Invoke(TotalGold);
        SetGameState(GameState.ResourceGatheringPhase);
    }
}
