using UnityEngine;
using UnityEngine.UI;
using static AICore;

[RequireComponent(typeof(Image))]
public class AlertNotificator : MonoBehaviour
{
    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void SetAlertProgress(float value, AlertLevel alertLevel)
    {
        value = Mathf.Clamp(value, 0, 1);
        image.fillAmount = value;

        switch (alertLevel)
        {
            case AlertLevel.None:
                image.color = Color.gray;
                break;
            case AlertLevel.Low:
                image.color = Color.white;
                break;
            case AlertLevel.Medium:
                image.color = Color.yellow;
                break;
            case AlertLevel.High:
                image.color = Color.orange;
                break;
            case AlertLevel.Extreme:
                image.color = Color.red;
                break;
        }
    }
}
