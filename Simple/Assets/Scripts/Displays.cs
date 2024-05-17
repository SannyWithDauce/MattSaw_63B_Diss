using UnityEngine;
using TMPro;  // Namespace for TextMeshPro

public class Displays : MonoBehaviour
{
    public TextMeshProUGUI goldText;  // Drag your TextMeshPro UI element for gold here in the inspector
    public TextMeshProUGUI unitsText;  // Drag your TextMeshPro UI element for units here in the inspector
    public TextMeshProUGUI enemyGoldText;  // Drag your TextMeshPro UI element for enemy gold here in the inspector
    public TextMeshProUGUI enemyUnitsText;  // Drag your TextMeshPro UI element for enemy units here in the inspector

    private void Start()
    {
        // Initialize displays (you could fetch these from GoldMLManager if already set)
        UpdateGoldDisplay(GoldMLManager.Instance.TotalGold);
        UpdateUnitDisplay(UnitMLManager.Instance.totalUnits, UnitMLManager.Instance.maxUnits);

        // Initialize enemy displays
        UpdateEnemyGoldDisplay(EnemyGameManager.Instance.TotalGold);
        UpdateEnemyUnitDisplay(EnemyUnitManager.Instance.totalUnits, EnemyUnitManager.Instance.maxUnits);
    }

    private void OnEnable()
    {
        // Subscribe to events that trigger when gold and unit counts change
        GoldMLManager.OnGoldChanged += UpdateGoldDisplay;
        UnitMLManager.OnUnitChanged += UpdateUnitDisplay;

        // Subscribe to enemy events
        EnemyGameManager.OnEnemyGoldChanged += UpdateEnemyGoldDisplay;
        EnemyUnitManager.OnEnemyUnitChanged += UpdateEnemyUnitDisplay;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        GoldMLManager.OnGoldChanged -= UpdateGoldDisplay;
        UnitMLManager.OnUnitChanged -= UpdateUnitDisplay;

        // Unsubscribe from enemy events
        EnemyGameManager.OnEnemyGoldChanged -= UpdateEnemyGoldDisplay;
        EnemyUnitManager.OnEnemyUnitChanged -= UpdateEnemyUnitDisplay;
    }

    public void UpdateGoldDisplay(int totalGold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {totalGold}";
        else
            Debug.LogError("TextMeshPro component not set on GoldDisplay script.");
    }

    public void UpdateUnitDisplay(int totalUnits, int maxUnits)
    {
        if (unitsText != null)
            unitsText.text = $"Units: {totalUnits}/{maxUnits}";
        else
            Debug.LogError("TextMeshPro component not set on UnitsDisplay script.");
    }

    public void UpdateEnemyGoldDisplay(int totalGold)
    {
        if (enemyGoldText != null)
            enemyGoldText.text = $"Enemy Gold: {totalGold}";
        else
            Debug.LogError("TextMeshPro component not set on EnemyGoldDisplay script.");
    }

    public void UpdateEnemyUnitDisplay(int totalUnits, int maxUnits)
    {
        if (enemyUnitsText != null)
            enemyUnitsText.text = $"Enemy Units: {totalUnits}/{maxUnits}";
        else
            Debug.LogError("TextMeshPro component not set on EnemyUnitsDisplay script.");
    }
}
