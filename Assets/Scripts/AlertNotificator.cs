using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AICore;

[RequireComponent(typeof(CanvasGroup))]
public class AlertNotificator : MonoBehaviour
{
    [SerializeField] Image image1;
    [SerializeField] Image image2;
    private Dictionary<AICore, AlertData> activeAlerts = new Dictionary<AICore, AlertData>();
    CanvasGroup cg;

    private struct AlertData
    {
        public float tm;
        public AlertLevel level;
    }

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        OnAlertChanged += HandleAlertChanged;
        OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        OnAlertChanged -= HandleAlertChanged;
        OnEnemyDied -= HandleEnemyDied;
    }

    private void HandleAlertChanged(AICore enemy, float value, AlertLevel alertLevel)
    {
        if (alertLevel == AlertLevel.None && value <= 0f)
        {
            if (activeAlerts.ContainsKey(enemy))
            {
                activeAlerts.Remove(enemy);
            }
        }
        else
        {
            // Update or add this enemy's current status
            activeAlerts[enemy] = new AlertData { tm = value, level = alertLevel };
        }

        RefreshGlobalIndicator();
    }

    private void HandleEnemyDied(AICore enemy)
    {
        if (activeAlerts.ContainsKey(enemy))
        {
            activeAlerts.Remove(enemy);
            RefreshGlobalIndicator();
        }
    }

    private void RefreshGlobalIndicator()
    {
        if (activeAlerts.Count == 0)
        {
            cg.alpha = 0;
            return;
        }

        cg.alpha = 1;

        float highestTM = 0f;
        AlertLevel highestLevel = AlertLevel.None;

        foreach (var alert in activeAlerts.Values)
        {
            if (alert.tm > highestTM)
            {
                highestTM = alert.tm;
                highestLevel = alert.level;
            }
        }

        SetAlertProgress(highestTM, highestLevel);
    }

    private void SetAlertProgress(float value, AlertLevel alertLevel)
    {
        value = Mathf.Clamp(value, 0, 1);
        image1.fillAmount = value/5f;
        image2.fillAmount = value/5f;
        Color newColor = Color.clear;

        switch (alertLevel)
        {
            case AlertLevel.None:
                newColor = Color.clear;
                break;
            case AlertLevel.Low:
                newColor = Color.white;
                break;
            case AlertLevel.Medium:
                newColor = Color.gray;
                break;
            case AlertLevel.High:
                newColor = Color.yellow;
                break;
            case AlertLevel.Extreme:
                newColor = Color.red;
                break;
        }

        image1.color = newColor;
        image2.color = newColor;
    }
}
