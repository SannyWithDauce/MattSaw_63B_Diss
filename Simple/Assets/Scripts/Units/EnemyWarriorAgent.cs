using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyWarriorAgent : MonoBehaviour, IDamageable
{
    public NavMeshAgent navMeshAgent;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float currentHealth;
    public float destinationRadius = 1f;
    public float maxDestinationAttempts = 5;
    public bool isAssignedTask = false;

    [Header("Warrior Stats")]
    public float health = 150f;
    public float attackSpeed = 1.0f;
    public float attackRange = 1.5f;
    public float attackDamage = 40f;

    public IEnumerator attackCoroutine;
    public GameObject currentTarget;
    private float aggroRadius = 7f;
    public enum State { Idle, Moving, Attacking }
    public State currentState = State.Idle;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentHealth = health;

        if (navMeshAgent == null)
        {
            Debug.Log("NavMeshAgent component not found on the warrior, disabling script.");
            this.enabled = false;
        }
    }

    void Update()
    {
        if (this == null)
        {
            return;
        }

        HandleHealth();

        if (currentState == State.Idle)
        {
            CheckForEnemiesInAggroRadius();
        }

        if (currentState == State.Moving && navMeshAgent != null && !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
            {
                currentState = State.Idle; // Set to idle if the agent has reached the destination
                CheckForEnemiesInAggroRadius(); // Re-check for enemies when reaching the destination
            }
        }
    }

    private void CheckForEnemiesInAggroRadius()
    {
        if (this == null)
        {
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, aggroRadius);
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject.CompareTag("PlayerPrefabs"))
            {
                if (currentState != State.Attacking)
                {
                    currentTarget = col.gameObject;
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                    }
                    attackCoroutine = AttackTarget(currentTarget);
                    StartCoroutine(attackCoroutine);
                    break; // Attack the first enemy found within aggro radius
                }
            }
        }
    }

    private Vector3 GetAdjustedDestination(Vector3 targetPosition)
    {
        Vector3 adjustedPosition = targetPosition;
        float randomX = Random.Range(-destinationRadius, destinationRadius);
        float randomZ = Random.Range(-destinationRadius, destinationRadius);
        adjustedPosition += new Vector3(randomX, 0f, randomZ);
        return adjustedPosition;
    }

    public IEnumerator AttackTarget(GameObject target)
    {
        bool isAttacking = true; // Flag to indicate if the warrior is currently attacking

        while (target != null && this != null && gameObject.activeInHierarchy && isAttacking)
        {
            // Ensure NavMeshAgent and damageable component are still valid
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
            {
                currentState = State.Idle;
                yield break; // Exit if the NavMeshAgent is destroyed or not on a NavMesh
            }

            // Check if the target is active
            if (!target.activeInHierarchy)
            {
                currentTarget = null;
                currentState = State.Idle;
                yield break; // Exit if the target is deactivated
            }

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                // Exit if the target is no longer valid or alive
                currentTarget = null;
                currentState = State.Idle;
                yield break;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            // Move to target if out of attack range
            if (distanceToTarget > attackRange)
            {
                MoveToLocation(target.transform.position);
                currentState = State.Moving;
                // Wait until the target is within attack range or until the target/current unit is destroyed
                yield return new WaitUntil(() => target == null || this == null || gameObject.activeInHierarchy == false || Vector3.Distance(transform.position, target.transform.position) <= attackRange);
            }

            // Attack if in range
            if (target != null && this != null && distanceToTarget <= attackRange)
            {
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true; // Stop moving to attack
                }
                currentState = State.Attacking;
                damageable.TakeDamage(attackDamage); // Deal damage to the target

                // Wait for the attack interval before next attack
                float attackDelay = 1f / attackSpeed;
                yield return new WaitForSeconds(attackDelay);
            }
        }

        // Reset state to idle when loop exits
        currentState = State.Idle;
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }

        // Re-check for enemies after a brief delay to avoid rapid re-engagement
        if (this != null && gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(1f);
            CheckForEnemiesInAggroRadius();
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
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
        // Stop all coroutines explicitly to avoid any execution after destruction
        StopAllCoroutines();  // This will ensure all coroutines are stopped when the warrior dies

        // Communicate with any manager or system that needs to know this unit has died
        EnemyUnitManager.Instance.RemoveWarrior(gameObject);

        // Ensure no further code executes by deactivating the gameObject first
        gameObject.SetActive(false);

        Destroy(gameObject);
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
            Vector3 adjustedPosition = GetAdjustedDestination(targetPosition);
            NavMeshHit targetNavMeshHit;
            if (NavMesh.SamplePosition(adjustedPosition, out targetNavMeshHit, 1.0f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(targetNavMeshHit.position);
                currentState = State.Moving;
            }
            else
            {
                Debug.Log("Failed to find valid NavMesh position.");
            }
        }
    }
}
