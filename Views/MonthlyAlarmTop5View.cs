using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Onthesys;

/// <summary>
/// ���� �˶� TOP5 View - MVVM ����
/// ViewModel���� UnityEvent�� �޾� UI�� ������Ʈ
/// DOTween ������ ����, �⺻ Unity �ִϸ��̼� ���
/// </summary>
public class MonthlyAlarmTop5View : MonoBehaviour
{
    [Header("ViewModel Connection - Inspector���� ���� �巡��")]
    [SerializeField] private MonthlyAlarmTop5ViewModel _viewModel;

    [Header("UI Components - Inspector���� �巡��")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform listPanel;
    [SerializeField] private Image[] chartImages = new Image[5];
    [SerializeField] private TMP_Text centerText;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

        // ��Ʈ ������Ʈ (�ִϸ��̼� ����)
        StartCoroutine(UpdateChartCoroutine(top5Data, totalCount));

        // �߾� �ؽ�Ʈ ������Ʈ
        UpdateCenterText(top5Data, modelProvider);
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

    /// <summary>
    /// ��Ʈ ������Ʈ �ڷ�ƾ (�⺻ Unity �ִϸ��̼� ���)
    /// </summary>
    private IEnumerator UpdateChartCoroutine(List<(int areaId, int count)> top5Data, int totalCount)
    {
        // �����Ͱ� ������ �� ��Ʈ ǥ��
        if (top5Data.Count == 0 || totalCount == 0)
        {
            yield return StartCoroutine(ShowEmptyChartCoroutine());
            yield break;
        }

        // ���� ��Ʈ ������Ʈ
        yield return StartCoroutine(ShowDataChartCoroutine(top5Data, totalCount));
    }

    /// <summary>
    /// �� ��Ʈ ǥ�� (�ִϸ��̼�)
    /// </summary>
    private IEnumerator ShowEmptyChartCoroutine()
    {
        // ù ��° ��Ʈ�� ȸ������ 100% ǥ��
        if (chartImages[0] != null)
        {
            yield return StartCoroutine(AnimateChartFill(chartImages[0], 0f, 1f, Color.gray));
        }

        // ������ ��Ʈ���� 0���� ����
        for (int i = 1; i < chartImages.Length; i++)
        {
            if (chartImages[i] != null)
            {
                chartImages[i].fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// ������ ��Ʈ ǥ�� (�ִϸ��̼�)
    /// </summary>
    private IEnumerator ShowDataChartCoroutine(List<(int areaId, int count)> top5Data, int totalCount)
    {
        const float fillRatioMin = 0.01f;
        float currentRotation = 0f;

        for (int i = 0; i < chartImages.Length; i++)
        {
            if (chartImages[i] == null) continue;

            if (i < top5Data.Count)
            {
                float p = (float)top5Data[i].count / totalCount;
                var targetFillAmount = p < fillRatioMin ? fillRatioMin : p;

                // ȸ�� ����
                chartImages[i].transform.localRotation = Quaternion.Euler(0, 0, currentRotation);

                // Fill Amount �ִϸ��̼�
                StartCoroutine(AnimateChartFill(chartImages[i], 0f, targetFillAmount, chartImages[i].color));

                currentRotation -= (360 * targetFillAmount);

                // ������ �ִϸ��̼��� ���� �ణ�� ���
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // ������ ���� ��Ʈ�� 0���� ����
                chartImages[i].fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// Chart Fill Amount �ִϸ��̼� (�⺻ Unity �ڷ�ƾ)
    /// </summary>
    private IEnumerator AnimateChartFill(Image chartImage, float fromFill, float toFill, Color color)
    {
        if (chartImage == null) yield break;

        chartImage.color = color;
        chartImage.fillAmount = fromFill;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;

            // AnimationCurve ����
            float curveValue = animationCurve.Evaluate(t);
            chartImage.fillAmount = Mathf.Lerp(fromFill, toFill, curveValue);

            yield return null;
        }

        // ������ Ȯ���� ����
        chartImage.fillAmount = toFill;
    }

    private void UpdateCenterText(List<(int areaId, int count)> top5Data, ModelProvider modelProvider)
    {
        if (centerText == null) return;

        if (top5Data.Count > 0)
        {
            // 1�� ���� ���� ǥ��
            var topData = top5Data[0];
            AreaData topArea = modelProvider.GetArea(topData.areaId);

            if (topArea != null)
            {
                centerText.text = $"�ִ�\n{topArea.areaName}\n{topData.count}ȸ";
            }
            else
            {
                centerText.text = $"�ִ�\n����{topData.areaId}\n{topData.count}ȸ";
            }
        }
        else
        {
            // ������ ���� ��
            centerText.text = "������\n����";
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

    /// <summary>
    /// ���� ���ΰ�ħ ��ư Ŭ�� �� ȣ�� (Inspector���� ��ư�� ���� ����)
    /// </summary>
    public void OnRefreshButtonClicked()
    {
        Debug.Log("[MonthlyAlarmTop5View] ���� ���ΰ�ħ ��û");

        if (_viewModel != null)
        {
            // ViewModel�� ���� ������ ���ΰ�ħ ��û
            var dataService = FindObjectOfType<HNS.Services.DataService>();
            if (dataService != null)
            {
                dataService.TriggerDataSync();
            }
        }
    }

    private void OnDestroy()
    {
        // ViewModel �̺�Ʈ ���� ����
        if (_viewModel != null)
        {
            _viewModel.OnTop5DataChanged.RemoveListener(OnTop5DataUpdated);
        }

        // ���� ���� ��� �ڷ�ƾ ����
        StopAllCoroutines();

        Debug.Log("[MonthlyAlarmTop5View] ���� �Ϸ�");
    }

    private void OnValidate()
    {
        if (_viewModel == null)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] ViewModel�� ������� �ʾҽ��ϴ�: {gameObject.name}");
        }

        if (chartImages != null && chartImages.Length != 5)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] Chart Images �迭 ũ�Ⱑ 5�� �ƴմϴ�: {chartImages.Length}");
        }
    }

    /// <summary>
    /// ����׿� �׽�Ʈ �޼���
    /// </summary>
    [ContextMenu("�׽�Ʈ �����ͷ� ������Ʈ")]
    public void TestWithDummyData()
    {
        var testData = new List<(int, int)>
        {
            (1, 25), (2, 18), (3, 12), (4, 9), (5, 7)
        };

        OnTop5DataUpdated(testData, 71);
    }
}

/// <summary>
/// ���� ���� �˶� ������ �� - DOTween ������ ���� ����
/// </summary>
[System.Serializable]
public class MonthlyAlarmItemView : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text rankText;
    public TMP_Text areaNameText;
    public TMP_Text alarmCountText;
    public TMP_Text percentageText;
    public Image backgroundImage;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        // CanvasGroup�� ������ �߰�
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// ������ ������ ����
    /// </summary>
    public void SetAreaData(Color chartColor, int areaId, string areaName, int alarmCount, float percentage)
    {
        // ���� �ִϸ��̼� �ߴ�
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        if (areaId <= 0 || alarmCount <= 0)
        {
            SetEmptyData();
            return;
        }

        // UI ������ ����
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

        // ������Ʈ Ȱ��ȭ
        gameObject.SetActive(true);

        // ���̵� �� �ִϸ��̼� ����
        _fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// �� ������ ����
    /// </summary>
    public void SetEmptyData()
    {
        if (rankText != null)
            rankText.text = "-";

        if (areaNameText != null)
            areaNameText.text = "������ ����";

        if (alarmCountText != null)
            alarmCountText.text = "00";

        if (percentageText != null)
            percentageText.text = "0%";

        if (backgroundImage != null)
            backgroundImage.color = Color.gray;

        // ���������� ����
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.3f;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// ���̵� �� �ִϸ��̼� (�⺻ Unity �ڷ�ƾ)
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void OnDestroy()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
    }
}