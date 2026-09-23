using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the "CONSOLE" panel (mock region #3):
/// a scrolling, timestamped, color-coded event log like:
/// [06:38] > UAV sweep initiated over Sector 7G
/// [06:42] > Unit damaged - Minor injury (C03)
///
/// Any script in the game can call:
///     ConsoleLogUI.Instance.Log("Squad Alpha deployed to Sector 3", ConsoleLogUI.LogType.Info);
/// and it will appear instantly, color-coded by type.
///
/// Setup:
/// 1. Create a ScrollRect with a Vertical Layout Group content panel.
/// 2. Create a TMP text prefab for a single log line (just an empty
///    TextMeshProUGUI, no special setup needed).
/// 3. Assign contentParent (the ScrollRect's Content object) and
///    logLinePrefab in the Inspector.
/// </summary>
public class ConsoleLogUI : MonoBehaviour
{
    public static ConsoleLogUI Instance { get; private set; }

    public enum LogType { Info, Success, Warning, Danger, System }

    [Header("UI References")]
    [SerializeField] private Transform contentParent;       // ScrollRect Content
    [SerializeField] private TextMeshProUGUI logLinePrefab;  // single log line prefab
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI eventCountLabel; // e.g. "7 EVTS"

    [Header("Behavior")]
    [SerializeField] private int maxLines = 100;
    [SerializeField] private bool autoScroll = true;
    [SerializeField] private bool useGameClockTimestamp = true;
    [SerializeField] private DayCounterUI dayCounter; // optional, for real in-game timestamps

    [Header("Colors")]
    [SerializeField] private Color infoColor = new Color(0.6f, 0.75f, 0.85f);
    [SerializeField] private Color successColor = new Color(0.35f, 0.85f, 0.45f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.2f);
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.35f, 0.3f);
    [SerializeField] private Color systemColor = new Color(0.6f, 0.6f, 0.65f);

    private readonly Queue<TextMeshProUGUI> activeLines = new Queue<TextMeshProUGUI>();
    private int totalEvents;

    private void Awake()
    {
        // Simple singleton so any script can reach this without a scene reference.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Push a new line into the console. Call this from anywhere:
    /// ConsoleLogUI.Instance.Log("Resource collected: 120 metal", ConsoleLogUI.LogType.Success);
    /// </summary>
    public void Log(string message, LogType type = LogType.Info)
    {
        if (logLinePrefab == null || contentParent == null)
        {
            Debug.LogWarning("[ConsoleLogUI] Missing prefab or content parent.");
            return;
        }

        string timestamp = GetTimestamp();
        TextMeshProUGUI line = Instantiate(logLinePrefab, contentParent);
        line.text = $"[{timestamp}] > {message}";
        line.color = GetColor(type);
        line.gameObject.SetActive(true);

        activeLines.Enqueue(line);
        totalEvents++;
        UpdateEventCount();
        TrimOldLines();

        if (autoScroll)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    private string GetTimestamp()
    {
        if (useGameClockTimestamp && dayCounter != null)
            return $"{dayCounter.CurrentHour:00}:{dayCounter.CurrentMinute:00}";

        return System.DateTime.Now.ToString("HH:mm");
    }

    private Color GetColor(LogType type)
    {
        switch (type)
        {
            case LogType.Success: return successColor;
            case LogType.Warning: return warningColor;
            case LogType.Danger: return dangerColor;
            case LogType.System: return systemColor;
            default: return infoColor;
        }
    }

    private void TrimOldLines()
    {
        while (activeLines.Count > maxLines)
        {
            var oldest = activeLines.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }
    }

    private void UpdateEventCount()
    {
        if (eventCountLabel != null)
            eventCountLabel.text = $"{totalEvents} EVTS";
    }

    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        // Wait a frame so the layout group has resized before we scroll.
        yield return null;
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    // ------------------------------------------------------------------
    // Cell-unit convenience logging (matches mock lines like:
    // "Squad Alpha deployed to Sector 3", "Human vitals nominal - A01",
    // "Unit damaged - Minor injury (C03)")
    // ------------------------------------------------------------------

    /// <summary>Log a unit being deployed into the world.</summary>
    /// <example>ConsoleLogUI.Instance.LogDeployment("Macrophage-01", "Sector 3");</example>
    public void LogDeployment(string unitName, string location)
    {
        Log($"{unitName} deployed to {location}", LogType.Success);
    }

    /// <summary>Log a command issued to a unit (move, attack, patrol, hold, etc).</summary>
    /// <example>ConsoleLogUI.Instance.LogCommand("Macrophage-01", "Move", "(124, 88)");</example>
    public void LogCommand(string unitName, string command, string target = null)
    {
        string msg = string.IsNullOrEmpty(target)
            ? $"{unitName} ordered to {command}"
            : $"{unitName} ordered to {command} \u2192 {target}";
        Log(msg, LogType.Info);
    }

    /// <summary>Log a unit's current status/vitals/condition.</summary>
    /// <example>ConsoleLogUI.Instance.LogStatus("A01", "vitals nominal", ConsoleLogUI.LogType.Success);</example>
    /// <example>ConsoleLogUI.Instance.LogStatus("C03", "Minor injury", ConsoleLogUI.LogType.Warning);</example>
    public void LogStatus(string unitName, string statusMessage, LogType type = LogType.Info)
    {
        Log($"{unitName} \u2013 {statusMessage}", type);
    }

    /// <summary>Log a unit being lost/destroyed.</summary>
    public void LogUnitLost(string unitName, string cause = null)
    {
        string msg = string.IsNullOrEmpty(cause)
            ? $"{unitName} lost"
            : $"{unitName} lost \u2013 {cause}";
        Log(msg, LogType.Danger);
    }

    public void ClearLog()
    {
        while (activeLines.Count > 0)
        {
            var line = activeLines.Dequeue();
            if (line != null) Destroy(line.gameObject);
        }
        totalEvents = 0;
        UpdateEventCount();
    }

    // --- Quick test helper: right-click this component in the Inspector ---
    [ContextMenu("Test Log Line")]
    private void TestLog()
    {
        Log("Test event fired from ConsoleLogUI.", LogType.Info);
    }
}
