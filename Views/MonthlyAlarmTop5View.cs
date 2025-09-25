using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Onthesys;

/// <summary>
/// 월간 알람 TOP5 View - MVVM 패턴
/// ViewModel에서 UnityEvent를 받아 UI를 업데이트
/// </summary>
public class MonthlyAlarmTop5View : MonoBehaviour
{
    [Header("UI Components - Inspector에서 드래그")]
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

        Debug.Log("[MonthlyAlarmTop5View] 초기화 시작...");
        _viewModel = GetComponent<MonthlyAlarmTop5ViewModel>();

        // ViewModel 유효성 검사
        if (_viewModel == null)
        {
            Debug.LogError("[MonthlyAlarmTop5View] ViewModel이 Inspector에서 연결되지 않았습니다!");
            return;
        }

        // UI 컴포넌트 찾기
        InitializeUIComponents();

        // ViewModel 이벤트 구독
        _viewModel.OnTop5DataChanged.AddListener(OnTop5DataUpdated);

        // 제목 설정
        SetCurrentMonthTitle();

        _isInitialized = true;
        Debug.Log("[MonthlyAlarmTop5View] 초기화 완료");
    }

    private void InitializeUIComponents()
    {
        // 리스트 패널에서 아이템 뷰들 찾기
        if (listPanel != null)
        {
            var foundItems = listPanel.GetComponentsInChildren<MonthlyAlarmItemView>();
            for (int i = 0; i < Math.Min(foundItems.Length, 5); i++)
            {
                items[i] = foundItems[i];
                if (items[i] != null)
                {
                    Debug.Log($"[MonthlyAlarmTop5View] 아이템 {i} 연결됨: {items[i].name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[MonthlyAlarmTop5View] List Panel이 연결되지 않았습니다.");
        }

        // 차트 이미지 유효성 검사
        for (int i = 0; i < chartImages.Length; i++)
        {
            if (chartImages[i] == null)
            {
                Debug.LogWarning($"[MonthlyAlarmTop5View] Chart Image {i}가 연결되지 않았습니다.");
            }
        }
    }

    /// <summary>
    /// ViewModel에서 데이터 변경 시 호출되는 메서드 (UnityEvent)
    /// </summary>
    private void OnTop5DataUpdated(List<(int areaId, int count)> top5Data, int totalCount)
    {
        Debug.Log($"[MonthlyAlarmTop5View] TOP5 데이터 업데이트: {top5Data.Count}개 지역, 총 {totalCount}개 알람");

        if (!_isInitialized)
        {
            Debug.LogWarning("[MonthlyAlarmTop5View] 아직 초기화되지 않았습니다.");
            return;
        }

        // ModelProvider에서 지역 정보 가져오기 (ViewModel에서 이미 검증됨)
        var modelProvider = UiManager.Instance?.modelProvider;
        if (modelProvider == null)
        {
            Debug.LogError("[MonthlyAlarmTop5View] ModelProvider에 접근할 수 없습니다.");
            return;
        }

        // 리스트 아이템 업데이트
        UpdateListItems(top5Data, totalCount, modelProvider);

        // 차트 업데이트
        UpdateChart(top5Data, totalCount);
    }

    private void UpdateListItems(List<(int areaId, int count)> top5Data, int totalCount, ModelProvider modelProvider)
    {
        int sum = Math.Max(totalCount, 1); // 0으로 나누기 방지

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
                        Debug.LogWarning($"[MonthlyAlarmTop5View] 지역 ID {data.areaId}를 찾을 수 없습니다.");
                        items[i].SetAreaData(Color.gray, -1, "지역없음", data.count, 0);
                    }
                }
                else
                {
                    // 빈 데이터
                    items[i].SetAreaData(Color.gray, -1, "-", 0, 0);
                }
            }
        }
    }

    private void UpdateChart(List<(int areaId, int count)> top5Data, int totalCount)
    {
        // 데이터가 없으면 회색 원형으로 표시
        if (top5Data.Count == 0 || totalCount == 0)
        {
            ShowEmptyChart();
            return;
        }

        // 정상 차트 업데이트
        ShowDataChart(top5Data, totalCount);
    }

    private void ShowEmptyChart()
    {
        // 첫 번째 차트만 회색으로 100% 표시
        if (chartImages[0] != null)
        {
            chartImages[0].color = Color.gray;
            chartImages[0].DOFillAmount(1f, 1f);
        }

        // 나머지 차트들은 숨김
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
                // 데이터 없는 차트는 0으로 설정
                chartImages[i].DOFillAmount(0f, duration);
            }
        }
    }

    private void SetCurrentMonthTitle()
    {
        if (titleText != null)
        {
            DateTime now = DateTime.Now;
            string monthText = $"{now.Month}월 최다 알람 발생 TOP 5";
            titleText.text = monthText;
        }
    }

    private void OnDestroy()
    {
        // ViewModel 이벤트 구독 해제
        if (_viewModel != null)
        {
            _viewModel.OnTop5DataChanged.RemoveListener(OnTop5DataUpdated);
        }

        // DOTween 정리
        DOTween.Kill(this);

        Debug.Log("[MonthlyAlarmTop5View] 정리 완료");
    }

    private void OnValidate()
    {
        if (_viewModel == null)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] ViewModel이 연결되지 않았습니다: {gameObject.name}");
        }
    }
}

/// <summary>
/// 개별 월간 알람 아이템 뷰
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
    /// 아이템 데이터 설정
    /// </summary>
    public void SetAreaData(Color chartColor, int areaId, string areaName, int alarmCount, float percentage)
    {
        if (rankText != null)
            rankText.text = ""; // 순위는 UI 위치로 표현

        if (areaNameText != null)
            areaNameText.text = areaName;

        if (alarmCountText != null)
            alarmCountText.text = alarmCount.ToString("00");

        if (percentageText != null)
            percentageText.text = $"{(percentage * 100):F0}%";

        if (backgroundImage != null)
            backgroundImage.color = chartColor;

        // 빈 데이터 처리
        gameObject.SetActive(areaId > 0 && alarmCount > 0);
    }
}