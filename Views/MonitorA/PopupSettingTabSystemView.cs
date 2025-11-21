using HNS.Common.Models;
using HNS.Common.ViewModels;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.Common.Views
{
    /// <summary>
    /// 환경설정 - 시스템 설정 탭 View
    /// </summary>
    public class PopupSettingTabSystemView : MonoBehaviour
    {
        [Header("Alarm Setting")]
        [SerializeField] private Slider sldAlarmThreshold;
        [SerializeField] private Image imgSliderHandle;

        [Header("Database Setting")]
        [SerializeField] private TMP_InputField txtDbUrl;

        private static System.Collections.Generic.Dictionary<ToxinStatus, Color> statusColorDic = new()
        {
            { ToxinStatus.Green,    ColorFromHex("#FFF600") },  // 원본과 동일
            { ToxinStatus.Yellow,   ColorFromHex("#FF0000") },
            { ToxinStatus.Red,      ColorFromHex("#6C00E2") },
            { ToxinStatus.Purple,   ColorFromHex("#C6C6C6") }
        };

        private bool _isSubscribed = false;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupUIEvents();
        }

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Inspector에서 연결되지 않은 경우 동적으로 찾기
            if (sldAlarmThreshold == null)
            {
                Transform pnlAlarmSetting = transform.Find("pnlAlarmSetting");
                if (pnlAlarmSetting != null)
                {
                    sldAlarmThreshold = pnlAlarmSetting.GetComponentInChildren<Slider>();

                    // 원본처럼 Handle 찾기
                    if (sldAlarmThreshold != null)
                    {
                        Transform handleArea = sldAlarmThreshold.transform.Find("Handle Slide Area");
                        if (handleArea != null)
                        {
                            Transform handle = handleArea.Find("Handle");
                            if (handle != null)
                                imgSliderHandle = handle.GetComponent<Image>();
                        }
                    }
                }
            }

            if (txtDbUrl == null)
            {
                Transform pnlDatabase = transform.Find("pnlDatabase");
                if (pnlDatabase != null)
                {
                    txtDbUrl = pnlDatabase.GetComponentInChildren<TMP_InputField>();
                }
            }
        }

        private void SetupUIEvents()
        {
            if (sldAlarmThreshold != null)
                sldAlarmThreshold.onValueChanged.AddListener(OnAlarmSliderChanged);

            if (txtDbUrl != null)
                txtDbUrl.onEndEdit.AddListener(OnDbUrlChanged);
        }

        #endregion

        #region ViewModel Subscription

        private void SubscribeToViewModel()
        {
            if (_isSubscribed) return;

            if (PopupSettingViewModel.Instance == null)
            {
                LogError("PopupSettingViewModel.Instance가 null입니다!");
                return;
            }

            PopupSettingViewModel.Instance.OnSystemSettingsLoaded += OnSystemSettingsLoaded;

            _isSubscribed = true;
            LogInfo("ViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnSystemSettingsLoaded -= OnSystemSettingsLoaded;
            }

            _isSubscribed = false;
        }

        #endregion

        #region ViewModel Event Handlers

        private void OnSystemSettingsLoaded(SystemSettingData settings)
        {
            LogInfo($"시스템 설정 로드: AlarmThreshold={settings.AlarmThreshold}");

            // 원본처럼 알람 임계값 슬라이더 설정 (역방향: 1-value)
            int selectionCount = Enum.GetNames(typeof(ToxinStatus)).Length;
            float normalizedValue = 1f - (float)settings.AlarmThreshold / (selectionCount - 1);

            if (sldAlarmThreshold != null)
                sldAlarmThreshold.SetValueWithoutNotify(normalizedValue);

            if (imgSliderHandle != null)
                imgSliderHandle.color = statusColorDic[settings.AlarmThreshold];

            // DB URL
            if (txtDbUrl != null)
                txtDbUrl.SetTextWithoutNotify(settings.DatabaseUrl);
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 알람 임계값 슬라이더 변경 (원본 OnAlarmSliderChanged와 동일)
        /// </summary>
        private void OnAlarmSliderChanged(float value)
        {
            int selectionCount = Enum.GetNames(typeof(ToxinStatus)).Length;
            int choosenIdx = Mathf.RoundToInt((1 - value) * (selectionCount - 1));

            // 정규화된 값으로 다시 설정 (스냅)
            float normalizedSliderValue = (float)choosenIdx / (float)(selectionCount - 1);
            if (sldAlarmThreshold != null)
                sldAlarmThreshold.SetValueWithoutNotify(1f - normalizedSliderValue);

            ToxinStatus threshold = (ToxinStatus)choosenIdx;

            // 핸들 색상 업데이트
            if (imgSliderHandle != null)
                imgSliderHandle.color = statusColorDic[threshold];

            // ViewModel 업데이트
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.ChangeAlarmThreshold(threshold);
            }

            LogInfo($"알람 임계값 변경: {threshold}");
        }

        private void OnDbUrlChanged(string url)
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.ChangeDatabaseUrl(url);
            }

            LogInfo($"DB URL 변경: {url}");
        }

        #endregion

        #region Utility

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[PopupSettingTabSystemView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PopupSettingTabSystemView] {message}");
        }

        #endregion
    }
}