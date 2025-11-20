using Core;
using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts_refactoring.Views.MonitorA
{
    /// <summary>
    /// 지역 알람 차트 View (12개월 히스토그램 + 범례)
    /// Monitor A 지역 상세 화면에서 표시
    /// ✅ MapAreaView 패턴 적용: GameObject는 항상 active, CanvasGroup으로 표시/숨김
    /// ⚡ DOTween 제거: 즉시 렌더링 방식으로 성능 최적화
    /// </summary>
    public class AreaAlarmChartView : BaseView
    {
        #region Inspector 설정

        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Chart Container")]
        [SerializeField] private RectTransform chartRectTransform;

        [Header("Title")]
        [SerializeField] private TMP_Text txtChartTitle; // "O O 지역 알람 발생"

        [Header("Legend (범례)")]
        [SerializeField] private TMP_Text txtLegendA; // "관측소1"
        [SerializeField] private TMP_Text txtLegendB; // "관측소2"
        [SerializeField] private TMP_Text txtLegendC; // "관측소3"

        [Header("12 Month Labels")]
        [SerializeField] private List<TMP_Text> lblMonthTexts; // Panel_Month의 12개 Text

        [Header("Y-Axis Labels")]
        [SerializeField] private List<TMP_Text> lblCountValues; // Panel_Count의 값

        [Header("12 Month Histogram")]
        [SerializeField] private Transform histogramParent; // Panel_Histogram

        [Header("Position Settings")]
        [SerializeField] private float slideDistance = 400f;

        #endregion

        #region Private Fields

        private Vector3 _visiblePosition;
        private Vector3 _hiddenPosition;
        private List<Transform> _histogramMonths; // 12개월 Transform 캐싱

        #endregion

        #region Unity Lifecycle Override

        // ⭐ BaseView의 OnDisable을 오버라이드하여 이벤트 구독 유지
        protected override void OnDisable()
        {
            LogInfo("OnDisable 호출 - 이벤트 구독 유지 (오버라이드)");
            // ❌ base.OnDisable() 호출하지 않음!
            // 이벤트 구독을 해제하지 않고 유지
        }

        #endregion

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            LogInfo("========================================");
            LogInfo("=== InitializeUIComponents 시작 ===");
            LogInfo($"GameObject 이름: {gameObject.name}");
            LogInfo($"GameObject 활성화 상태: {gameObject.activeSelf}");
            LogInfo("========================================");

            // Inspector 연결 검증
            bool isValid = ValidateComponents(
                (chartRectTransform, "chartRectTransform"),
                (txtChartTitle, "txtChartTitle"),
                (txtLegendA, "txtLegendA"),
                (txtLegendB, "txtLegendB"),
                (txtLegendC, "txtLegendC"),
                (histogramParent, "histogramParent")
            );

            if (!isValid)
            {
                LogError("필수 컴포넌트가 연결되지 않았습니다!");
                return;
            }

            LogInfo($"chartRectTransform 연결됨: {chartRectTransform.name}");

            // CanvasGroup 자동 추가 (MapAreaView와 동일)
            if (canvasGroup == null)
            {
                LogInfo("CanvasGroup이 null입니다. 자동 추가 시도...");
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가 완료!");
                }
                else
                {
                    LogInfo("기존 CanvasGroup 발견!");
                }
            }
            else
            {
                LogInfo("CanvasGroup이 Inspector에서 연결되어 있음");
            }

            // 월 라벨 검증
            if (lblMonthTexts == null || lblMonthTexts.Count != 12)
            {
                LogError($"월 라벨이 12개가 아닙니다! 현재: {lblMonthTexts?.Count ?? 0}개");
            }

            // Y축 라벨 검증
            if (lblCountValues == null || lblCountValues.Count == 0)
            {
                LogWarning("Y축 라벨이 설정되지 않았습니다!");
            }

            // 12개월 Transform 캐싱
            _histogramMonths = new List<Transform>();
            if (histogramParent != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (i < histogramParent.childCount)
                    {
                        _histogramMonths.Add(histogramParent.GetChild(i));
                    }
                }

                if (_histogramMonths.Count != 12)
                {
                    LogError($"히스토그램 개수 오류! 예상: 12개, 실제: {_histogramMonths.Count}개");
                }
            }

            // 초기 위치 설정
            _visiblePosition = chartRectTransform.anchoredPosition;
            _hiddenPosition = _visiblePosition + new Vector3(0f, slideDistance, 0f);

            LogInfo($"현재 위치: {chartRectTransform.anchoredPosition}");
            LogInfo($"보이는 위치: {_visiblePosition}");
            LogInfo($"숨김 위치: {_hiddenPosition}");

            // GameObject는 active 유지, CanvasGroup으로 숨김
            HideChart();

            LogInfo($"CanvasGroup 상태 - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}");
            LogInfo($"차트 초기화 완료 (GameObject active: {gameObject.activeSelf})");
            LogInfo("========================================");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("SetupViewEvents 호출");
            // View 자체 이벤트 없음 (ViewModel이 모두 제어)
        }

        protected override void ConnectToViewModel()
        {
            LogInfo("========================================");
            LogInfo("ConnectToViewModel 시작");

            if (AreaAlarmChartViewModel.Instance == null)
            {
                LogError("AreaAlarmChartViewModel.Instance가 null입니다!");
                return;
            }

            LogInfo("AreaAlarmChartViewModel.Instance 발견!");

            // ViewModel 이벤트 구독
            AreaAlarmChartViewModel.Instance.OnAreaEntered.AddListener(OnAreaEntered);
            AreaAlarmChartViewModel.Instance.OnAreaExited.AddListener(OnAreaExited);
            AreaAlarmChartViewModel.Instance.OnChartDataLoaded.AddListener(OnChartDataLoaded);
            AreaAlarmChartViewModel.Instance.OnError.AddListener(OnError);

            LogInfo("AreaAlarmChartViewModel 이벤트 구독 완료");

            // ⭐ Area3DViewModel 이벤트 구독
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryOpened);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                LogInfo("Area3DViewModel 이벤트 구독 완료");
            }
            else
            {
                LogWarning("Area3DViewModel.Instance가 null입니다!");
            }

            LogInfo("========================================");
        }

        protected override void DisconnectFromViewModel()
        {
            if (AreaAlarmChartViewModel.Instance != null)
            {
                AreaAlarmChartViewModel.Instance.OnAreaEntered.RemoveListener(OnAreaEntered);
                AreaAlarmChartViewModel.Instance.OnAreaExited.RemoveListener(OnAreaExited);
                AreaAlarmChartViewModel.Instance.OnChartDataLoaded.RemoveListener(OnChartDataLoaded);
                AreaAlarmChartViewModel.Instance.OnError.RemoveListener(OnError);
            }

            // ⭐ Area3DViewModel 이벤트 구독 해제 추가!
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryOpened);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
                LogInfo("Area3DViewModel 이벤트 구독 해제 완료");
            }
        }

        protected override void DisconnectViewEvents()
        {
            // View 자체 이벤트 없음
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        /// <summary>
        /// 지역 진입 → 차트 즉시 표시 (애니메이션 제거)
        /// </summary>
        private void OnAreaEntered(int areaId)
        {
            LogInfo("========================================");
            LogInfo($"OnAreaEntered 호출됨! AreaId={areaId}");

            if (canvasGroup == null)
            {
                LogError("CanvasGroup이 null입니다!");
                return;
            }

            // ⚡ 이전 차트 데이터 즉시 초기화 (렌더링 지연 방지)
            ClearChartData();

            // ⚡ 즉시 표시 (애니메이션 제거)
            chartRectTransform.anchoredPosition = _visiblePosition;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            LogInfo("차트 즉시 표시 완료!");
        }

        /// <summary>
        /// 지역 퇴장 → 차트 즉시 숨김 (애니메이션 제거)
        /// </summary>
        private void OnAreaExited()
        {
            LogInfo("OnAreaExited 호출됨!");

            if (canvasGroup == null)
            {
                LogError("CanvasGroup이 null입니다!");
                return;
            }

            // ⚡ 즉시 숨김 (애니메이션 제거)
            chartRectTransform.anchoredPosition = _hiddenPosition;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            LogInfo("차트 즉시 숨김 완료!");
        }

        /// <summary>
        /// 관측소 열림 → 차트 즉시 숨김
        /// </summary>
        private void OnObservatoryOpened(int obsId)
        {
            LogInfo($"관측소 열림: ObsId={obsId} - 차트 즉시 숨김");
            HideChart();
        }

        /// <summary>
        /// 관측소 닫힘 → 차트 즉시 다시 표시
        /// </summary>
        private void OnObservatoryClosed()
        {
            LogInfo("관측소 닫힘 - 차트 즉시 표시");
            ShowChart();
        }

        /// <summary>
        /// 차트 데이터 로드 완료 → 데이터 업데이트
        /// </summary>
        private void OnChartDataLoaded(AreaChartData data)
        {
            if (data == null)
            {
                LogError("차트 데이터가 null입니다!");
                return;
            }

            LogInfo($"차트 데이터 수신: {data.AreaName}");
            UpdateChartData(data);
        }

        /// <summary>
        /// 에러 핸들러
        /// </summary>
        private void OnError(string errorMessage)
        {
            LogError($"ViewModel 에러: {errorMessage}");
        }

        #endregion

        #region 차트 표시/숨김

        /// <summary>
        /// 차트 즉시 표시 (애니메이션 제거)
        /// </summary>
        private void ShowChart()
        {
            if (canvasGroup == null)
            {
                LogError("CanvasGroup이 null입니다!");
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            LogInfo("차트 즉시 표시 완료");
        }

        /// <summary>
        /// 차트 즉시 숨김 (CanvasGroup 사용)
        /// </summary>
        private void HideChart()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("HideChart() 실행 완료");
            }
            else
            {
                LogError("HideChart() 실행 실패: CanvasGroup이 null!");
            }
        }

        #endregion

        #region 차트 초기화

        /// <summary>
        /// 차트 데이터 즉시 초기화 (지역 전환 시 이전 데이터 제거)
        /// </summary>
        private void ClearChartData()
        {
            LogInfo("차트 데이터 초기화 시작");

            // 1. 타이틀 초기화
            if (txtChartTitle != null)
            {
                txtChartTitle.text = "로딩 중...";
            }

            // 2. 범례 초기화
            if (txtLegendA != null) txtLegendA.text = "-";
            if (txtLegendB != null) txtLegendB.text = "-";
            if (txtLegendC != null) txtLegendC.text = "-";

            // 3. 월 라벨 초기화
            if (lblMonthTexts != null)
            {
                foreach (var label in lblMonthTexts)
                {
                    if (label != null) label.text = "";
                }
            }

            // 4. Y축 라벨 초기화
            if (lblCountValues != null)
            {
                foreach (var label in lblCountValues)
                {
                    if (label != null) label.text = "0";
                }
            }

            // 5. 모든 막대 초기화 (scaleY = 0)
            if (_histogramMonths != null)
            {
                foreach (var month in _histogramMonths)
                {
                    ResetMonthBars(month);
                }
            }

            LogInfo("차트 데이터 초기화 완료");
        }

        #endregion

        #region 차트 업데이트

        /// <summary>
        /// 차트 데이터 업데이트
        /// </summary>
        private void UpdateChartData(AreaChartData data)
        {
            LogInfo($"========== 차트 업데이트 시작 ==========");
            LogInfo($"지역명: {data.AreaName}");
            LogInfo($"관측소: {string.Join(", ", data.ObservatoryNames)}");
            LogInfo($"최대값: {data.MaxAlarmCount}");
            LogInfo($"월 라벨 개수: {data.MonthLabels?.Count ?? 0}");
            LogInfo($"월별 데이터 개수: {data.MonthlyData?.Count ?? 0}");

            // 1. 타이틀 업데이트
            if (txtChartTitle != null)
            {
                txtChartTitle.text = $"{data.AreaName} 지역 알람 발생";
                LogInfo($"타이틀 업데이트: {txtChartTitle.text}");
            }

            // 2. 범례 업데이트
            UpdateLegend(data.ObservatoryNames);

            // 3. 월 라벨 업데이트 (YY/MM)
            UpdateMonthLabels(data.MonthLabels);

            // 4. Y축 값 업데이트
            UpdateYAxisLabels(data.MaxAlarmCount);

            // 5. 12개월 막대 즉시 업데이트 (애니메이션 제거)
            UpdateHistogramBars(data.MonthlyData);

            LogInfo($"========== 차트 업데이트 완료 ==========");
        }

        /// <summary>
        /// 범례 업데이트
        /// </summary>
        private void UpdateLegend(List<string> observatoryNames)
        {
            if (observatoryNames == null || observatoryNames.Count < 3)
            {
                LogWarning($"관측소 이름 개수 부족: {observatoryNames?.Count ?? 0}개 (예상: 3개)");
                return;
            }

            if (txtLegendA != null) txtLegendA.text = observatoryNames[0];
            if (txtLegendB != null) txtLegendB.text = observatoryNames[1];
            if (txtLegendC != null) txtLegendC.text = observatoryNames[2];

            LogInfo($"범례 업데이트: {observatoryNames[0]}, {observatoryNames[1]}, {observatoryNames[2]}");
        }

        /// <summary>
        /// 월 라벨 업데이트 (YY/MM)
        /// </summary>
        private void UpdateMonthLabels(List<string> monthLabels)
        {
            if (monthLabels == null || lblMonthTexts == null)
            {
                LogWarning("월 라벨 데이터 또는 UI가 null!");
                return;
            }

            for (int i = 0; i < Mathf.Min(monthLabels.Count, lblMonthTexts.Count); i++)
            {
                if (lblMonthTexts[i] != null)
                {
                    lblMonthTexts[i].text = monthLabels[i];
                }
            }

            LogInfo($"월 라벨 업데이트 완료: {string.Join(", ", monthLabels)}");
        }

        /// <summary>
        /// Y축 라벨 업데이트 (최대값 기준)
        /// </summary>
        private void UpdateYAxisLabels(int maxAlarmCount)
        {
            if (lblCountValues == null || lblCountValues.Count == 0)
            {
                LogWarning("Y축 라벨이 null!");
                return;
            }

            // 최대값을 4등분
            int interval = Mathf.CeilToInt(maxAlarmCount / 4f);

            for (int i = 0; i < lblCountValues.Count; i++)
            {
                if (lblCountValues[i] != null)
                {
                    lblCountValues[i].text = (interval * (i + 1)).ToString();
                }
            }

            LogInfo($"Y축 라벨 업데이트: 최대값={maxAlarmCount}, 간격={interval}");
        }

        /// <summary>
        /// 12개월 막대 즉시 업데이트 (애니메이션 제거로 성능 최적화)
        /// </summary>
        private void UpdateHistogramBars(List<MonthlyAlarmData> monthlyData)
        {
            LogInfo("--- 막대 차트 업데이트 시작 ---");

            if (monthlyData == null || monthlyData.Count != 12)
            {
                LogError($"월별 데이터 개수 오류: {monthlyData?.Count ?? 0}개 (예상: 12개)");
                return;
            }

            if (_histogramMonths == null || _histogramMonths.Count != 12)
            {
                LogError($"히스토그램 Transform 개수 오류: {_histogramMonths?.Count ?? 0}개");
                return;
            }

            // 모든 막대 초기화 (scaleY = 0)
            foreach (var month in _histogramMonths)
            {
                ResetMonthBars(month);
            }

            LogInfo("막대 초기화 완료");

            // 각 월 데이터 즉시 적용
            for (int i = 0; i < 12; i++)
            {
                Transform monthTransform = _histogramMonths[i];
                MonthlyAlarmData data = monthlyData[i];

                LogInfo($"[{i}월] ObsA={data.ObsA_Normalized:F2}, ObsB={data.ObsB_Normalized:F2}, ObsC={data.ObsC_Normalized:F2}");

                // 3개 관측소 막대 (Turquoise, Orange, Green)
                Transform barA = monthTransform.Find("Histogram05_Turquoise");
                Transform barB = monthTransform.Find("Histogram05_Orange");
                Transform barC = monthTransform.Find("Histogram05_Green");

                if (barA == null) LogWarning($"[{i}월] Turquoise 막대를 찾을 수 없습니다!");
                if (barB == null) LogWarning($"[{i}월] Orange 막대를 찾을 수 없습니다!");
                if (barC == null) LogWarning($"[{i}월] Green 막대를 찾을 수 없습니다!");

                // ⚡ DOTween 제거: 즉시 적용으로 성능 최적화
                if (barA != null)
                {
                    barA.localScale = new Vector3(1f, data.ObsA_Normalized, 1f);
                }

                if (barB != null)
                {
                    barB.localScale = new Vector3(1f, data.ObsB_Normalized, 1f);
                }

                if (barC != null)
                {
                    barC.localScale = new Vector3(1f, data.ObsC_Normalized, 1f);
                }
            }

            LogInfo("--- 막대 차트 업데이트 완료 ---");
        }

        /// <summary>
        /// 월별 막대 초기화 (scaleY = 0)
        /// </summary>
        private void ResetMonthBars(Transform monthTransform)
        {
            if (monthTransform == null)
                return;

            Transform barA = monthTransform.Find("Histogram05_Turquoise");
            Transform barB = monthTransform.Find("Histogram05_Orange");
            Transform barC = monthTransform.Find("Histogram05_Green");

            if (barA != null) barA.localScale = new Vector3(1f, 0f, 1f);
            if (barB != null) barB.localScale = new Vector3(1f, 0f, 1f);
            if (barC != null) barC.localScale = new Vector3(1f, 0f, 1f);
        }

        #endregion

        #region 로깅

        private void LogInfo(string message)
        {
            Debug.Log($"[AreaAlarmChartView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AreaAlarmChartView] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AreaAlarmChartView] {message}");
        }

        #endregion
    }
}