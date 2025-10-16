using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorB;

public class PopupAlarmDetailItemView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text txtSensorName;
    [SerializeField] private TMP_Text txtCurrentValue;
    [SerializeField] private TMP_Text txtUnit;
    [SerializeField] private Image imgStatus;

    public void SetData(AlarmSensorData data)
    {
        if (txtSensorName != null)
        {
            txtSensorName.text = data.SensorName;
        }

        if (txtCurrentValue != null)
        {
            txtCurrentValue.text = data.CurrentValue.ToString("F2");
        }

        if (txtUnit != null)
        {
            txtUnit.text = data.Unit;
        }

        if (imgStatus != null)
        {
            imgStatus.color = GetStatusColor(data.Status);
        }
    }

    private Color GetStatusColor(SensorStatus status)
    {
        return status switch
        {
            SensorStatus.Normal => Color.green,
            SensorStatus.Warning => Color.yellow,
            SensorStatus.Critical => Color.red,
            SensorStatus.Error => new Color(0.5f, 0f, 0.5f),
            _ => Color.white
        };
    }
}