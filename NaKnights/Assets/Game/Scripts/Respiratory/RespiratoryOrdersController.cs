using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RespiratoryOrdersController : MonoBehaviour
{
    private const float DefaultCooldownDuration = 5f;
    private const string CoughButtonName = "Cough";
    private const string ReadyLabel = "Cough";

    [SerializeField] private RespiratorySystemController respiratorySystemController;
    [SerializeField] private Button coughButton;
    [SerializeField] private TextMeshProUGUI coughLabel;

    [Tooltip("Seconds before Cough can be used again.")]
    [SerializeField] private float cooldownDuration = DefaultCooldownDuration;

    private void Start()
    {
        if (respiratorySystemController == null)
        {
            Debug.LogError("RespiratoryOrdersController requires a RespiratorySystemController reference.", this);
            return;
        }

        if (coughButton == null)
        {
            Transform buttonTransform = transform.Find(CoughButtonName);
            if (buttonTransform != null)
            {
                coughButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (coughLabel == null && coughButton != null)
        {
            coughLabel = coughButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (coughButton == null || coughLabel == null)
        {
            Debug.LogError("RespiratoryOrdersController requires a Cough button with a TextMeshProUGUI label.", this);
            return;
        }

        coughLabel.text = ReadyLabel;
        coughButton.onClick.AddListener(Cough);
    }

    /// <summary>
    /// Clears all active air particles and starts the Cough cooldown.
    /// </summary>
    public void Cough()
    {
        if (coughButton == null || !coughButton.interactable)
        {
            return;
        }

        respiratorySystemController.DestroyAllAir();
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        coughButton.interactable = false;
        float remainingTime = Mathf.Max(0f, cooldownDuration);

        while (remainingTime > 0f)
        {
            coughLabel.text = $"{ReadyLabel}\n{Mathf.CeilToInt(remainingTime)}s";
            yield return null;
            remainingTime -= Time.deltaTime;
        }

        coughLabel.text = ReadyLabel;
        coughButton.interactable = true;
    }
}
