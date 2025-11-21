using HNS.Common.Models;
using Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.Common;

namespace Views.Common
{
    public class PopupAlarmView : Core.BaseView
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text lblTitle;
        [SerializeField] private TMP_Text lblSummary;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgSignalLamp;
        [SerializeField] private Image imgSignalLight;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnAlarmTransition;

        // ⭐ Icon Sprites는 제거 - Resources에서 동적 로드
        // [Header("Icon Sprites")] - 삭제됨

        // 상태별 아이콘 경로 (원본 코드와 동일)
        private static Dictionary<ToxinStatus, string> statusSpriteDic = new()
        {
            {ToxinStatus.Yellow,   "Image/ErrorIcon/Serious"},     // 경계
            {ToxinStatus.Red,      "Image/ErrorIcon/Warning"},     // 경보
            {ToxinStatus.Purple,   "Image/ErrorIcon/Malfunction"}, // 설비이상
        };

        private Coroutine _updateTimerCoroutine;
        private bool _isSubscribed = false;

        protected override void InitializeUIComponents()
        {
            LogInfo("UI 컴포넌트 초기화 시작...");

            bool isValid = ValidateComponents(
                (canvasGroup, "CanvasGroup"),
                (lblTitle, "lblTitle"),
                (lblSummary, "lblSummary"),
                (imgIcon, "imgIcon"),
                (imgSignalLamp, "imgSignalLamp"),
                (imgSignalLight, "imgSignalLight"),
                (btnClose, "btnClose"),
                (btnAlarmTransition, "btnAlarmTransition")
            );

            if (!isValid)
            {
                LogError("필수 UI 컴포넌트가 누락되었습니다!");
                return;
            }

            // ✅ 초기 상태: 완전히 숨김
            HidePopup();

            // ✅ GameObject도 비활성화 (CanvasGroup만으로 부족할 경우 대비)
            // gameObject.SetActive(false);  // ← 이건 사용하면 안 됨! (BaseView 생명주기 문제)

            LogInfo("UI 컴포넌트 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정 시작...");
            btnClose.onClick.AddListener(OnClickClose);
            btnAlarmTransition.onClick.AddListener(OnClickAlarmTransition);
            LogInfo("View 이벤트 설정 완료");
        }

        protected override void DisconnectViewEvents()
        {
            LogInfo("View 이벤트 해제 시작...");
            btnClose.onClick.RemoveListener(OnClickClose);
            btnAlarmTransition.onClick.RemoveListener(OnClickAlarmTransition);
            LogInfo("View 이벤트 해제 완료");
        }

        protected override void ConnectToViewModel()
        {
            LogInfo("ConnectToViewModel 호출됨");
            SubscribeToViewModel();
        }

        protected override void DisconnectFromViewModel()
        {
            LogInfo("DisconnectFromViewModel 호출됨");
            UnsubscribeFromViewModel();
        }

