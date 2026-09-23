using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameScript : MonoBehaviour
{
    public UIDocument uiDocument;
    public Spawner spawner;

    public float duration = 60f;
    public float cooldownDuration = 1f;

    public int resourceCount = 5000;
    public int macrophageCost = 250;
    public int neutrophilCost = 150;
    public int basophilCost = 500;

    private ProgressBar _progressBar;
    private float _currentTime = 0f;
    private bool _isTimerRunning = false;

    private Button macrophage;
    private Button neutrophil;

    [SerializeField] public LayerMask selectableLayer;
    public Transform _currentSelection;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        Button macrophage = root.Q<Button>("macrophage");
        Button neutrophil = root.Q<Button>("neutrophil");
        Button basophil = root.Q<Button>("basophil");
        Label resource = root.Q<Label>("resource");
        Label nameLabel = root.Q<Label>("name");
        _progressBar = root.Q<ProgressBar>("timer-bar");

        nameLabel.text = "-";
        resource.text = resourceCount.ToString();
        // functions for BUTTONS
        if (macrophage != null)
            macrophage.clicked +=
                () =>
                {
                    Debug.Log("Before: " + resourceCount);
                    resourceCount = resourceCount - macrophageCost;
                    resource.text = resourceCount.ToString();
                    Debug.Log("After: " + resourceCount);

                    StartCoroutine(MacrophageRoutine(macrophage, () => spawner.SpawnMacrophage()));
                };
        if (neutrophil != null)
            neutrophil.clicked +=
                () =>
                {
                    Debug.Log("Before: " + resourceCount);
                    resourceCount = resourceCount - neutrophilCost;
                    resource.text = resourceCount.ToString();
                    Debug.Log("After: " + resourceCount);

                    StartCoroutine(NeutrophilRoutine(neutrophil, () => spawner.SpawnNeutrophil()));
                };
                    if (basophil != null)
            basophil.clicked += () => StartCoroutine(BasophilRoutine(basophil));

        // debugger
        if (_progressBar != null)
        {
            ResetTimer();
            StartTimer();
        }
        else
        {
            Debug.LogError("ProgressBar with name 'timer-bar' not found in UIDocument.");
        }
    }

    void Update()
    {
        // restarts the game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MainMenu");
        }
        HandleTimer();
        // checks for mouse input, selects a pathogen
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            DeselectCurrent();

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
            {

                Transform selection = hit.transform;
                SelectObject(selection);

            }
        }
        // checks if RESOURCE_COUNT is insufficient, disables BUTTONS
        if (macrophageCost > resourceCount)
        {
            if (macrophage != null)
            {
                macrophage.SetEnabled(false);
            }
        }
        if (!(neutrophilCost <= resourceCount))
        {
            if (neutrophil != null)
            {
                neutrophil.SetEnabled(false);
            }
        }
    }

    private void HandleTimer() // no idea
    {
        if (!_isTimerRunning || _progressBar == null) return;

        _currentTime += Time.deltaTime;
        float progressPercentage = (_currentTime / duration) * 100f;
        _progressBar.value = Mathf.Clamp(progressPercentage, 0f, 100f);

        if (_currentTime >= duration)
        {
            OnTimerComplete();
        }
    }

    public void StartTimer() => _isTimerRunning = true; // no idea
    public void PauseTimer() => _isTimerRunning = false; // no idea

    public void ResetTimer() // no idea
    {
        _currentTime = 0f;
        if (_progressBar != null) _progressBar.value = 0f; 
    }

    private void OnTimerComplete() // no idea
    {
        _isTimerRunning = false;
        Debug.Log("Timer finished!");
    }

    private void SelectObject(Transform selection)
    {

        _currentSelection = selection;

        var selectionRenderer = _currentSelection.GetComponent<Renderer>();
        if (selectionRenderer != null)
        {
            selectionRenderer.material.color = Color.darkRed; // or new material
        }

        // nameLabel.text = _currentSelection.name;, doesn't work
        Debug.Log(_currentSelection.name);
    }

    public void DeselectCurrent()
    {
        if (_currentSelection != null)
        {
            var selectionRenderer = _currentSelection.GetComponent<Renderer>();
            if (selectionRenderer != null)
            {
                selectionRenderer.material.color = Color.gray; // or original material
            }

            _currentSelection = null;
        }
    }

    private IEnumerator MacrophageRoutine(Button button, System.Action spawnAction)
    {
        button.SetEnabled(false);
        spawnAction.Invoke();
        yield return new WaitForSeconds(cooldownDuration);
        if (macrophageCost < resourceCount)
        {
            button.SetEnabled(true);
        }
    }

    private IEnumerator NeutrophilRoutine(Button button, System.Action spawnAction)
    {
        button.SetEnabled(false);
        spawnAction.Invoke();
        yield return new WaitForSeconds(cooldownDuration);
        if (neutrophilCost < resourceCount)
        {
            button.SetEnabled(true);
        }
    }

    private IEnumerator BasophilRoutine(Button button)
    {
        button.SetEnabled(false);

        cooldownDuration = 0.5f;
        yield return new WaitForSeconds(15f);

        cooldownDuration = 1f;
        yield return new WaitForSeconds(15f);

        if (macrophageCost < resourceCount)
        {
            button.SetEnabled(true);
        }
    }
}