using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class UnitMLManager : MonoBehaviour
{
    public List<GameObject> workers;
    public List<GameObject> warriors;
    public List<GameObject> archers;
    public GameObject[] goldMines;

    public GameObject workerPrefab;
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public GameObject basePrefab;
    public GameObject barracksPrefab;
    public Transform baseSpawnPoint;
    public Transform barracksSpawnPoint;

    public bool canSpawnWorkers = true;
    public bool canSpawnBarracks = true;

    public delegate void UnitChanged(int totalUnits, int maxUnits);
    public static event UnitChanged OnUnitChanged;

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

    private static UnitMLManager _instance;

    public static UnitMLManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitMLManager>();
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
        EnsureMinimumWorkers(); // Ensure at least 2 workers at the start
        goldMines = GameObject.FindGameObjectsWithTag("Resource");
        UpdateUnitList();
        AssignMiningTasks();
    }

    private void UpdateUnitCount()
    {
        totalUnits = workers.Count + warriors.Count + archers.Count;
        OnUnitChanged?.Invoke(totalUnits, maxUnits);
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

    private void EnsureMinimumWorkers()
    {
        while (workers.Count < 2)
        {
            GameObject newWorker = Instantiate(workerPrefab, baseSpawnPoint.position, Quaternion.identity);
            NavMeshAgent agent = newWorker.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }
            workers.Add(newWorker);
            workerCount++;
            totalUnits++;
        }
        UpdateUnitCount();
        AssignMiningTasks();
    }


    public void ResetUnits()
    {
        StopAllSpawning(); // Stop any spawning that was happening

        // Destroy all units except the first two workers
        if (workers.Count > 2)
        {
            for (int i = workers.Count - 1; i >= 2; i--)
            {
                Destroy(workers[i]);
            }
            workers.RemoveRange(2, workers.Count - 2);
        }

        foreach (GameObject warrior in warriors)
        {
            Destroy(warrior);
        }
        warriors.Clear();

        foreach (GameObject archer in archers)
        {
            Destroy(archer);
        }
        archers.Clear();

        // Ensure at least two workers are present
        while (workers.Count < 2)
        {
            GameObject newWorker = Instantiate(workerPrefab, baseSpawnPoint.position, Quaternion.identity);
            NavMeshAgent agent = newWorker.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }
            workers.Add(newWorker);
            workerCount++;
            totalUnits++;
        }

        // Reset the starting two workers' positions
        if (workers.Count >= 2)
        {
            workers[0].transform.position = baseSpawnPoint.position;
            workers[1].transform.position = baseSpawnPoint.position;
        }

        // Reset counts
        workerCount = workers.Count;
        warriorCount = 0;
        archerCount = 0;
        totalUnits = workers.Count;
        totalSoldiers = 0;

        UpdateUnitCount();
        AssignMiningTasks(); // Assign tasks to the workers

        StartCoroutine(ResetWarriorsAndArchersAfterDelay());
    }

    private IEnumerator ResetWarriorsAndArchersAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        // Reset warriors and archers again
        foreach (GameObject warrior in warriors)
        {
            Destroy(warrior);
        }
        warriors.Clear();

        foreach (GameObject archer in archers)
        {
            Destroy(archer);
        }
        archers.Clear();

        warriorCount = 0;
        archerCount = 0;
        totalSoldiers = 0;

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
                        if (warriorAgent.currentState != WarriorAgent.State.Attacking)
                        {
                            if (warriorAgent.attackCoroutine != null)
                            {
                                warriorAgent.StopCoroutine(warriorAgent.attackCoroutine);
                            }
                            warriorAgent.attackCoroutine = warriorAgent.AttackTarget(nearestEnemy);
                            warriorAgent.StartCoroutine(warriorAgent.attackCoroutine);
                        }
                        continue;
                    }

                    ArcherAgent archerAgent = unit.GetComponent<ArcherAgent>();
                    if (archerAgent != null)
                    {
                        if (archerAgent.currentState != ArcherAgent.State.Attacking)
                        {
                            if (archerAgent.attackCoroutine != null)
                            {
                                archerAgent.StopCoroutine(archerAgent.attackCoroutine);
                            }
                            archerAgent.attackCoroutine = archerAgent.AttackTarget(nearestEnemy);
                            archerAgent.StartCoroutine(archerAgent.attackCoroutine);
                        }
                        continue;
                    }
                }
            }
        }
    }

    public void AssignMiningTasks()
    {
        if (workers.Count == 0)
        {
            return;
        }

        foreach (GameObject worker in workers)
        {
            WorkerAgent agent = worker.GetComponent<WorkerAgent>();
            if (agent != null && !agent.isAssignedTask)
            {
                if (agent.playerBase == null || !agent.playerBase.IsAlive)
                {
                    agent.StopMining();
                    continue;
                }

                if (baseSpawnPoint != null)
                {
                    GameObject nearestMine = FindNearestGoldMine(worker.transform.position);
                    if (nearestMine != null)
                    {
                        agent.StartMining(nearestMine);
                    }
                }
                else
                {
                    Debug.LogError("Base spawn point is null. Cannot assign mining tasks.");
                }
            }
        }
    }

    public void AssignGuardTasks()
    {
        float radius = 3f;

        foreach (GameObject warrior in warriors)
        {
            if (warrior != null)
            {
                AssignUnitToGuard(warrior, radius);
            }
        }

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
            GameObject nearestMine = FindNearestGoldMine(unit.transform.position);
            if (nearestMine != null)
            {
                Vector3 guardPosition = CalculateGuardPosition(nearestMine.transform.position, radius);
                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.destination = guardPosition;
                }
            }
        }
    }

    private Vector3 CalculateGuardPosition(Vector3 minePosition, float radius)
    {
        float angle = Random.Range(0, 360);
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
            if (enemy != null && enemy.transform != null)
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
            Debug.Log($"[FindNearestEnemy] Nearest enemy found at {nearestEnemy.transform.position} with distance {minDistance}");
        }
        else
        {
            Debug.Log("[FindNearestEnemy] No enemies found.");
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

    public void StopAllSpawning()
    {
        canSpawnWorkers = false;
        StopCoroutine("ProcessWorkerQueue");
        StopCoroutine("ProcessBarracksQueue");
    }

    public void StopAllSpawningBarracks()
    {
        canSpawnBarracks = false;
        StopCoroutine("ProcessBarracksQueue");
    }

    public void SpawnWorker()
    {
        if (baseSpawnPoint == null || baseSpawnPoint.gameObject.activeInHierarchy == false)
        {
            Debug.LogError("Spawn point is destroyed. Cannot spawn worker.");
            return;
        }
        else if (GoldMLManager.Instance.TotalGold >= 50 && totalUnits < maxUnits)
        {
            GoldMLManager.Instance.AddGold(-50);
            workerQueue.Enqueue(workerPrefab);
            if (!isSpawningWorker)
            {
                StartCoroutine(ProcessWorkerQueue());
            }
        }
        else
        {
            Debug.Log("Not enough gold or max units reached. Worker not spawned.");
        }
    }

    private IEnumerator ProcessWorkerQueue()
    {
        if (baseSpawnPoint == null || !baseSpawnPoint.gameObject.activeInHierarchy)
        {
            Debug.LogError("Base spawn point is destroyed or inactive at the start of ProcessWorkerQueue.");
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        isSpawningWorker = true;

        while (workerQueue.Count > 0)
        {
            if (baseSpawnPoint == null || !baseSpawnPoint.gameObject.activeInHierarchy)
            {
                Debug.LogError("Base spawn point has been destroyed or is inactive. Stopping worker spawn process.");
                yield return null;
            }

            GameObject workerPrefab = workerQueue.Dequeue();
            yield return new WaitForSeconds(3f);

            if (baseSpawnPoint == null || !baseSpawnPoint.gameObject.activeInHierarchy)
            {
                yield return null;
                yield break;
            }

            if (totalUnits < maxUnits)
            {
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

                AssignMiningTasks();
            }
            else
            {
                Debug.Log("Max units reached during worker spawn. Worker not spawned.");
                break;
            }
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
        if (barracksSpawnPoint == null)
        {
            Debug.LogError("Base spawn point is null. Cannot spawn warrior.");
            return;
        }

        if (GoldMLManager.Instance.TotalGold >= 100 && totalUnits < maxUnits)
        {
            GoldMLManager.Instance.AddGold(-100);
            barracksQueue.Enqueue((warriorPrefab, true));
            if (!isSpawningBarracksUnit)
            {
                StartCoroutine(ProcessBarracksQueue());
            }
        }
        else
        {
            Debug.Log("Not enough gold or max units reached. Warrior not spawned.");
        }
    }

    public void SpawnArcher()
    {
        if (barracksSpawnPoint == null)
        {
            Debug.LogError("Base spawn point is null. Cannot spawn archer.");
            return;
        }

        if (GoldMLManager.Instance.TotalGold >= 100 && totalUnits < maxUnits)
        {
            GoldMLManager.Instance.AddGold(-100);
            barracksQueue.Enqueue((archerPrefab, false));
            if (!isSpawningBarracksUnit)
            {
                StartCoroutine(ProcessBarracksQueue());
            }
        }
        else
        {
            Debug.Log("Not enough gold or max units reached. Archer not spawned.");
        }
    }

    private IEnumerator ProcessBarracksQueue()
    {
        if (barracksSpawnPoint == null || !barracksSpawnPoint.gameObject.activeInHierarchy)
        {
            Debug.LogError("Barracks spawn point is destroyed or inactive at the start of ProcessBarracksQueue.");
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        isSpawningBarracksUnit = true;

        while (barracksQueue.Count > 0)
        {
            if (barracksSpawnPoint == null || !barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                Debug.LogError("Barracks spawn point has been destroyed or is inactive. Stopping barracks unit spawn process.");
                yield return null;
            }

            var (unitPrefab, isWarrior) = barracksQueue.Dequeue();
            yield return new WaitForSeconds(4f);

            if (barracksSpawnPoint == null || !barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                yield return null;
                yield break;
            }

            if (totalUnits < maxUnits)
            {
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

                AssignUnitToGuard(newUnit, 3f);
            }
            else
            {
                Debug.Log("Max units reached during barracks unit spawn. Unit not spawned.");
                break;
            }
        }
        isSpawningBarracksUnit = false;
    }

    public void RemoveWarrior(GameObject warrior)
    {
        if (warriors.Contains(warrior))
        {
            WarriorAgent warriorAgent = warrior.GetComponent<WarriorAgent>();
            if (warriorAgent != null && warriorAgent.attackCoroutine != null)
            {
                warriorAgent.StopCoroutine(warriorAgent.attackCoroutine);
                warriorAgent.attackCoroutine = null;
            }

            warriors.Remove(warrior);
            Destroy(warrior);
            warriorCount--;
            totalUnits--;
            totalSoldiers--;
            UpdateUnitCount();
        }
    }

    public void RemoveArcher(GameObject archer)
    {
        if (archers.Contains(archer))
        {
            ArcherAgent archerAgent = archer.GetComponent<ArcherAgent>();
            if (archerAgent != null && archerAgent.attackCoroutine != null)
            {
                archerAgent.StopCoroutine(archerAgent.attackCoroutine);
                archerAgent.attackCoroutine = null;
            }

            archers.Remove(archer);
            Destroy(archer);
            archerCount--;
            totalUnits--;
            totalSoldiers--;
            UpdateUnitCount();
        }
    }

    public void RemoveWorker(GameObject worker)
    {
        if (workers.Contains(worker))
        {
            workers.Remove(worker);
            Destroy(worker);
            workerCount--;
            totalUnits--;
            UpdateUnitCount();
        }
    }

    public void OnBaseDestroyed(Base destroyedBase)
    {
        StopAllSpawning();
        foreach (GameObject worker in workers)
        {
            WorkerAgent workerAgent = worker.GetComponent<WorkerAgent>();
            if (workerAgent != null && workerAgent.playerBase == destroyedBase)
            {
                workerAgent.StopMining();
            }
        }
    }

    public void OnBarracksDestroyed(Barracks destroyedBarracks)
    {
        StopAllSpawningBarracks();
    }
}
