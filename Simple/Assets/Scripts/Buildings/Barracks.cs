using UnityEngine;
using UnityEngine.UI;

public class Barracks : MonoBehaviour, IDamageable
{
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public Transform spawnPoint;
    public SelectableObject selectableObject;
    public GameObject unitStatDisplay;
    public Image healthBarAmount;
    public float health = 500f;
    private float currentHealth;
    public Button spawnArcherButton;
    public Button spawnWarriorButton;

    public void Start()
    {
        selectableObject = GetComponent<SelectableObject>();
        currentHealth = health;
        selectableObject.selectableByDrag = false;
        spawnArcherButton.gameObject.SetActive(false);
        spawnWarriorButton.gameObject.SetActive(false);
        spawnArcherButton.onClick.AddListener(() => UnitManager.Instance.SpawnArcher());
        spawnWarriorButton.onClick.AddListener(() => UnitManager.Instance.SpawnWarrior());
    }

    public void Update()
    {
        HandleHealth();

        if (selectableObject.isSelected)
        {
            spawnArcherButton.gameObject.SetActive(true);
            spawnWarriorButton.gameObject.SetActive(true);
        }
        else
        {
            spawnArcherButton.gameObject.SetActive(false);
            spawnWarriorButton.gameObject.SetActive(false);
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

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    private void Die()
    {
        if (selectableObject)
        {
            selectableObject.DeSelectMe();
            SelectionManager.Instance.AllSelectableObjects.Remove(selectableObject);
        }
        //UnitMLManager.Instance.OnBarracksDestroyed(this);
        gameObject.SetActive(false);
    }

    public void ResetBarracks()
    {
        currentHealth = health;
        //gameObject.tag = "PlayerBase"; // Change tag back to "PlayerBase" or the appropriate tag
        gameObject.SetActive(true);
        HandleHealth();
    }
}
