using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyWorkerAgent : MonoBehaviour, IDamageable
{
    public NavMeshAgent navMeshAgent;
    public GameObject unitStatDisplay;
    public EnemyBase enemyBase;
    public Image healthBarAmount;
    public float currentHealth;
    private Coroutine miningCoroutine;
    public bool isAssignedTask = false;

    [Header("Worker Stats")]
    public float health = 100f;
    public int capacity = 10;
    public float attackSpeed = 1f;
    public float attackRange = 1.0f;  // Updated to be consistent with mining range
    public int carriedGold = 0;

    void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }

    void Start()
    {
        navMeshAgent.enabled = true;
        currentHealth = health;

        if (navMeshAgent == null)
        {
            Debug.Log("NavMeshAgent component not found on the worker, disabling script.");
            this.enabled = false;
        }

        enemyBase = FindObjectOfType<EnemyBase>();
        if (enemyBase == null)
        {
            Debug.Log("EnemyBase not found in the scene.");
            this.enabled = false;
        }

        EnemyUnitManager.Instance.AddEnemyWorker(gameObject);
    }

    void Update()
    {
        HandleHealth();
    }

    public void StartMining(GameObject goldMine)
    {
        if (goldMine == null)
        {
            Debug.Log("Attempted to mine at a null gold mine location.");
            return;
        }

        isAssignedTask = true;
        MoveToLocation(goldMine.transform.position);
        miningCoroutine = StartCoroutine(MineGold(goldMine));
    }

    private IEnumerator MineGold(GameObject goldMine)
    {
        Vector3 minePosition = goldMine.transform.position;
        while (isAssignedTask)
        {
            if (goldMine == null || enemyBase == null) // Check if either goldMine or enemyBase is null
            {
                StopMining();
                yield break;
            }

            if (Vector3.Distance(transform.position, minePosition) > attackRange)
            {
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                    MoveToLocation(minePosition);
                }
            }

            yield return new WaitUntil(() => goldMine == null || enemyBase == null || Vector3.Distance(transform.position, minePosition) <= attackRange);

            if (goldMine == null || enemyBase == null) // Check if either goldMine or enemyBase is null
            {
                StopMining();
                yield break;
            }

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
            }

            while (carriedGold < capacity && Vector3.Distance(transform.position, minePosition) <= attackRange)
            {
                carriedGold++;
                yield return new WaitForSeconds(0.2f / attackSpeed);

                if (carriedGold >= capacity)
                {
                    if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                    {
                        navMeshAgent.isStopped = false;
                        MoveToLocation(enemyBase.transform.position);
                    }

                    yield return new WaitUntil(() => enemyBase == null || Vector3.Distance(transform.position, enemyBase.transform.position) <= attackRange);

                    if (enemyBase != null)
                    {
                        enemyBase.DepositGold(carriedGold);
                        carriedGold = 0;
                    }
                    else
                    {
                        Debug.LogError("EnemyBase reference not set on EnemyWorkerAgent.");
                    }

                    break;
                }
            }

            if (carriedGold == capacity)
            {
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                    MoveToLocation(enemyBase.transform.position);
                }

                yield return new WaitUntil(() => enemyBase == null || Vector3.Distance(transform.position, enemyBase.transform.position) <= attackRange);

                if (enemyBase != null)
                {
                    enemyBase.DepositGold(carriedGold);
                    carriedGold = 0;
                }
                else
                {
                    Debug.LogError("EnemyBase reference not set on EnemyWorkerAgent.");
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        isAssignedTask = false;
    }


    public void StopMining()
    {
        Debug.Log("Stopped mining.");
        isAssignedTask = false;
        if (miningCoroutine != null)
        {
            StopCoroutine(miningCoroutine);
            miningCoroutine = null;
        }
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = false;
        }
    }


    private void DepositGold()
    {
        if (enemyBase != null)
        {
            enemyBase.DepositGold(carriedGold);
            carriedGold = 0;
        }
        else
        {
            Debug.Log("EnemyBase reference not set on EnemyWorkerAgent.");
        }
    }

    public void MoveToLocation(Vector3 targetPosition)
    {
        StartCoroutine(MoveToLocationCoroutine(targetPosition));
    }

    private IEnumerator MoveToLocationCoroutine(Vector3 targetPosition)
    {
        if (this == null || navMeshAgent == null)
        {
            Debug.Log("Agent or NavMeshAgent is null.");
            yield break;
        }

        while (!navMeshAgent.isOnNavMesh)
        {
            NavMeshHit navMeshHit;
            if (NavMesh.SamplePosition(transform.position, out navMeshHit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = navMeshHit.position;
                navMeshAgent.Warp(navMeshHit.position); // Place the agent on the NavMesh
                yield return new WaitForSeconds(0.1f); // Small delay to ensure agent is placed on NavMesh
            }
            else
            {
                Debug.Log("NavMeshAgent is not on the NavMesh and failed to find a valid position.");
                yield break;
            }
        }

        if (navMeshAgent.isOnNavMesh && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(targetPosition);
            Debug.Log($"Moving {gameObject.name} to {targetPosition} after ensuring on NavMesh");
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    private void HandleHealth()
    {
        if (unitStatDisplay == null)
        {
            return;
        }

        unitStatDisplay.transform.LookAt(unitStatDisplay.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        healthBarAmount.fillAmount = currentHealth / health;
    }

    private void Die()
    {
        if (miningCoroutine != null)
        {
            StopCoroutine(miningCoroutine);
            miningCoroutine = null;
        }

        EnemyUnitManager.Instance.RemoveWorker(gameObject);

        // Ensure no further code executes by deactivating the gameObject first
        gameObject.SetActive(false);

        Destroy(gameObject);

        if (this == null)
        {
            return;
        }
    }
}