        private void SubscribeToViewModel()
        {
            if (_isSubscribed)
            {
                LogWarning("이미 ViewModel에 구독되어 있습니다.");
                return;
            }

            LogInfo("ViewModel 이벤트 구독 시작");

            if (PopupAlarmViewModel.Instance == null)
            {
                LogError("PopupAlarmViewModel.Instance가 null입니다!");
                return;
            }

            PopupAlarmViewModel.Instance.OnNewAlarmDetected += OnNewAlarmDetected;
            PopupAlarmViewModel.Instance.OnAlarmTimeUpdated += OnAlarmTimeUpdated;
            PopupAlarmViewModel.Instance.OnAlarmCleared += OnAlarmCleared;

            _isSubscribed = true;
            LogInfo("ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            LogInfo("ViewModel 이벤트 구독 해제 시작");

            if (PopupAlarmViewModel.Instance != null)
            {
                PopupAlarmViewModel.Instance.OnNewAlarmDetected -= OnNewAlarmDetected;
                PopupAlarmViewModel.Instance.OnAlarmTimeUpdated -= OnAlarmTimeUpdated;
                PopupAlarmViewModel.Instance.OnAlarmCleared -= OnAlarmCleared;
            }

            _isSubscribed = false;
            LogInfo("ViewModel 이벤트 구독 해제 완료");
        }

        private void OnNewAlarmDetected(AlarmLogData alarmData)
        {
            LogInfo($"신규 알람 감지: {alarmData.logId} - {alarmData.obsName}");
            UpdateAlarmUI(alarmData);
            ShowPopup();
            StartUpdateTimer();
        }

        private void OnAlarmTimeUpdated(string timeAgo)
        {
            if (PopupAlarmViewModel.Instance.CurrentAlarm != null)
            {
                var alarm = PopupAlarmViewModel.Instance.CurrentAlarm;
                UpdateSummaryText(alarm, timeAgo);
            }
        }

        private void OnAlarmCleared()
        {
            LogInfo("알람 클리어됨");
            HidePopup();
        }

        private void UpdateAlarmUI(AlarmLogData alarmData)
        {
            UpdateTitle(alarmData);
            string timeAgo = PopupAlarmViewModel.Instance.GetFormattedTimeAgo(alarmData.time);
            UpdateSummaryText(alarmData, timeAgo);
            ToxinStatus toxinStatus = GetToxinStatus(alarmData.status);
            UpdateIcon(toxinStatus);
            UpdateSignalLamp(toxinStatus);
        }

        private void UpdateTitle(AlarmLogData alarmData)
        {
            ToxinStatus logStatus = GetToxinStatus(alarmData.status);

            switch (logStatus)
            {
                case ToxinStatus.Purple: // 설비이상
                    lblTitle.text = $"설비이상 발생 : {alarmData.areaName} - {alarmData.obsName} 보드 {alarmData.boardId}번";
                    break;
                case ToxinStatus.Yellow: // 경계
                    lblTitle.text = $"경계 알람 발생 : {alarmData.areaName} - {alarmData.obsName} {alarmData.sensorName}";
                    break;
                case ToxinStatus.Red: // 경보
                    lblTitle.text = $"경보 알람 발생 : {alarmData.areaName} - {alarmData.obsName} {alarmData.sensorName}";
                    break;
            }
        }

        private void UpdateSummaryText(AlarmLogData alarmData, string timeAgo)
        {
            // 원본 코드 형식으로 표시
            DateTime logDt = alarmData.time;
            ToxinStatus logStatus = GetToxinStatus(alarmData.status);

            lblSummary.text = "" +
                $"발생 지점 : {alarmData.areaName} - {alarmData.obsName}\n" +
                $"발생 시각 : {logDt:yy/MM/dd HH:mm}({timeAgo})\n\n";

            // 설비이상과 센서 알람 구분 표시
            if (logStatus == ToxinStatus.Purple)
            {
                lblSummary.text +=
                    $"설비 이상 : {"보드 " + alarmData.boardId}";
            }
            else
            {
                // 측정값 포맷팅
                string measureStr = alarmData.alarmValue.HasValue
                    ? alarmData.alarmValue.Value.ToString("F2")
                    : "-";

                // ⭐ 임계값: status에 따라 경계값 또는 경보값 표시
                string thresholdStr = "-";
                if (alarmData.status == 1 && alarmData.warningThreshold.HasValue)
                {
                    // 경계 알람 → 경계 임계값 표시
                    thresholdStr = alarmData.warningThreshold.Value.ToString("F2");
                }
                else if (alarmData.status == 2 && alarmData.criticalThreshold.HasValue)
                {
                    // 경보 알람 → 경보 임계값 표시
                    thresholdStr = alarmData.criticalThreshold.Value.ToString("F2");
                }

                lblSummary.text +=
                    $"원인 물질 : {alarmData.sensorName}\n" +
                    $"측정 값 : {measureStr} / {thresholdStr}";
            }
        }

        /// <summary>
        /// 아이콘 업데이트 - Resources 폴더에서 동적 로드 (원본 코드 방식)
        /// </summary>
        private void UpdateIcon(ToxinStatus status)
        {
            if (statusSpriteDic.ContainsKey(status))
            {
                string spritePath = statusSpriteDic[status];
                Sprite loadedSprite = Resources.Load<Sprite>(spritePath);

                if (loadedSprite != null)
                {
                    imgIcon.sprite = loadedSprite;
                    LogInfo($"아이콘 로드 성공: {spritePath}");
                }
                else
                {
                    LogWarning($"아이콘 로드 실패: {spritePath}");
                }
            }
            else
            {
                LogWarning($"상태에 해당하는 아이콘 경로 없음: {status}");
            }
        }

        /// <summary>
        /// 신호등 업데이트 - 색상만 변경 (원본 코드 방식)
        /// </summary>
        private void UpdateSignalLamp(ToxinStatus status)
        {
            Color lampColor = GetLampColor(status);
            imgSignalLamp.color = lampColor;
            imgSignalLight.color = lampColor;
        }

        private Color GetLampColor(ToxinStatus status)
        {
            switch (status)
            {
                case ToxinStatus.Yellow:
                    return new Color(1f, 0.92f, 0.016f);
                case ToxinStatus.Red:
                    return new Color(1f, 0f, 0f);
                case ToxinStatus.Purple:
                    return new Color(0.5f, 0f, 0.5f);
                default:
                    return Color.white;
            }
        }

        private ToxinStatus GetToxinStatus(int status)
        {
            switch (status)
            {
                case 0: return ToxinStatus.Purple;
                case 1: return ToxinStatus.Yellow;
                case 2: return ToxinStatus.Red;
                default: return ToxinStatus.Green;
            }
        }

        private void ShowPopup()
        {
            if (canvasGroup == null)
            {
                LogError("⚠️ CanvasGroup이 null입니다! Inspector에서 연결하세요.");
                return;
            }

            LogInfo("팝업 표시");
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void HidePopup()
        {
            if (canvasGroup == null)
            {
                LogError("⚠️ CanvasGroup이 null입니다! Inspector에서 연결하세요.");
                return;
            }

            LogInfo("팝업 숨김");
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void StartUpdateTimer()
        {
            StopUpdateTimer();
            _updateTimerCoroutine = StartCoroutine(UpdateTimeCoroutine());
        }

        private void StopUpdateTimer()
        {
            if (_updateTimerCoroutine != null)
            {
                StopCoroutine(_updateTimerCoroutine);
                _updateTimerCoroutine = null;
            }
        }

        private IEnumerator UpdateTimeCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f);
                PopupAlarmViewModel.Instance?.UpdateCurrentAlarmTime();
            }
        }

        private void OnClickClose()
        {
            LogInfo("닫기 버튼 클릭");
            HidePopup();
            StopUpdateTimer();
        }

        private void OnClickAlarmTransition()
        {
            LogInfo("자세히 보기 버튼 클릭");
            PopupAlarmViewModel.Instance.SelectCurrentAlarm();
            OnClickClose();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
#endif
    }
}