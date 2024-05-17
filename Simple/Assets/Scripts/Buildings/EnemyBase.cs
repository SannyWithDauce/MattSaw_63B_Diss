using UnityEngine;
using UnityEngine.UI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    public GameObject WorkerPrefab;
    public Transform spawnPoint;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float health = 1000f;
    private float currentHealth;
    public float CurrentHealth => health;
    public bool IsAlive => health > 0;

    public void Start()
    {
        currentHealth = health;
    }

    public void Update()
    {
        HandleHealth();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void HandleHealth()
    {
        unitStatDisplay.transform.LookAt(unitStatDisplay.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        healthBarAmount.fillAmount = currentHealth / health;
    }

    private void Die()
    {
        //EnemyUnitManager.Instance.OnBaseDestroyed(this);
        gameObject.SetActive(false);
    }

    public void ResetEnemyBase()
    {
        currentHealth = health;
        //gameObject.tag = "PlayerBase"; // Change tag back to "PlayerBase" or the appropriate tag
        gameObject.SetActive(true);
        HandleHealth();
    }

    public void DepositGold(int amount)
    {
        if (EnemyGameManager.Instance != null)
        {
            EnemyGameManager.Instance.AddGold(amount);
        }
        else
        {
            Debug.LogError("GameManager instance is null when trying to deposit gold.");
        }
    }
}
