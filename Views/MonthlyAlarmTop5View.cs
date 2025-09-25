using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Onthesys;

/// <summary>
/// ���� �˶� TOP5 View - MVVM ����
/// ViewModel���� UnityEvent�� �޾� UI�� ������Ʈ
/// </summary>
public class MonthlyAlarmTop5View : MonoBehaviour
{
    [Header("UI Components - Inspector���� �巡��")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform listPanel;
    [SerializeField] private Image[] chartImages = new Image[5];

    private MonthlyAlarmTop5ViewModel _viewModel;
    private MonthlyAlarmItemView[] items = new MonthlyAlarmItemView[5];
    private bool _isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        Debug.Log("[MonthlyAlarmTop5View] �ʱ�ȭ ����...");
        _viewModel = GetComponent<MonthlyAlarmTop5ViewModel>();

        // ViewModel ��ȿ�� �˻�
        if (_viewModel == null)
        {
            Debug.LogError("[MonthlyAlarmTop5View] ViewModel�� Inspector���� ������� �ʾҽ��ϴ�!");
            return;
        }

        // UI ������Ʈ ã��
        InitializeUIComponents();

        // ViewModel �̺�Ʈ ����
        _viewModel.OnTop5DataChanged.AddListener(OnTop5DataUpdated);

        // ���� ����
        SetCurrentMonthTitle();

        _isInitialized = true;
        Debug.Log("[MonthlyAlarmTop5View] �ʱ�ȭ �Ϸ�");
    }

