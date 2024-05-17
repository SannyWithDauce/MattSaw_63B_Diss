using UnityEngine;
using UnityEngine.UI;

public class EnemyBarracks : MonoBehaviour, IDamageable
{
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public Transform spawnPoint;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float health = 350f;
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

    public void SpawnWarrior()
    {
        if (GameManager.Instance.TotalGold >= 100)
        {
            Instantiate(warriorPrefab, spawnPoint.position, Quaternion.identity);
            GameManager.Instance.AddGold(-100); // Deduct the cost of the warrior
            Debug.Log("Warrior spawned.");
        }
        else
        {
            Debug.Log("Not enough gold to spawn a warrior.");
        }
    }

    public void SpawnArcher()
    {
        if (GameManager.Instance.TotalGold >= 125)
        {
            Instantiate(archerPrefab, spawnPoint.position, Quaternion.identity);
            GameManager.Instance.AddGold(-125); // Deduct the cost of the archer
            Debug.Log("Archer spawned.");
        }
        else
        {
            Debug.Log("Not enough gold to spawn an archer.");
        }
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
        //EnemyUnitManager.Instance.OnBarracksDestroyed(this);
        gameObject.SetActive(false);
    }

    public void ResetEnemyBarracks()
    {
        currentHealth = health;
        //gameObject.tag = "PlayerBase"; // Change tag back to "PlayerBase" or the appropriate tag
        gameObject.SetActive(true);
        HandleHealth();
    }
}
