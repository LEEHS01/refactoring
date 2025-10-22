using Common.UI;
using Models.MonitorB;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupAlarmDetailItemView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text txtSensorName;
    [SerializeField] private TMP_Text txtCurrentValue;
    [SerializeField] private TMP_Text txtUnit;
    [SerializeField] private Image imgStatus;

    [Header("Chart Configuration")]
    [SerializeField] private RectTransform chartBoundsArea; 

    private ChartLineRenderer chartRenderer;
    private void Awake()
    {
        Debug.Log($"🔥 Awake 호출: {gameObject.name}");

        // ChartLineRenderer 자동 탐색
        chartRenderer = GetComponentInChildren<ChartLineRenderer>();

        if (chartRenderer == null)
        {
            Debug.LogError($"❌ ChartLineRenderer를 찾을 수 없음!");
        }
        else
        {
            Debug.Log($"✅ ChartLineRenderer 발견");
        }

        if (chartBoundsArea == null)
        {
            Debug.LogError($"❌ chartBoundsArea가 null!");
        }
        else
        {
            Debug.Log($"✅ chartBoundsArea 발견: {chartBoundsArea.name}");
        }

        if (chartRenderer != null && chartBoundsArea != null)
        {
            chartRenderer.Initialize(chartBoundsArea);
            Debug.Log($"✅ ChartRenderer Initialize 완료");
        }
    }

    public void SetData(AlarmSensorData data)
    {
        Debug.Log($"🔍 SetData 호출됨 - 센서: {data.SensorName}, 값: {data.CurrentValue}, 차트데이터: {data.ChartValues.Count}개");

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

        // ⭐ 추가: 차트 그리기
        UpdateMiniChart(data);
    }

    /// <summary>
    /// 미니 차트 업데이트
    /// </summary>
    private void UpdateMiniChart(AlarmSensorData data)
    {
        Debug.Log($"🔍 UpdateMiniChart 시작: {data.SensorName}");

        if (chartRenderer == null)
        {
            Debug.LogError($"❌ {data.SensorName}: ChartRenderer가 null!");
            return;
        }

        Debug.Log($"📊 차트 데이터 개수: {data.ChartValues.Count}");

        if (data.ChartValues == null || data.ChartValues.Count == 0)
        {
            Debug.LogWarning($"⚠️ {data.SensorName}: 차트 데이터 없음");
            return;
        }

        // 값 출력
        Debug.Log($"📊 차트 값들: {string.Join(", ", data.ChartValues.Take(5))}... (처음 5개)");

        // 최대값 기준으로 정규화
        float max = data.ChartValues.Max();
        Debug.Log($"📊 최대값: {max}");

        if (max <= 0) max = 1f;

        var normalizedValues = data.ChartValues.Select(v => v / max).ToList();
        Debug.Log($"📊 정규화된 값들: {string.Join(", ", normalizedValues.Take(5))}... (처음 5개)");

        // 차트 그리기
        Debug.Log($"🎨 chartRenderer.UpdateChart 호출!");
        chartRenderer.UpdateChart(normalizedValues);

        Debug.Log($"✅ {data.SensorName}: 차트 그림 완료");
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