using UnityEngine;
using UnityEngine.UI;

public class Base : MonoBehaviour, IDamageable
{
    public GameObject WorkerPrefab;
    public Transform spawnPoint;
    public SelectableObject selectableObject;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float health = 1000f;
    private float currentHealth;
    public Button spawnWorkerButton;

    public void Start()
    {
        selectableObject = GetComponent<SelectableObject>();
        currentHealth = health;
        selectableObject.selectableByDrag = false;
        spawnWorkerButton.gameObject.SetActive(false);
        spawnWorkerButton.onClick.AddListener(() => UnitMLManager.Instance.SpawnWorker());
    }

    public void Update()
    {
        HandleHealth();

        if (selectableObject.isSelected)
        {
            spawnWorkerButton.gameObject.SetActive(true);
        }
        else
        {
            spawnWorkerButton.gameObject.SetActive(false);
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
        if (selectableObject)
        {
            selectableObject.DeSelectMe();
            SelectionManager.Instance.AllSelectableObjects.Remove(selectableObject);
        }
        //UnitMLManager.Instance.OnBaseDestroyed(this);
        gameObject.SetActive(false);
    }

    public void ResetBase()
    {
        currentHealth = health;
        //gameObject.tag = "PlayerBase"; // Change tag back to "PlayerBase" or the appropriate tag
        gameObject.SetActive(true);
        HandleHealth();
    }

    public void DepositGold(int amount)
    {
        if (GoldMLManager.Instance != null)
        {
            GoldMLManager.Instance.AddGold(amount);
        }
        else
        {
            Debug.LogError("GoldMLManager instance is null when trying to deposit gold.");
        }
    }
}
