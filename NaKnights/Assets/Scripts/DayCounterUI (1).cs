using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the day and hour display in the HUD.
/// The configured real-time day length controls progression while running.
/// </summary>
public class DayCounterUI : MonoBehaviour
{
    public enum ProgressionMode { RealTime, Manual, ObjectiveBased, Hybrid }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayLabel;
    [SerializeField] private TextMeshProUGUI timeLabel;

    private const float DEFAULT_DAY_LENGTH_IN_REAL_SECONDS = 128.57143f;

    [Header("Progression Mode")]
    [Tooltip("RealTime: ticks on its own. Manual: you call AdvanceDay()/TickMinutes(). " +
             "ObjectiveBased: only advances via CompleteObjective(). Hybrid: ticks AND can be force-advanced.")]
    [SerializeField] private ProgressionMode progressionMode = ProgressionMode.RealTime;

    [Header("Real-Time Tuning")]
    [Tooltip("If enabled, set 'Day Length In Real Seconds' below and the per-minute tick rate is calculated for you.")]
    [SerializeField] private bool useDayLengthShortcut = true;
    [Tooltip("How many real seconds a full in-game day should take (RealTime/Hybrid only).")]
    [SerializeField] private float dayLengthInRealSeconds = DEFAULT_DAY_LENGTH_IN_REAL_SECONDS;
    [Tooltip("Manual tick rate: real seconds per in-game minute. Only used if the shortcut above is off.")]
    [SerializeField] private float secondsPerGameMinute = 1f;
    [Tooltip("Global speed multiplier you can nudge at runtime (e.g. a fast-forward button).")]
    [Range(0.1f, 10f)]
    [SerializeField] private float timeScale = 1f;

    [Header("Objective-Based Tuning")]
    [Tooltip("If true, CompleteObjective() only advances the day once progress reaches the threshold below.")]
    [SerializeField] private bool requireFullObjective = true;
    [Range(0f, 1f)]
    [SerializeField] private float objectiveCompletionThreshold = 1f;

    [Header("Start State")]
    [SerializeField] private bool isRunning = true;
    [SerializeField] private int startDay = 1;
    [SerializeField] private int startHour = 0;
    [SerializeField] private int startMinute = 0;

    public event Action<int> OnDayAdvanced;
    public event Action<int, int> OnTimeChanged;

    private int currentDay;
    private int currentHour;
    private int currentMinute;
    private float minuteTimer;
    private float runtimeSecondsPerGameMinute;

    private const int MINUTES_PER_HOUR = 60;
    private const int HOURS_PER_DAY = 24;
    private const int MINUTES_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR;

    private void Awake()
    {
        currentDay = startDay;
        currentHour = Mathf.Clamp(startHour, 0, HOURS_PER_DAY - 1);
        currentMinute = Mathf.Clamp(startMinute, 0, MINUTES_PER_HOUR - 1);
        RecalculateTickRate();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void OnValidate()
    {
        RecalculateTickRate();
    }

    private void RecalculateTickRate()
    {
        runtimeSecondsPerGameMinute = useDayLengthShortcut
            ? Mathf.Max(0.001f, dayLengthInRealSeconds / MINUTES_PER_DAY)
            : Mathf.Max(0.001f, secondsPerGameMinute);
    }

    private void Update()
    {
        bool clockShouldRun = isRunning &&
            (progressionMode == ProgressionMode.RealTime || progressionMode == ProgressionMode.Hybrid);

        if (!clockShouldRun) return;

        minuteTimer += Time.deltaTime * Mathf.Max(0.01f, timeScale);
        int minutesToAdvance = Mathf.FloorToInt(minuteTimer / runtimeSecondsPerGameMinute);
        if (minutesToAdvance <= 0) return;

        minuteTimer -= minutesToAdvance * runtimeSecondsPerGameMinute;
        TickMinutes(minutesToAdvance);
    }

    /// <summary>Manually advances the clock by the requested number of in-game minutes.</summary>
    public void TickMinutes(int minutes)
    {
        for (int i = 0; i < minutes; i++)
        {
            currentMinute++;
            if (currentMinute >= MINUTES_PER_HOUR)
            {
                currentMinute = 0;
                currentHour++;
                if (currentHour >= HOURS_PER_DAY)
                {
                    currentHour = 0;
                    AdvanceDay();
                }
            }
        }

        OnTimeChanged?.Invoke(currentHour, currentMinute);
        RefreshTime();
    }

    /// <summary>Advances the displayed day by one.</summary>
    public void AdvanceDay()
    {
        currentDay++;
        OnDayAdvanced?.Invoke(currentDay);
        RefreshDay();
    }

    /// <summary>Advances the day when objective progress meets the configured threshold.</summary>
    /// <param name="progress01">Completion progress from 0 to 1.</param>
    public void CompleteObjective(float progress01)
    {
        if (progressionMode != ProgressionMode.ObjectiveBased && progressionMode != ProgressionMode.Hybrid)
            return;

        bool meetsThreshold = requireFullObjective
            ? progress01 >= objectiveCompletionThreshold
            : progress01 > 0f;

        if (meetsThreshold)
            AdvanceDay();
    }

    /// <summary>Sets whether real-time progression is running.</summary>
    public void SetActive(bool active)
    {
        isRunning = active;
    }

    /// <summary>Sets the displayed day.</summary>
    public void SetDay(int day)
    {
        currentDay = day;
        RefreshDay();
    }

    /// <summary>Sets the displayed time using a 24-hour hour and minute value.</summary>
    public void SetTime(int hour, int minute)
    {
        currentHour = Mathf.Clamp(hour, 0, HOURS_PER_DAY - 1);
        currentMinute = Mathf.Clamp(minute, 0, MINUTES_PER_HOUR - 1);
        RefreshTime();
    }

    /// <summary>Sets the runtime clock speed multiplier.</summary>
    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0.01f, scale);
    }

    /// <summary>Switches the clock progression mode at runtime.</summary>
    public void SetProgressionMode(ProgressionMode mode)
    {
        progressionMode = mode;
    }

    private void RefreshAll()
    {
        RefreshDay();
        RefreshTime();
    }

    private void RefreshDay()
    {
        if (dayLabel != null)
            dayLabel.text = $"DAY {currentDay}";
    }

    private void RefreshTime()
    {
        if (timeLabel == null) return;

        int displayHour = currentHour % 12;
        if (displayHour == 0) displayHour = 12;
        string ampm = currentHour < 12 ? "AM" : "PM";
        timeLabel.text = $"{displayHour} {ampm}";
    }

    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public bool IsRunning => isRunning;
    public ProgressionMode CurrentMode => progressionMode;
}