    private void InitializeUIComponents()
    {
        // ����Ʈ �гο��� ������ ��� ã��
        if (listPanel != null)
        {
            var foundItems = listPanel.GetComponentsInChildren<MonthlyAlarmItemView>();
            for (int i = 0; i < Math.Min(foundItems.Length, 5); i++)
            {
                items[i] = foundItems[i];
                if (items[i] != null)
                {
                    Debug.Log($"[MonthlyAlarmTop5View] ������ {i} �����: {items[i].name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[MonthlyAlarmTop5View] List Panel�� ������� �ʾҽ��ϴ�.");
        }

        // ��Ʈ �̹��� ��ȿ�� �˻�
        for (int i = 0; i < chartImages.Length; i++)
        {
            if (chartImages[i] == null)
            {
                Debug.LogWarning($"[MonthlyAlarmTop5View] Chart Image {i}�� ������� �ʾҽ��ϴ�.");
            }
        }
    }

    /// <summary>
    /// ViewModel���� ������ ���� �� ȣ��Ǵ� �޼��� (UnityEvent)
    /// </summary>
    private void OnTop5DataUpdated(List<(int areaId, int count)> top5Data, int totalCount)
    {
        Debug.Log($"[MonthlyAlarmTop5View] TOP5 ������ ������Ʈ: {top5Data.Count}�� ����, �� {totalCount}�� �˶�");

        if (!_isInitialized)
        {
            Debug.LogWarning("[MonthlyAlarmTop5View] ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return;
        }

        // ModelProvider���� ���� ���� �������� (ViewModel���� �̹� ������)
        var modelProvider = UiManager.Instance?.modelProvider;
        if (modelProvider == null)
        {
            Debug.LogError("[MonthlyAlarmTop5View] ModelProvider�� ������ �� �����ϴ�.");
            return;
        }

        // ����Ʈ ������ ������Ʈ
        UpdateListItems(top5Data, totalCount, modelProvider);

        // ��Ʈ ������Ʈ
        UpdateChart(top5Data, totalCount);
    }

    private void UpdateListItems(List<(int areaId, int count)> top5Data, int totalCount, ModelProvider modelProvider)
    {
        int sum = Math.Max(totalCount, 1); // 0���� ������ ����

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                if (i < top5Data.Count)
                {
                    var data = top5Data[i];
                    AreaData area = modelProvider.GetArea(data.areaId);

                    if (area != null)
                    {
                        float percent = (float)data.count / sum;
                        Color chartColor = i < chartImages.Length && chartImages[i] != null ? chartImages[i].color : Color.cyan;

                        items[i].SetAreaData(chartColor, area.areaId, area.areaName, data.count, percent);
                    }
                    else
                    {
                        Debug.LogWarning($"[MonthlyAlarmTop5View] ���� ID {data.areaId}�� ã�� �� �����ϴ�.");
                        items[i].SetAreaData(Color.gray, -1, "��������", data.count, 0);
                    }
                }
                else
                {
                    // �� ������
                    items[i].SetAreaData(Color.gray, -1, "-", 0, 0);
                }
            }
        }
    }

    private void UpdateChart(List<(int areaId, int count)> top5Data, int totalCount)
    {
        // �����Ͱ� ������ ȸ�� �������� ǥ��
        if (top5Data.Count == 0 || totalCount == 0)
        {
            ShowEmptyChart();
            return;
        }

        // ���� ��Ʈ ������Ʈ
        ShowDataChart(top5Data, totalCount);
    }

    private void ShowEmptyChart()
    {
        // ù ��° ��Ʈ�� ȸ������ 100% ǥ��
        if (chartImages[0] != null)
        {
            chartImages[0].color = Color.gray;
            chartImages[0].DOFillAmount(1f, 1f);
        }

        // ������ ��Ʈ���� ����
        for (int i = 1; i < chartImages.Length; i++)
        {
            if (chartImages[i] != null)
            {
                chartImages[i].DOFillAmount(0f, 1f);
            }
        }
    }

    private void ShowDataChart(List<(int areaId, int count)> top5Data, int totalCount)
    {
        const float fillRatioMin = 0.01f;
        var duration = 1f;
        var rotation = fillRatioMin;

        for (int i = 0; i < chartImages.Length; i++)
        {
            if (chartImages[i] == null) continue;

            if (i < top5Data.Count)
            {
                float p = (float)top5Data[i].count / totalCount;
                var setPercent = p < fillRatioMin ? fillRatioMin : p;

                chartImages[i].DOFillAmount(setPercent, duration);
                chartImages[i].transform.DOLocalRotate(new Vector3(0, 0, rotation), duration);

                rotation -= (360 * setPercent);
            }
            else
            {
                // ������ ���� ��Ʈ�� 0���� ����
                chartImages[i].DOFillAmount(0f, duration);
            }
        }
    }

    private void SetCurrentMonthTitle()
    {
        if (titleText != null)
        {
            DateTime now = DateTime.Now;
            string monthText = $"{now.Month}�� �ִ� �˶� �߻� TOP 5";
            titleText.text = monthText;
        }
    }

    private void OnDestroy()
    {
        // ViewModel �̺�Ʈ ���� ����
        if (_viewModel != null)
        {
            _viewModel.OnTop5DataChanged.RemoveListener(OnTop5DataUpdated);
        }

        // DOTween ����
        DOTween.Kill(this);

        Debug.Log("[MonthlyAlarmTop5View] ���� �Ϸ�");
    }

    private void OnValidate()
    {
        if (_viewModel == null)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] ViewModel�� ������� �ʾҽ��ϴ�: {gameObject.name}");
        }
    }
}

/// <summary>
/// ���� ���� �˶� ������ ��
/// </summary>
public class MonthlyAlarmItemView : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text rankText;
    public TMP_Text areaNameText;
    public TMP_Text alarmCountText;
    public TMP_Text percentageText;
    public Image backgroundImage;

    /// <summary>
    /// ������ ������ ����
    /// </summary>
    public void SetAreaData(Color chartColor, int areaId, string areaName, int alarmCount, float percentage)
    {
        if (rankText != null)
            rankText.text = ""; // ������ UI ��ġ�� ǥ��

        if (areaNameText != null)
            areaNameText.text = areaName;

        if (alarmCountText != null)
            alarmCountText.text = alarmCount.ToString("00");

        if (percentageText != null)
            percentageText.text = $"{(percentage * 100):F0}%";

        if (backgroundImage != null)
            backgroundImage.color = chartColor;

        // �� ������ ó��
        gameObject.SetActive(areaId > 0 && alarmCount > 0);
    }
}