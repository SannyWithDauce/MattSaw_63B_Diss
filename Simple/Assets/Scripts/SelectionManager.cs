using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public RectTransform SelectBox;
    public static SelectionManager Instance { get; set; }

    public List<SelectableObject> AllSelectableObjects;
    public List<SelectableObject> CurrSelectedObjects;

    bool isMouseDown, isDragging;
    Vector3 MouseStartPos;
    Vector3 mousePos;
    float mouseDownTime;
    const float holdDuration = 0.5f;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;  // Ensure this is set before any references are made
        //llSelectableObjects = new List<SelectableObject>();  // Initialize the list
    }

    void Start()
    {
        isMouseDown = false;
        isDragging = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // If it's over a UI element, return immediately to avoid further processing
                return;
            }

            ClearSelection(); // Clear the current selection list
            isMouseDown = true;
            MouseStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonDown(0)) // Right click
        {
            MouseStartPos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(MouseStartPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))  // Adjust the distance if necessary
            {
                SelectableObject hitObject = hit.transform.GetComponent<SelectableObject>();
                if (hitObject != null)
                {
                    ClearSelection();
                    CurrSelectedObjects.Add(hitObject);
                    hitObject.SelectMe();
                    Debug.Log($"Directly selected: {hitObject.gameObject.name}");
                    return;  // Exit the method to avoid initiating drag selection
                }
            }
            isMouseDown = true;
        }

        if (isMouseDown)
        {
            if (Vector3.Distance(Input.mousePosition, MouseStartPos) > 1 && !isDragging)
            {
                isDragging = true;
                SelectBox.gameObject.SetActive(true);
            }
            if (isDragging)
            {
                float boxWidth = Input.mousePosition.x - MouseStartPos.x;
                float boxHeight = Input.mousePosition.y - MouseStartPos.y;

                SelectBox.sizeDelta = new Vector2(Mathf.Abs(boxWidth), Mathf.Abs(boxHeight));
                SelectBox.anchoredPosition = (MouseStartPos + Input.mousePosition) / 2;

                SelectUnits();
            }
        }

        if (Input.GetMouseButtonUp(0) && isMouseDown == true)
        {
            isMouseDown = false;
            isDragging = false;
            SelectBox.gameObject.SetActive(false);
        }

        if (Input.GetMouseButtonDown(1) && isDragging == false)  // Check for right-click
        {
            ProcessRightClickWorker();
            ProcessRightClickArcher();
            ProcessRightClickWarrior();
        }
    }

    private void ClearSelection()
    {
        foreach (SelectableObject so in CurrSelectedObjects)
        {
            so.DeSelectMe();
        }
        CurrSelectedObjects.Clear();
    }

    void SelectUnits()
    {
        foreach (SelectableObject so in AllSelectableObjects)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(so.transform.position);

            float left = SelectBox.anchoredPosition.x - (SelectBox.sizeDelta.x / 2);
            float right = SelectBox.anchoredPosition.x + (SelectBox.sizeDelta.x / 2);
            float top = SelectBox.anchoredPosition.y + (SelectBox.sizeDelta.y / 2);
            float bottom = SelectBox.anchoredPosition.y - (SelectBox.sizeDelta.y / 2);

            if (screenPos.x > left && screenPos.x < right && screenPos.y > bottom && screenPos.y < top)
            {
                if (!CurrSelectedObjects.Contains(so) && so.selectableByDrag)
                {
                    CurrSelectedObjects.Add(so);
                    so.SelectMe();
                    Debug.Log($"Selected: {so.gameObject.name}");
                }
            }
            else
            {
                if (CurrSelectedObjects.Contains(so))
                {
                    CurrSelectedObjects.Remove(so);
                    so.DeSelectMe();
                    Debug.Log($"Deselected: {so.gameObject.name}");
                }
            }
        }
    }

    private void ProcessRightClickWorker()
    {
        foreach (var selectedObject in CurrSelectedObjects)
        {
            WorkerAgent worker = selectedObject.GetComponent<WorkerAgent>();
            if (worker != null)
            {
                worker.HandleRightClick();
            }
        }
    }

    private void ProcessRightClickWarrior()
    {
        foreach (var selectedObject in CurrSelectedObjects)
        {
            WarriorAgent warrior = selectedObject.GetComponent<WarriorAgent>();
            if (warrior != null)
            {
                warrior.HandleRightClick();
            }
        }
    }

    private void ProcessRightClickArcher()
    {
        foreach (var selectedObject in CurrSelectedObjects)
        {
            ArcherAgent archer = selectedObject.GetComponent<ArcherAgent>();
            if (archer != null)
            {
                archer.HandleRightClick();
            }
        }
    }
}
