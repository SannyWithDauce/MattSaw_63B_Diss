using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainingStepDisplay : MonoBehaviour
{
    public TextMeshProUGUI stepText; // Reference to the UI Text element

    // This method will be called to update the step text
    public void UpdateStepText(int steps)
    {
        if (stepText != null)
        {
            stepText.text = "Steps: " + steps.ToString();
        }
    }
}