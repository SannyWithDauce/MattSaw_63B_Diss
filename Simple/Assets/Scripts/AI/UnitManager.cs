using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class UnitManager : MonoBehaviour
{
    public List<GameObject> workers;
    public List<GameObject> warriors;
    public List<GameObject> archers;
    public GameObject[] goldMines;

    public delegate void UnitChanged(int totalUnits, int maxUnits);
    public static event UnitChanged OnUnitChanged;

    public GameObject workerPrefab;
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public Transform baseSpawnPoint;
    public Transform barracksSpawnPoint;

    private static UnitManager _instance;

    public int maxUnits = 30;
    public int totalSoldiers = 0;
    public int totalUnits = 0;
    public int workerCount = 0;
    public int warriorCount = 0;
    public int archerCount = 0;

    private Queue<GameObject> workerQueue = new Queue<GameObject>();
    private Queue<(GameObject prefab, bool isWarrior)> barracksQueue = new Queue<(GameObject prefab, bool isWarrior)>();

    private bool isSpawningWorker = false;
    private bool isSpawningBarracksUnit = false;

    public static UnitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitManager>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        InitializeUnits();
        goldMines = GameObject.FindGameObjectsWithTag("Resource");
        UpdateUnitList(); // Initial update to populate the worker list
        AssignMiningTasks();

        // Test sequential spawning by calling the spawn methods in the Start method
        //StartCoroutine(TestSpawning());
    }

    private void UpdateUnitCount()
    {
        totalUnits = workers.Count + warriors.Count + archers.Count;
        OnUnitChanged?.Invoke(totalUnits, maxUnits);  // Trigger the event whenever unit count changes
    }

    private void InitializeUnits()
    {
        WorkerAgent[] workerAgents = FindObjectsOfType<WorkerAgent>();
        foreach (WorkerAgent worker in workerAgents)
        {
            if (worker != null)
            {
                workers.Add(worker.gameObject);
                workerCount++;
            }
        }

        WarriorAgent[] warriorAgents = FindObjectsOfType<WarriorAgent>();
        foreach (WarriorAgent warrior in warriorAgents)
        {
            if (warrior != null)
            {
                warriors.Add(warrior.gameObject);
                warriorCount++;
            }
        }

        ArcherAgent[] archerAgents = FindObjectsOfType<ArcherAgent>();
        foreach (ArcherAgent archer in archerAgents)
        {
            if (archer != null)
            {
                archers.Add(archer.gameObject);
                archerCount++;
            }
        }

        UpdateUnitCount();
    }

    public void UpdateUnitList()
    {
        workers.Clear();
        warriors.Clear();
        archers.Clear();

        var workerAgents = FindObjectsOfType<WorkerAgent>();
        var warriorAgents = FindObjectsOfType<WarriorAgent>();
        var archerAgents = FindObjectsOfType<ArcherAgent>();

        workers.AddRange(workerAgents.Where(agent => agent != null).Select(agent => agent.gameObject));
        warriors.AddRange(warriorAgents.Where(agent => agent != null).Select(agent => agent.gameObject));
        archers.AddRange(archerAgents.Where(agent => agent != null).Select(agent => agent.gameObject));

        UpdateUnitCount();
    }

    public void AssignAttackingTasks()
    {
        foreach (GameObject unit in warriors.Concat(archers))
        {
            if (unit != null)
            {
                GameObject nearestEnemy = FindNearestEnemy(unit.transform.position);
                if (nearestEnemy != null)
                {
                    WarriorAgent warriorAgent = unit.GetComponent<WarriorAgent>();
                    if (warriorAgent != null)
                    {
                        StartCoroutine(warriorAgent.AttackTarget(nearestEnemy));
                        // Debug.Log($"[AssignAttackingTasks] Warrior {warriorAgent.name} is moving to attack {nearestEnemy.name}"); // Log the assignment of an attack task to a warrior
                        continue;
                    }

                    ArcherAgent archerAgent = unit.GetComponent<ArcherAgent>();
                    if (archerAgent != null)
                    {
                        StartCoroutine(archerAgent.AttackTarget(nearestEnemy));
                        // Debug.Log($"[AssignAttackingTasks] Archer {archerAgent.name} is moving to attack {nearestEnemy.name}"); // Log the assignment of an attack task to an archer
                        continue;
                    }
                }
                else
                {
                    // Debug.Log($"[AssignAttackingTasks] No enemies found for {unit.name} to attack."); // Log if no enemies are found for a unit to attack
                }
            }
        }
    }

    public void AssignMiningTasks()
    {
        if (workers.Count == 0)
        {
            // Debug.LogWarning("No workers available to assign tasks."); // Warn if there are no workers available
            return;
        }

        foreach (GameObject worker in workers)
        {
            WorkerAgent agent = worker.GetComponent<WorkerAgent>();
            if (agent != null && !agent.isAssignedTask)
            {
                GameObject nearestMine = FindNearestGoldMine(worker.transform.position);
                if (nearestMine != null)
                {
                    agent.StartMining(nearestMine);
                    // Debug.Log($"[AssignMiningTasks] Worker {worker.name} assigned to mine {nearestMine.name}"); // Log the assignment of a mining task to a worker
                }
                else
                {
                    // Debug.LogWarning("No gold mine found for worker."); // Warn if no gold mine is found for the worker
                }
            }
        }
    }

    public void AssignGuardTasks()
    {
        float radius = 3f; // Set the guarding radius to 3f

        // Iterate through all warriors and assign them to guard the nearest gold mine
        foreach (GameObject warrior in warriors)
        {
            if (warrior != null)
            {
                AssignUnitToGuard(warrior, radius);
            }
        }

        // Iterate through all archers and assign them to guard the nearest gold mine
        foreach (GameObject archer in archers)
        {
            if (archer != null)
            {
                AssignUnitToGuard(archer, radius);
            }
        }
    }

    private void AssignUnitToGuard(GameObject unit, float radius)
    {
        if (unit != null)
        {
            // Find the nearest gold mine to the unit
            GameObject nearestMine = FindNearestGoldMine(unit.transform.position);
            if (nearestMine != null)
            {
                // Calculate the guard position around the gold mine
                Vector3 guardPosition = CalculateGuardPosition(nearestMine.transform.position, radius);
                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    // Set the unit's destination to the calculated guard position
                    agent.destination = guardPosition;
                    // Debug.Log($"{unit.name} is assigned to guard at {guardPosition}"); // Log the guard assignment for the unit
                }
            }
        }
    }

    private Vector3 CalculateGuardPosition(Vector3 minePosition, float radius)
    {
        // Generate a random angle
        float angle = Random.Range(0, 360);
        // Calculate the guard position using the angle and radius
        Vector3 guardPosition = minePosition + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
        return guardPosition;
    }

    private GameObject FindNearestGoldMine(Vector3 workerPosition)
    {
        GameObject nearestMine = null;
        float minDistance = float.MaxValue;

        foreach (GameObject mine in goldMines)
        {
            if (mine != null)
            {
                float distance = Vector3.Distance(workerPosition, mine.transform.position);
                if (distance < minDistance)
                {
                    nearestMine = mine;
                    minDistance = distance;
                }
            }
        }

        return nearestMine;
    }

    private GameObject FindNearestEnemy(Vector3 unitPosition)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("EnemyPrefabs");
        GameObject nearestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(unitPosition, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        if (nearestEnemy != null)
        {
            // Debug.Log($"[FindNearestEnemy] Nearest enemy found at {nearestEnemy.transform.position} with distance {minDistance}"); // Log the nearest enemy found
        }
        else
        {
            // Debug.Log("[FindNearestEnemy] No enemies found."); // Log if no enemies are found
        }

        return nearestEnemy;
    }

    public void AddWorker(GameObject worker)
    {
        if (!workers.Contains(worker))
        {
            workers.Add(worker);
        }
    }

    public void SpawnWorker()
    {
        if (GameManager.Instance.TotalGold >= 50 && totalUnits < maxUnits)  // Assuming it costs 50 gold to spawn a worker
        {
            GameManager.Instance.AddGold(-50); // Deduct gold immediately
            workerQueue.Enqueue(workerPrefab);
            if (!isSpawningWorker)
            {
                StartCoroutine(ProcessWorkerQueue());
            }
        }
        else
        {
            // Debug.Log("Not enough gold to spawn a worker or max unit size."); // Log if not enough gold or max units reached
        }
    }

    private IEnumerator ProcessWorkerQueue()
    {
        isSpawningWorker = true;
        while (workerQueue.Count > 0)
        {
            GameObject workerPrefab = workerQueue.Dequeue();
            yield return new WaitForSeconds(3f);

            GameObject newWorker = Instantiate(workerPrefab, baseSpawnPoint.position, Quaternion.identity);
            NavMeshAgent agent = newWorker.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }

            SelectionManager.Instance.AllSelectableObjects.Add(newWorker.GetComponent<SelectableObject>());
            AddWorker(newWorker);
            workerCount++;
            totalUnits++;

            UpdateUnitCount();
            UpdateUnitList();

            // Assign mining task to the new worker
            AssignMiningTasks();
        }
        isSpawningWorker = false;
    }

    public void AddBarracksUnit(GameObject unit, bool isWarrior)
    {
        if (isWarrior)
        {
            if (!warriors.Contains(unit))
            {
                warriors.Add(unit);
            }
        }
        else
        {
            if (!archers.Contains(unit))
            {
                archers.Add(unit);
            }
        }
    }

    public void SpawnWarrior()
    {
        if (GameManager.Instance.TotalGold >= 70 && totalUnits < maxUnits)
        {
            GameManager.Instance.AddGold(-100); // Deduct gold immediately
            barracksQueue.Enqueue((warriorPrefab, true));
            if (!isSpawningBarracksUnit)
            {
                StartCoroutine(ProcessBarracksQueue());
            }
        }
        else
        {
            // Debug.Log("Not enough gold to spawn a warrior.");
        }
    }

    public void SpawnArcher()
    {
        if (GameManager.Instance.TotalGold >= 125 && totalUnits < maxUnits)
        {
            GameManager.Instance.AddGold(-125); // Deduct gold immediately
            barracksQueue.Enqueue((archerPrefab, false));
            if (!isSpawningBarracksUnit)
            {
                StartCoroutine(ProcessBarracksQueue());
            }
        }
        else
        {
            // Debug.Log("Not enough gold to spawn an archer.");
        }
    }

    private IEnumerator ProcessBarracksQueue()
    {
        isSpawningBarracksUnit = true;
        while (barracksQueue.Count > 0)
        {
            var (unitPrefab, isWarrior) = barracksQueue.Dequeue();
            yield return new WaitForSeconds(4f);

            GameObject newUnit = Instantiate(unitPrefab, barracksSpawnPoint.position, Quaternion.identity);
            NavMeshAgent agent = newUnit.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }

            SelectionManager.Instance.AllSelectableObjects.Add(newUnit.GetComponent<SelectableObject>());
            AddBarracksUnit(newUnit, isWarrior);
            if (isWarrior)
            {
                warriorCount++;
            }
            else
            {
                archerCount++;
            }

            totalUnits++;
            totalSoldiers++;

            UpdateUnitCount();
            UpdateUnitList();

            // Assign guard task to the new unit
            AssignUnitToGuard(newUnit, 3f);

        }
        isSpawningBarracksUnit = false;
    }

    public void RemoveWarrior(GameObject warrior)
    {
        if (warriors.Contains(warrior))
        {
            warriors.Remove(warrior);
            Destroy(warrior); // Destroy the GameObject
            warriorCount--; // Decrement the warrior count
            totalUnits--; // Decrement the total units count
            totalSoldiers--; // Decrement the total soldiers count
            UpdateUnitCount(); // Update the UI or any other relevant components
        }
    }

    public void RemoveArcher(GameObject archer)
    {
        if (archers.Contains(archer))
        {
            archers.Remove(archer);
            Destroy(archer); // Destroy the GameObject
            archerCount--; // Decrement the warrior count
            totalUnits--; // Decrement the total units count
            totalSoldiers--; // Decrement the total soldiers count
            UpdateUnitCount(); // Update the UI or any other relevant components
        }
    }

    public void RemoveWorker(GameObject worker)
    {
        if (workers.Contains(worker))
        {
            workers.Remove(worker);
            Destroy(worker); // Destroy the GameObject
            workerCount--; // Decrement the warrior count
            totalUnits--; // Decrement the total units count // Decrement the total soldiers count
            UpdateUnitCount(); // Update the UI or any other relevant components
        }
    }
}
