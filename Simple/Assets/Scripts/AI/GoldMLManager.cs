using UnityEngine;

public class GoldMLManager : MonoBehaviour
{
    public static GoldMLManager Instance { get; set; }
    public int TotalGold { get; set; } = 40;

    public delegate void GoldChanged(int goldAmount);
    public static event GoldChanged OnGoldChanged;

    private void Awake()
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

    public void AddGold(int amount)
    {
        TotalGold += amount;
        OnGoldChanged?.Invoke(TotalGold);
    }

    public void ResetGold()
    {
        TotalGold = 40;
        OnGoldChanged?.Invoke(TotalGold);
    }
}
