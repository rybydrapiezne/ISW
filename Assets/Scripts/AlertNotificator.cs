using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AICore;

[RequireComponent(typeof(Image))]
public class AlertNotificator : MonoBehaviour
{
    Image image;
    private Dictionary<AICore, AlertData> activeAlerts = new Dictionary<AICore, AlertData>();

    private struct AlertData
    {
        public float tm;
        public AlertLevel level;
    }

    void Awake()
    {
        image = GetComponent<Image>();
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
            image.enabled = false;
            return;
        }

        image.enabled = true;

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
        image.fillAmount = value;

        switch (alertLevel)
        {
            case AlertLevel.None:
                image.color = Color.clear;
                break;
            case AlertLevel.Low:
                image.color = Color.white;
                break;
            case AlertLevel.Medium:
                image.color = Color.gray;
                break;
            case AlertLevel.High:
                image.color = Color.yellow;
                break;
            case AlertLevel.Extreme:
                image.color = Color.red;
                break;
        }
    }
}
