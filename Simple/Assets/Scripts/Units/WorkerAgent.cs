using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class WorkerAgent : MonoBehaviour, IDamageable
{
    public NavMeshAgent navMeshAgent;
    public Base playerBase;
    public SelectableObject selectableObject;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float currentHealth;

    // Change 1: Modified Coroutine to IEnumerator
    public IEnumerator miningCoroutine;

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
        navMeshAgent = GetComponent<NavMeshAgent>();
        selectableObject = GetComponent<SelectableObject>();
        navMeshAgent.enabled = true;
        currentHealth = health;

        if (navMeshAgent == null)
        {
            Debug.Log("NavMeshAgent component not found on the worker, disabling script.");
            this.enabled = false;
        }

        playerBase = FindObjectOfType<Base>();

        if (playerBase == null)
        {
            Debug.Log("Base not assigned to " + gameObject.name);
            this.enabled = false; // Optionally disable this script if the base is not assigned.
        }

        UnitMLManager.Instance.AddWorker(gameObject);
    }

    void Update()
    {
        HandleHealth();
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
        Camera camera = Camera.main;
        unitStatDisplay.transform.LookAt(unitStatDisplay.transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);

        healthBarAmount.fillAmount = currentHealth / health;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Stop all coroutines explicitly to avoid any execution after destruction
        StopAllCoroutines();  // This will ensure all coroutines are stopped when the worker dies

        if (selectableObject)
        {
            selectableObject.DeSelectMe();
            SelectionManager.Instance.AllSelectableObjects.Remove(selectableObject);
        }

        // Communicate with any manager or system that needs to know this unit has died
        UnitMLManager.Instance.RemoveWorker(gameObject);

        // Ensure no further code executes by deactivating the gameObject first
        gameObject.SetActive(false);

        Destroy(gameObject);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void HandleRightClick()
    {
        if (selectableObject.isSelected)  // Only process click if this unit is selected
        {
            Debug.Log($"Right click handled for selected {gameObject.name}");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // Check if the hit object does not have the "Resource" tag
                if (hit.collider.gameObject.tag != "Resource")
                {
                    StopMining();  // Stop the mining task if currently mining
                    MoveToLocation(hit.point);
                    Debug.Log(hit.point); // Move to the clicked location
                }
                else if (hit.collider.gameObject.tag == "Resource")
                {
                    StartMining(hit.collider.gameObject);
                }
            }
        }
        else
        {
            Debug.Log($"Right click ignored for unselected {gameObject.name}");
        }
    }

    public void MoveToLocation(Vector3 targetPosition)
    {
        if (this == null || navMeshAgent == null)
        {
            Debug.Log("Agent or NavMeshAgent is null.");
            return;
        }

        if (!navMeshAgent.isOnNavMesh)
        {
            StartCoroutine(EnsureAgentIsOnNavMesh(targetPosition));
        }
        else
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(targetPosition);
            Debug.Log($"Moving {gameObject.name} to {targetPosition}");
        }
    }

    private IEnumerator EnsureAgentIsOnNavMesh(Vector3 targetPosition)
    {
        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(transform.position, out navMeshHit, 1.0f, NavMesh.AllAreas))
        {
            transform.position = navMeshHit.position;
            navMeshAgent.Warp(navMeshHit.position); // Place the agent on the NavMesh
        }
        else
        {
            Debug.Log("NavMeshAgent is not on the NavMesh and failed to find a valid position.");
            yield break;
        }

        yield return new WaitForSeconds(0.1f); // Small delay to ensure agent is placed on NavMesh

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(targetPosition);
        Debug.Log($"Moving {gameObject.name} to {targetPosition} after ensuring on NavMesh");
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
        // Change 2: Updated to start coroutine from IEnumerator
        miningCoroutine = MineGold(goldMine);
        StartCoroutine(miningCoroutine);
    }

    private IEnumerator MineGold(GameObject goldMine)
    {
        Vector3 minePosition = goldMine.transform.position;
        while (isAssignedTask)
        {
            if (goldMine == null || playerBase == null || this == null || !gameObject.activeInHierarchy)
            {
                StopMining();
                yield break; // Use yield break to exit the coroutine immediately
            }

            if (Vector3.Distance(transform.position, minePosition) > attackRange)
            {
                if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
                {
                    StopMining();
                    yield break; // Use yield break to exit the coroutine immediately
                }
                navMeshAgent.isStopped = false;
                MoveToLocation(minePosition);
            }

            yield return new WaitUntil(() => goldMine == null || playerBase == null || this == null || !gameObject.activeInHierarchy || Vector3.Distance(transform.position, minePosition) <= attackRange);

            if (goldMine == null || playerBase == null || this == null || !gameObject.activeInHierarchy)
            {
                StopMining();
                yield break; // Use yield break to exit the coroutine immediately
            }

            while (carriedGold < capacity && Vector3.Distance(transform.position, minePosition) <= attackRange)
            {
                carriedGold++;
                yield return new WaitForSeconds(0.2f / attackSpeed);

                if (carriedGold >= capacity || this == null || !gameObject.activeInHierarchy)
                {
                    if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
                    {
                        Debug.Log("NavMeshAgent is null or not on the NavMesh.");
                        yield break; // Use yield break to exit the coroutine immediately
                    }
                    navMeshAgent.isStopped = false;
                    MoveToLocation(playerBase != null ? playerBase.transform.position : Vector3.zero);
                    yield return new WaitUntil(() => playerBase == null || Vector3.Distance(transform.position, playerBase.transform.position) <= attackRange);

                    if (playerBase != null && this != null && gameObject.activeInHierarchy)
                    {
                        playerBase.DepositGold(carriedGold);
                        carriedGold = 0;
                    }
                    else
                    {
                        Debug.Log("Base reference not set on WorkerAgent or WorkerAgent is destroyed.");
                    }

                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
            navMeshAgent.isStopped = true;
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
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }
    }

}
