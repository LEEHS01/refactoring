using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Onthesys;

/// <summary>
/// 월간 알람 TOP5 View - MVVM 패턴
/// ViewModel에서 UnityEvent를 받아 UI를 업데이트
/// DOTween 의존성 제거, 기본 Unity 애니메이션 사용
/// </summary>
public class MonthlyAlarmTop5View : MonoBehaviour
{
    [Header("ViewModel Connection - Inspector에서 에셋 드래그")]
    [SerializeField] private MonthlyAlarmTop5ViewModel _viewModel;

    [Header("UI Components - Inspector에서 드래그")]
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

        Debug.Log("[MonthlyAlarmTop5View] 초기화 시작...");

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

        // 차트 업데이트 (애니메이션 포함)
        StartCoroutine(UpdateChartCoroutine(top5Data, totalCount));

        // 중앙 텍스트 업데이트
        UpdateCenterText(top5Data, modelProvider);
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

    /// <summary>
    /// 차트 업데이트 코루틴 (기본 Unity 애니메이션 사용)
    /// </summary>
    private IEnumerator UpdateChartCoroutine(List<(int areaId, int count)> top5Data, int totalCount)
    {
        // 데이터가 없으면 빈 차트 표시
        if (top5Data.Count == 0 || totalCount == 0)
        {
            yield return StartCoroutine(ShowEmptyChartCoroutine());
            yield break;
        }

        // 정상 차트 업데이트
        yield return StartCoroutine(ShowDataChartCoroutine(top5Data, totalCount));
    }

    /// <summary>
    /// 빈 차트 표시 (애니메이션)
    /// </summary>
    private IEnumerator ShowEmptyChartCoroutine()
    {
        // 첫 번째 차트만 회색으로 100% 표시
        if (chartImages[0] != null)
        {
            yield return StartCoroutine(AnimateChartFill(chartImages[0], 0f, 1f, Color.gray));
        }

        // 나머지 차트들은 0으로 설정
        for (int i = 1; i < chartImages.Length; i++)
        {
            if (chartImages[i] != null)
            {
                chartImages[i].fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// 데이터 차트 표시 (애니메이션)
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

                // 회전 설정
                chartImages[i].transform.localRotation = Quaternion.Euler(0, 0, currentRotation);

                // Fill Amount 애니메이션
                StartCoroutine(AnimateChartFill(chartImages[i], 0f, targetFillAmount, chartImages[i].color));

                currentRotation -= (360 * targetFillAmount);

                // 순차적 애니메이션을 위한 약간의 대기
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // 데이터 없는 차트는 0으로 설정
                chartImages[i].fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// Chart Fill Amount 애니메이션 (기본 Unity 코루틴)
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

            // AnimationCurve 적용
            float curveValue = animationCurve.Evaluate(t);
            chartImage.fillAmount = Mathf.Lerp(fromFill, toFill, curveValue);

            yield return null;
        }

        // 최종값 확실히 설정
        chartImage.fillAmount = toFill;
    }

    private void UpdateCenterText(List<(int areaId, int count)> top5Data, ModelProvider modelProvider)
    {
        if (centerText == null) return;

        if (top5Data.Count > 0)
        {
            // 1위 지역 정보 표시
            var topData = top5Data[0];
            AreaData topArea = modelProvider.GetArea(topData.areaId);

            if (topArea != null)
            {
                centerText.text = $"최다\n{topArea.areaName}\n{topData.count}회";
            }
            else
            {
                centerText.text = $"최다\n지역{topData.areaId}\n{topData.count}회";
            }
        }
        else
        {
            // 데이터 없을 때
            centerText.text = "데이터\n없음";
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

    /// <summary>
    /// 수동 새로고침 버튼 클릭 시 호출 (Inspector에서 버튼과 연결 가능)
    /// </summary>
    public void OnRefreshButtonClicked()
    {
        Debug.Log("[MonthlyAlarmTop5View] 수동 새로고침 요청");

        if (_viewModel != null)
        {
            // ViewModel을 통해 데이터 새로고침 요청
            var dataService = FindObjectOfType<HNS.Services.DataService>();
            if (dataService != null)
            {
                dataService.TriggerDataSync();
            }
        }
    }

    private void OnDestroy()
    {
        // ViewModel 이벤트 구독 해제
        if (_viewModel != null)
        {
            _viewModel.OnTop5DataChanged.RemoveListener(OnTop5DataUpdated);
        }

        // 실행 중인 모든 코루틴 정지
        StopAllCoroutines();

        Debug.Log("[MonthlyAlarmTop5View] 정리 완료");
    }

    private void OnValidate()
    {
        if (_viewModel == null)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] ViewModel이 연결되지 않았습니다: {gameObject.name}");
        }

        if (chartImages != null && chartImages.Length != 5)
        {
            Debug.LogWarning($"[MonthlyAlarmTop5View] Chart Images 배열 크기가 5가 아닙니다: {chartImages.Length}");
        }
    }

    /// <summary>
    /// 디버그용 테스트 메서드
    /// </summary>
    [ContextMenu("테스트 데이터로 업데이트")]
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
/// 개별 월간 알람 아이템 뷰 - DOTween 의존성 제거 버전
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
        // CanvasGroup이 없으면 추가
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
    /// 아이템 데이터 설정
    /// </summary>
    public void SetAreaData(Color chartColor, int areaId, string areaName, int alarmCount, float percentage)
    {
        // 기존 애니메이션 중단
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        if (areaId <= 0 || alarmCount <= 0)
        {
            SetEmptyData();
            return;
        }

        // UI 데이터 설정
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

        // 오브젝트 활성화
        gameObject.SetActive(true);

        // 페이드 인 애니메이션 시작
        _fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// 빈 데이터 설정
    /// </summary>
    public void SetEmptyData()
    {
        if (rankText != null)
            rankText.text = "-";

        if (areaNameText != null)
            areaNameText.text = "데이터 없음";

        if (alarmCountText != null)
            alarmCountText.text = "00";

        if (percentageText != null)
            percentageText.text = "0%";

        if (backgroundImage != null)
            backgroundImage.color = Color.gray;

        // 반투명으로 설정
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.3f;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 페이드 인 애니메이션 (기본 Unity 코루틴)
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