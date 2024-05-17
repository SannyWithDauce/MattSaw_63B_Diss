using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyUnitManager : MonoBehaviour
{
    public static EnemyUnitManager Instance { get; set; }

    public List<GameObject> enemyWorkers = new List<GameObject>();
    public List<GameObject> enemyArchers = new List<GameObject>();
    public List<GameObject> enemyWarriors = new List<GameObject>();
    public GameObject[] goldMines;

    public GameObject workerPrefab;
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public GameObject basePrefab;
    public GameObject barracksPrefab;
    public Transform baseSpawnPoint;
    public Transform barracksSpawnPoint;

    public delegate void EnemyUnitDestroyed(string unitType);
    public static event EnemyUnitDestroyed OnEnemyUnitDestroyed;

    public int totalUnits = 0;
    public int maxUnits = 30;
    public int totalSoldiers = 0;
    public int workerCount = 0;
    public int warriorCount = 0;
    public int archerCount = 0;

    private Queue<GameObject> workerQueue = new Queue<GameObject>();
    private Queue<(GameObject prefab, bool isWarrior)> barracksQueue = new Queue<(GameObject prefab, bool isWarrior)>();

    private bool isSpawningWorker = false;
    private bool isSpawningBarracksUnit = false;

    public delegate void EnemyUnitChanged(int totalUnits, int maxUnits);
    public static event EnemyUnitChanged OnEnemyUnitChanged;

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
        InitializeUnits();
        EnsureMinimumWorkers(); // Ensure at least 2 workers at the start
        goldMines = GameObject.FindGameObjectsWithTag("EnemyResource");
        UpdateUnitList();
        AssignMiningTasks();
    }

    private void UpdateUnitCount()
    {
        totalUnits = enemyWorkers.Count + enemyArchers.Count + enemyWarriors.Count;
        OnEnemyUnitChanged?.Invoke(totalUnits, maxUnits);
    }

    private void InitializeUnits()
    {
        EnemyWorkerAgent[] workerAgents = FindObjectsOfType<EnemyWorkerAgent>();
        foreach (EnemyWorkerAgent worker in workerAgents)
        {
            enemyWorkers.Add(worker.gameObject);
            workerCount++;
        }

        EnemyWarriorAgent[] warriorAgents = FindObjectsOfType<EnemyWarriorAgent>();
        foreach (EnemyWarriorAgent warrior in warriorAgents)
        {
            enemyWarriors.Add(warrior.gameObject);
            warriorCount++;
        }

        EnemyArcherAgent[] archerAgents = FindObjectsOfType<EnemyArcherAgent>();
        foreach (EnemyArcherAgent archer in archerAgents)
        {
            enemyArchers.Add(archer.gameObject);
            archerCount++;
        }

        UpdateUnitCount();
    }

    public void UpdateUnitList()
    {
        enemyWorkers.Clear();
        enemyWarriors.Clear();
        enemyArchers.Clear();

        var workers = FindObjectsOfType<EnemyWorkerAgent>();
        var warriors = FindObjectsOfType<EnemyWarriorAgent>();
        var archers = FindObjectsOfType<EnemyArcherAgent>();

        enemyWorkers.AddRange(workers.Where(agent => agent != null).Select(agent => agent.gameObject));
        enemyWarriors.AddRange(warriors.Where(agent => agent != null).Select(agent => agent.gameObject));
        enemyArchers.AddRange(archers.Where(agent => agent != null).Select(agent => agent.gameObject));

        UpdateUnitCount();
    }

    private void EnsureMinimumWorkers()
    {
        while (enemyWorkers.Count < 2)
        {
            GameObject newWorker = Instantiate(workerPrefab, baseSpawnPoint.position, Quaternion.identity);
            NavMeshAgent agent = newWorker.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }
            AddEnemyWorker(newWorker);
            workerCount++;
            totalUnits++;
        }
        UpdateUnitCount();
        AssignMiningTasks();
    }

    public void ResetUnits()
    {
        StopAllSpawning();

        if (enemyWorkers.Count > 2)
        {
            for (int i = enemyWorkers.Count - 1; i >= 2; i--)
            {
                Destroy(enemyWorkers[i]);
            }
            enemyWorkers.RemoveRange(2, enemyWorkers.Count - 2);
        }

        foreach (GameObject warrior in enemyWarriors)
        {
            Destroy(warrior);
        }
        enemyWarriors.Clear();

        foreach (GameObject archer in enemyArchers)
        {
            Destroy(archer);
        }
        enemyArchers.Clear();

        // Ensure at least two workers are present

        if (enemyWorkers.Count >= 2)
        {
            enemyWorkers[0].transform.position = baseSpawnPoint.position;
            enemyWorkers[1].transform.position = baseSpawnPoint.position;
        }

        workerCount = enemyWorkers.Count;
        warriorCount = 0;
        archerCount = 0;
        totalUnits = enemyWorkers.Count;
        totalSoldiers = 0;

        EnsureMinimumWorkers();

        UpdateUnitCount();
        AssignMiningTasks();

        StartCoroutine(ResetWarriorsAndArchersAfterDelay());
    }

    private IEnumerator ResetWarriorsAndArchersAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        // Reset warriors and archers again
        foreach (GameObject warrior in enemyWarriors)
        {
            Destroy(warrior);
        }
        enemyWarriors.Clear();

        foreach (GameObject archer in enemyArchers)
        {
            Destroy(archer);
        }
        enemyArchers.Clear();

        warriorCount = 0;
        archerCount = 0;
        totalSoldiers = 0;

        UpdateUnitCount();
    }

    public void AssignMiningTasks()
    {
        if (enemyWorkers.Count == 0)
        {
            return;
        }

        foreach (GameObject worker in enemyWorkers)
        {
            EnemyWorkerAgent agent = worker.GetComponent<EnemyWorkerAgent>();
            if (agent != null && !agent.isAssignedTask)
            {
                GameObject nearestMine = FindNearestGoldMine(worker.transform.position);
                if (nearestMine != null)
                {
                    agent.StartMining(nearestMine);
                }
            }
        }
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

    public void StopAllSpawning()
    {
        StopCoroutine("ProcessWorkerQueue");

    }

    public void StopAllSpawningBarracks()
    {

        StopCoroutine("ProcessBarracksQueue");
    }

    public void AssignAttackingTasks()
    {
        foreach (GameObject unit in enemyWarriors.Concat(enemyArchers))
        {
            if (unit != null)
            {
                GameObject nearestEnemy = FindNearestEnemy(unit.transform.position);
                if (nearestEnemy != null)
                {
                    EnemyWarriorAgent warriorAgent = unit.GetComponent<EnemyWarriorAgent>();
                    if (warriorAgent != null)
                    {
                        if (warriorAgent.currentState != EnemyWarriorAgent.State.Attacking)
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

                    EnemyArcherAgent archerAgent = unit.GetComponent<EnemyArcherAgent>();
                    if (archerAgent != null)
                    {
                        if (archerAgent.currentState != EnemyArcherAgent.State.Attacking)
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

    public void SpawnWorker()
    {
        if (baseSpawnPoint == null)
        {
            Debug.Log("Spawn point is null. Cannot spawn worker.");
            return;
        }
        else if (EnemyGameManager.Instance.TotalGold >= 50 && totalUnits < maxUnits)
        {
            EnemyGameManager.Instance.AddGold(-50);
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
            Debug.Log("Base spawn point is destroyed or inactive at the start of ProcessWorkerQueue.");
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        isSpawningWorker = true;

        while (workerQueue.Count > 0)
        {
            if (baseSpawnPoint == null || !baseSpawnPoint.gameObject.activeInHierarchy)
            {
                Debug.Log("Base spawn point has been destroyed or is inactive. Stopping worker spawn process.");
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

                AddEnemyWorker(newWorker);
                workerCount++;
                totalUnits++;

                UpdateUnitCount();
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

    public void AddEnemyWorker(GameObject worker)
    {
        if (!enemyWorkers.Contains(worker))
        {
            enemyWorkers.Add(worker);
        }
    }

    public void SpawnWarrior()
    {
        if (barracksSpawnPoint == null)
        {
            Debug.Log("Spawn point is null. Cannot spawn warrior.");
            return;
        }
        else if (EnemyGameManager.Instance.TotalGold >= 100 && totalUnits < maxUnits)
        {
            EnemyGameManager.Instance.AddGold(-100);
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
            Debug.Log("Spawn point is null. Cannot spawn archer.");
            return;
        }
        else if (EnemyGameManager.Instance.TotalGold >= 125 && totalUnits < maxUnits)
        {
            EnemyGameManager.Instance.AddGold(-125);
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
            Debug.Log("Barracks spawn point is destroyed or inactive at the start of ProcessBarracksQueue.");
            isSpawningBarracksUnit = false; // Set flag to false to stop coroutine execution
            yield break;
        }

        yield return new WaitForSeconds(1f);
        isSpawningBarracksUnit = true;

        while (barracksQueue.Count > 0)
        {
            if (barracksSpawnPoint == null || !barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                isSpawningBarracksUnit = false; // Set flag to false to stop coroutine execution
                yield break;
            }

            var (unitPrefab, isWarrior) = barracksQueue.Dequeue();
            yield return new WaitForSeconds(4f);

            if (barracksSpawnPoint == null || !barracksSpawnPoint.gameObject.activeInHierarchy)
            {
                isSpawningBarracksUnit = false; // Set flag to false to stop coroutine execution
                yield break; // Exit coroutine
            }

            if (totalUnits < maxUnits)
            {
                GameObject newUnit = Instantiate(unitPrefab, barracksSpawnPoint.position, Quaternion.identity);
                NavMeshAgent agent = newUnit.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = true;
                }

                if (isWarrior)
                {
                    enemyWarriors.Add(newUnit);
                    warriorCount++;
                }
                else
                {
                    enemyArchers.Add(newUnit);
                    archerCount++;
                }

                totalUnits++;
                totalSoldiers++;

                UpdateUnitCount();
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
        if (enemyWarriors.Contains(warrior))
        {
            EnemyWarriorAgent warriorAgent = warrior.GetComponent<EnemyWarriorAgent>();
            if (warriorAgent != null && warriorAgent.attackCoroutine != null)
            {
                warriorAgent.StopCoroutine(warriorAgent.attackCoroutine);
                warriorAgent.attackCoroutine = null;
            }

            enemyWarriors.Remove(warrior);
            Destroy(warrior);
            warriorCount--;
            totalUnits--;
            totalSoldiers--;
            OnEnemyUnitDestroyed?.Invoke("Warrior");
            UpdateUnitCount();
        }
    }

    public void RemoveArcher(GameObject archer)
    {
        if (enemyArchers.Contains(archer))
        {
            EnemyArcherAgent archerAgent = archer.GetComponent<EnemyArcherAgent>();
            if (archerAgent != null && archerAgent.attackCoroutine != null)
            {
                archerAgent.StopCoroutine(archerAgent.attackCoroutine);
                archerAgent.attackCoroutine = null;
            }

            enemyArchers.Remove(archer);
            Destroy(archer);
            archerCount--;
            totalUnits--;
            totalSoldiers--;
            OnEnemyUnitDestroyed?.Invoke("Archer");
            UpdateUnitCount();
        }
    }

    public void RemoveWorker(GameObject worker)
    {
        if (enemyWorkers.Contains(worker))
        {
            enemyWorkers.Remove(worker);
            Destroy(worker);
            workerCount--;
            totalUnits--;
            OnEnemyUnitDestroyed?.Invoke("Worker");
            UpdateUnitCount();
        }
    }

    public void OnBaseDestroyed(EnemyBase destroyedBase)
    {
        StopAllSpawning();
        foreach (GameObject worker in enemyWorkers)
        {
            EnemyWorkerAgent workerAgent = worker.GetComponent<EnemyWorkerAgent>();
            if (workerAgent != null && workerAgent.enemyBase == destroyedBase)
            {
                workerAgent.StopMining();
            }
        }
    }

    public void OnBarracksDestroyed(EnemyBarracks destroyedBarracks)
    {
        StopAllSpawningBarracks();
    }

    public void AssignGuardTasks()
    {
        float radius = 3f;

        foreach (GameObject warrior in enemyWarriors)
        {
            if (warrior != null)
            {
                AssignUnitToGuard(warrior, radius);
            }
        }

        foreach (GameObject archer in enemyArchers)
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

    private GameObject FindNearestEnemy(Vector3 unitPosition)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("PlayerPrefabs");
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

        return nearestEnemy;
    }
}
