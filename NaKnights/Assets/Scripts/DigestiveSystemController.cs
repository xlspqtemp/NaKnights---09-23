using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Controls digestive-system food spawning, routing, Consume/Vomit actions, and meal timing.
/// </summary>
public class DigestiveSystemController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DayCounterUI dayCounter;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private Transform foodParent;

    [Header("Food Prefabs")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject contaminatedFoodPrefab;

    [Header("Scheduled Feeding")]
    [SerializeField] private int scheduledBatchSize = DEFAULT_SCHEDULED_BATCH_SIZE;
    [SerializeField] private float spawnInterval = DEFAULT_SPAWN_INTERVAL;
    [SerializeField] private float contaminatedFoodChance = DEFAULT_CONTAMINATED_CHANCE;
    [SerializeField] private float navMeshSampleRadius = 5f;

    [Header("HUD Buttons")]
    [SerializeField] private Button consumeButton;
    [SerializeField] private Button vomitButton;
    [SerializeField] private TextMeshProUGUI consumeLabel;
    [SerializeField] private TextMeshProUGUI vomitLabel;
    [SerializeField] private float consumeCooldownDuration = DEFAULT_COOLDOWN;
    [SerializeField] private float scheduledConsumeLockDuration = DEFAULT_SCHEDULED_LOCK_DURATION;
    [SerializeField] private float vomitCooldownDuration = DEFAULT_COOLDOWN;

    private static readonly int[] ScheduledMealHours = { 8, 12, 18 };
    private readonly HashSet<int> scheduledMealsHandled = new HashSet<int>();

    private Coroutine consumeLockRoutine;
    private Coroutine vomitCooldownRoutine;
    private bool consumeLocked;
    private bool vomitOnCooldown;

    private const int MANUAL_BATCH_SIZE = 5;
    private const int HOURS_PER_DAY = 24;
    private const int DEFAULT_SCHEDULED_BATCH_SIZE = 10;
    private const float DEFAULT_SPAWN_INTERVAL = 0.5f;
    private const float DEFAULT_CONTAMINATED_CHANCE = 0.05f;
    private const float DEFAULT_COOLDOWN = 5f;
    private const float DEFAULT_SCHEDULED_LOCK_DURATION = 10f;

    private void Start()
    {
        ResolveSceneReferences();
        ConfigureButtons();

        if (dayCounter != null)
        {
            dayCounter.OnTimeChanged += HandleTimeChanged;
            HandleTimeChanged(dayCounter.CurrentHour, dayCounter.CurrentMinute);
        }
    }

    private void OnDestroy()
    {
        if (dayCounter != null)
            dayCounter.OnTimeChanged -= HandleTimeChanged;
    }

    /// <summary>Spawns five food items when the Consume button is available.</summary>
    public void Consume()
    {
        if (consumeLocked || consumeButton != null && !consumeButton.interactable)
            return;

        SpawnBatch(MANUAL_BATCH_SIZE);
        StartConsumeCooldown();
    }

    /// <summary>Destroys every active normal or contaminated food item and starts its cooldown.</summary>
    public void Vomit()
    {
        if (vomitOnCooldown || vomitButton != null && !vomitButton.interactable)
            return;

        DestroyAllFood();
        vomitCooldownRoutine = StartCoroutine(VomitCooldownRoutine());
    }

    private void ResolveSceneReferences()
    {
        if (dayCounter == null)
            dayCounter = FindFirstObjectByType<DayCounterUI>();

        if (spawnPoint == null)
        {
            GameObject startObject = GameObject.Find("Start");
            if (startObject != null)
                spawnPoint = startObject.transform;
        }

        if (endPoint == null)
        {
            GameObject endObject = GameObject.Find("End");
            if (endObject != null)
                endPoint = endObject.transform;
        }

        if (foodParent == null)
            foodParent = transform;

        scheduledBatchSize = Mathf.Max(1, scheduledBatchSize);
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        contaminatedFoodChance = Mathf.Clamp01(contaminatedFoodChance);
        navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
    }

    private void ConfigureButtons()
    {
        if (consumeButton != null)
        {
            consumeButton.onClick.RemoveListener(Consume);
            consumeButton.onClick.AddListener(Consume);
            consumeButton.interactable = true;
        }

        if (vomitButton != null)
        {
            vomitButton.onClick.RemoveListener(Vomit);
            vomitButton.onClick.AddListener(Vomit);
            vomitButton.interactable = true;
        }

        SetLabel(consumeLabel, "CONSUME");
        SetLabel(vomitLabel, "VOMIT");
    }

    private void HandleTimeChanged(int currentHour, int currentMinute)
    {
        if (dayCounter == null || currentMinute != 0)
            return;

        foreach (int mealHour in ScheduledMealHours)
        {
            if (currentHour != mealHour)
                continue;

            int mealKey = dayCounter.CurrentDay * HOURS_PER_DAY + mealHour;
            if (scheduledMealsHandled.Add(mealKey))
            {
                SpawnBatch(scheduledBatchSize);
                StartScheduledConsumeLock();
            }
        }
    }

    private void SpawnBatch(int batchSize)
    {
        GameObject selectedPrefab = SelectFoodPrefab();
        if (selectedPrefab == null || spawnPoint == null || endPoint == null)
            return;

        StartCoroutine(SpawnBatchRoutine(selectedPrefab, Mathf.Max(1, batchSize)));
    }

    private IEnumerator SpawnBatchRoutine(GameObject selectedPrefab, int batchSize)
    {
        Vector3 spawnPosition = GetNavMeshPosition(spawnPoint.position);

        for (int i = 0; i < batchSize; i++)
        {
            GameObject spawnedFood = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, foodParent);
            DigestiveFoodRoute route = spawnedFood.GetComponent<DigestiveFoodRoute>();
            if (route == null)
                route = spawnedFood.AddComponent<DigestiveFoodRoute>();

            route.Configure(endPoint);
            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private GameObject SelectFoodPrefab()
    {
        bool isContaminated = contaminatedFoodPrefab != null && Random.value < contaminatedFoodChance;
        return isContaminated ? contaminatedFoodPrefab : foodPrefab;
    }

    private Vector3 GetNavMeshPosition(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return hit.position;

        return requestedPosition;
    }

    private void DestroyAllFood()
    {
        if (foodParent == null)
            return;

        DigestiveFoodRoute[] foodItems = foodParent.GetComponentsInChildren<DigestiveFoodRoute>(true);
        foreach (DigestiveFoodRoute foodItem in foodItems)
        {
            if (foodItem != null)
                Destroy(foodItem.gameObject);
        }
    }

    private void StartConsumeCooldown()
    {
        StartConsumeLock(consumeCooldownDuration);
    }

    private void StartScheduledConsumeLock()
    {
        StartConsumeLock(scheduledConsumeLockDuration);
    }

    private void StartConsumeLock(float duration)
    {
        if (consumeLockRoutine != null)
            StopCoroutine(consumeLockRoutine);

        consumeLockRoutine = StartCoroutine(ConsumeLockRoutine(duration));
    }

    private IEnumerator ConsumeLockRoutine(float duration)
    {
        consumeLocked = true;
        if (consumeButton != null)
            consumeButton.interactable = false;

        float remaining = Mathf.Max(0f, duration);
        while (remaining > 0f)
        {
            SetLabel(consumeLabel, $"CONSUME\n{Mathf.CeilToInt(remaining)}s");
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        consumeLocked = false;
        consumeLockRoutine = null;
        if (consumeButton != null)
            consumeButton.interactable = true;
        SetLabel(consumeLabel, "CONSUME");
    }

    private IEnumerator VomitCooldownRoutine()
    {
        vomitOnCooldown = true;
        if (vomitButton != null)
            vomitButton.interactable = false;

        float remaining = Mathf.Max(0f, vomitCooldownDuration);
        while (remaining > 0f)
        {
            SetLabel(vomitLabel, $"VOMIT\n{Mathf.CeilToInt(remaining)}s");
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        vomitOnCooldown = false;
        vomitCooldownRoutine = null;
        if (vomitButton != null)
            vomitButton.interactable = true;
        SetLabel(vomitLabel, "VOMIT");
    }

    private static void SetLabel(TextMeshProUGUI label, string text)
    {
        if (label != null)
            label.text = text;
    }
}
