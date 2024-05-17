using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SelectableObject : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject SelectionMarker;
    public bool isSelected;
    public bool selectableByDrag = true;

    void Start()
    {
        SelectionMarker.SetActive(false);
        isSelected = false;
    }

    public void SelectMe()
    {
        SelectionMarker.SetActive(true);
        isSelected = true;
    }

    public void DeSelectMe()
    {
        SelectionMarker.SetActive(false);
        isSelected = false;
    }

}
