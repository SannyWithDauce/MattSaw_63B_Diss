using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }
    public int TotalGold { get; set; }
    public GameState CurrentState { get; set; }

    public int targetWorkersResource = 7;
    public int targetTroopsResource = 5;
    public int targetWorkersGuard = 10;
    public int targetTroopsGuard = 20;
    public int targetGold = 400;

    public delegate void GoldChanged(int goldAmount);
    public static event GoldChanged OnGoldChanged;

    public enum GameState
    {
        ResourceGatheringPhase,
        GuardPhase,
        AttackPhase,
        EconomicPhase,
        CombatPhase
    }

    private void Awake()
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
        TotalGold = 300;
    }

    void Update()
    {
        //Handle AI phase transitions here if needed
        HandleCurrentState();
        HandleAIPhases();
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChange(newState);
        Debug.Log("Game State changed to: " + CurrentState);
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
        // Logic for resource gathering: Prioritize worker and resource tasks
        //Debug.Log("Entered Resource Gathering Phase: Prioritize workers and resource collection.");
        //UnitManager.Instance.AssignMiningTasks();

        if (UnitManager.Instance.workerCount < targetWorkersResource)
        {
            UnitManager.Instance.SpawnWorker();
        }
        else if (UnitManager.Instance.totalSoldiers < targetTroopsResource && TotalGold >= 125) // Ensure there's enough gold to consider spawning other units
        {
            if (Random.value > 0.5f)
            {
                UnitManager.Instance.SpawnWarrior();
            }
            else
            {
                UnitManager.Instance.SpawnArcher();
            }
        }
    }

    private void HandleGuardPhase()
    {
        // Logic for guarding resources: Assign units to guard resources
        //Debug.Log("Entered Guard Phase: Assign units to guard resources.");
        //UnitManager.Instance.AssignGuardTasks();
        if (UnitManager.Instance.totalSoldiers < 20 && TotalGold >= 125)
        {
            float randomValue = Random.Range(0, 1);
            if (UnitManager.Instance.workerCount <= targetWorkersGuard && randomValue < 0.2f)
            {
                UnitManager.Instance.SpawnWorker();
            }
            else if (randomValue < 0.6f && randomValue > 0.2f)
            {
                UnitManager.Instance.SpawnArcher();

            }
            else
            {
                UnitManager.Instance.SpawnWarrior();
            }
        }

    }

    private void HandleAttackPhase()
    {
        if (UnitManager.Instance.totalSoldiers < targetTroopsGuard && TotalGold >= 125)
        {
            float randomValue = Random.Range(0, 1);
            if (randomValue < 0.5f)
            {
                UnitManager.Instance.SpawnArcher();
            }
            else
            {
                UnitManager.Instance.SpawnWarrior();
            }
        }
    }

    private void HandleCombatPhase()
    {
        // Logic for combat phase: Engage enemies, defend base, etc.
        Debug.Log("Entered Combat Phase: Prepare defenses and engage the enemy.");
        // Possible triggers or checks for transitioning out of this phase
    }

    private void HandleEconomicPhase()
    {
        // Logic for economic management: Enhance resource gathering, trade, etc.
        Debug.Log("Entered Economic Phase: Focus on economic growth and resource management.");
        // Possible triggers or checks for transitioning out of this phase
    }

    public void AddGold(int amount)
    {
        TotalGold += amount;
        OnGoldChanged?.Invoke(TotalGold);
        //Debug.Log($"Total gold updated: {TotalGold}");
    }

    private void HandleAIPhases()
    {
        switch (CurrentState)
        {
            case GameState.ResourceGatheringPhase:
                if (TotalGold > targetGold || targetTroopsResource <= UnitManager.Instance.totalSoldiers)
                {
                    UnitManager.Instance.AssignGuardTasks();
                    Debug.LogWarning("IN GUARD PHASE");
                    SetGameState(GameState.GuardPhase);
                }
                break;
            case GameState.GuardPhase:
                if (UnitManager.Instance.totalSoldiers >= Random.Range(15, 20))
                {
                    UnitManager.Instance.AssignAttackingTasks();
                    Debug.LogWarning("IN ATTACK PHASE");
                    SetGameState(GameState.AttackPhase);
                }
                break;
            case GameState.AttackPhase:
                if (UnitManager.Instance.totalSoldiers <= 4)
                {
                    UnitManager.Instance.AssignGuardTasks();
                    Debug.LogWarning("IN GUARD PHASE");
                    SetGameState(GameState.GuardPhase);
                    UnitManager.Instance.AssignGuardTasks();
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
}
