using Assets.Scripts_refactoring.Views.MonitorA;
using HNS.Common.Models;
using HNS.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.Common.Views
{
    /// <summary>
    /// 환경설정 - 관측소 설정 탭 View
    /// </summary>
    public class PopupSettingTabObsView : MonoBehaviour
    {
        [Header("Observatory Selection")]
        [SerializeField] private TMP_Dropdown ddlArea;
        [SerializeField] private TMP_Dropdown ddlObs;

        [Header("Toxin Board")]
        [SerializeField] private Toggle tglBoardToxin;
        [SerializeField] private TMP_InputField txtToxinWarning;

        [Header("Chemical Board")]
        [SerializeField] private Toggle tglBoardChemical;
        [SerializeField] private TMP_InputField txtSearchChemical;
        [SerializeField] private Transform chlUsingChemicalContainer;
        private List<PopupSettingSensorItemView> chlUsingChemical = new();

        [Header("Quality Board")]
        [SerializeField] private Toggle tglBoardQuality;
        [SerializeField] private Transform chlUsingQualityContainer;
        private List<PopupSettingSensorItemView> chlUsingQuality = new();

        [Header("CCTV URLs")]
        [SerializeField] private TMP_InputField txtCctvEquipment;
        [SerializeField] private TMP_InputField txtCctvOutdoor;

        private bool _isSubscribed = false;
        private int _currentObsId = -1;
        private List<AreaData> _areaList = new();
        private List<ObservatoryData> _obsList = new();

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
            FindUIComponents();

            // 센서 아이템 뷰 수집
            CollectSensorItems();

            LogInfo($"초기화 완료 - 화학: {chlUsingChemical.Count}개, 수질: {chlUsingQuality.Count}개");
        }

        private void FindUIComponents()
        {
            // pnlSelectObs
            if (ddlArea == null || ddlObs == null)
            {
                Transform pnlSelectObs = transform.Find("pnlSelectObs");
                if (pnlSelectObs != null)
                {
                    if (ddlArea == null)
                        ddlArea = pnlSelectObs.Find("ddlArea")?.GetComponent<TMP_Dropdown>();
                    if (ddlObs == null)
                        ddlObs = pnlSelectObs.Find("ddlObs")?.GetComponent<TMP_Dropdown>();
                }
            }

            // pnlBoardToxin
            if (tglBoardToxin == null || txtToxinWarning == null)
            {
                Transform pnlBoardToxin = transform.Find("pnlBoardToxin");
                if (pnlBoardToxin != null)
                {
                    if (tglBoardToxin == null)
                        tglBoardToxin = pnlBoardToxin.Find("tglBoardFixing")?.GetComponent<Toggle>();

                    // hlnToxinField 하위의 InputField 찾기
                    if (txtToxinWarning == null)
                    {
                        Transform hlnToxinField = pnlBoardToxin.Find("hlnToxinField");
                        if (hlnToxinField != null)
                            txtToxinWarning = hlnToxinField.GetComponentInChildren<TMP_InputField>();
                    }
                }
            }

            // pnlBoardChemical
            if (tglBoardChemical == null || txtSearchChemical == null || chlUsingChemicalContainer == null)
            {
                Transform pnlBoardChemical = transform.Find("pnlBoardChemical");
                if (pnlBoardChemical != null)
                {
                    if (tglBoardChemical == null)
                        tglBoardChemical = pnlBoardChemical.Find("tglBoardFixing")?.GetComponent<Toggle>();
                    if (txtSearchChemical == null)
                        txtSearchChemical = pnlBoardChemical.Find("txtSearchSensor")?.GetComponent<TMP_InputField>();
                    if (chlUsingChemicalContainer == null)
                    {
                        Transform scrollView = pnlBoardChemical.Find("chlUsingSensor");
                        if (scrollView != null)
                            chlUsingChemicalContainer = scrollView.Find("Content");
                    }
                }
            }

            // pnlBoardQuality (검색 필드 제거)
            if (tglBoardQuality == null || chlUsingQualityContainer == null)
            {
                Transform pnlBoardQuality = transform.Find("pnlBoardQuality");
                if (pnlBoardQuality != null)
                {
                    if (tglBoardQuality == null)
                        tglBoardQuality = pnlBoardQuality.Find("tglBoardFixing")?.GetComponent<Toggle>();
                    if (chlUsingQualityContainer == null)
                    {
                        Transform scrollView = pnlBoardQuality.Find("chlUsingSensor");
                        if (scrollView != null)
                            chlUsingQualityContainer = scrollView.Find("Content");
                    }
                }
            }

            // pnlCctvUrl
            if (txtCctvEquipment == null || txtCctvOutdoor == null)
            {
                Transform pnlCctvUrl = transform.Find("pnlCctvUrl");
                if (pnlCctvUrl != null)
                {
                    if (txtCctvEquipment == null)
                        txtCctvEquipment = pnlCctvUrl.Find("txtUrlEquipment")?.GetComponent<TMP_InputField>();
                    if (txtCctvOutdoor == null)
                        txtCctvOutdoor = pnlCctvUrl.Find("txtUrlOutdoor")?.GetComponent<TMP_InputField>();
                }
            }
        }

        private void CollectSensorItems()
        {
            // 화학물질 센서 아이템 수집 (범례 제외)
            if (chlUsingChemicalContainer != null)
            {
                chlUsingChemical = chlUsingChemicalContainer
                    .GetComponentsInChildren<PopupSettingSensorItemView>(true)
                    .Where(item => item.gameObject.name.Contains("PopupSettingItem"))
                    .ToList();

                // 원본처럼 초기화 (불러오는 중...)
                chlUsingChemical.ForEach(item =>
                    item.SetItem(_currentObsId, 2, -1, "불러오는 중...", true, 0f, 0f));
            }

            // 수질 센서 아이템 수집 (범례 제외)
            if (chlUsingQualityContainer != null)
            {
                chlUsingQuality = chlUsingQualityContainer
                    .GetComponentsInChildren<PopupSettingSensorItemView>(true)
                    .Where(item => item.gameObject.name.Contains("PopupSettingItem"))
                    .ToList();

                // 원본처럼 초기화 (불러오는 중...)
                chlUsingQuality.ForEach(item =>
                    item.SetItem(_currentObsId, 3, -1, "불러오는 중...", true, 0f, 0f));
            }
        }

        private void SetupUIEvents()
        {
            // 드롭다운
            if (ddlArea != null)
                ddlArea.onValueChanged.AddListener(OnAreaSelected);
            if (ddlObs != null)
                ddlObs.onValueChanged.AddListener(OnObsSelected);

            // 보드 고정 토글 (원본처럼 역방향: isOn → !isFixing)
            if (tglBoardToxin != null)
                tglBoardToxin.onValueChanged.AddListener(isOn => OnBoardFixingChanged(1, !isOn));
            if (tglBoardChemical != null)
                tglBoardChemical.onValueChanged.AddListener(isOn => OnBoardFixingChanged(2, !isOn));
            if (tglBoardQuality != null)
                tglBoardQuality.onValueChanged.AddListener(isOn => OnBoardFixingChanged(3, !isOn));

            // 독성도 경고값
            if (txtToxinWarning != null)
                txtToxinWarning.onEndEdit.AddListener(OnToxinWarningChanged);

            // 센서 검색 (화학물질만)
            if (txtSearchChemical != null)
                txtSearchChemical.onValueChanged.AddListener(OnSearchChemical);

            // CCTV URL (원본처럼 onValueChanged 사용)
            if (txtCctvEquipment != null)
                txtCctvEquipment.onValueChanged.AddListener(url => OnCctvUrlChanged(CctvType.Equipment, url));
            if (txtCctvOutdoor != null)
                txtCctvOutdoor.onValueChanged.AddListener(url => OnCctvUrlChanged(CctvType.Outdoor, url));
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

            PopupSettingViewModel.Instance.OnAreasLoaded += OnAreasLoaded;
            PopupSettingViewModel.Instance.OnObservatoriesLoaded += OnObservatoriesLoaded;
            PopupSettingViewModel.Instance.OnObservatorySettingsLoaded += OnObservatorySettingsLoaded;
            PopupSettingViewModel.Instance.OnUpdateSuccess += OnUpdateSuccess;

            _isSubscribed = true;
            LogInfo("ViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnAreasLoaded -= OnAreasLoaded;
                PopupSettingViewModel.Instance.OnObservatoriesLoaded -= OnObservatoriesLoaded;
                PopupSettingViewModel.Instance.OnObservatorySettingsLoaded -= OnObservatorySettingsLoaded;
                PopupSettingViewModel.Instance.OnUpdateSuccess -= OnUpdateSuccess;
            }

            _isSubscribed = false;
        }

        #endregion

        #region ViewModel Event Handlers

        private void OnAreasLoaded(List<AreaData> areas)
        {
            _areaList = areas;
            LogInfo($"지역 목록 로드: {areas.Count}개");

            if (ddlArea == null) return;

            ddlArea.ClearOptions();

            var options = areas.Select(a => new TMP_Dropdown.OptionData(a.areaName)).ToList();
            ddlArea.AddOptions(options);

            if (areas.Count > 0)
            {
                ddlArea.SetValueWithoutNotify(0);
                OnAreaSelected(0);
            }
        }

        private void OnObservatoriesLoaded(List<ObservatoryData> observatories)
        {
            _obsList = observatories;
            LogInfo($"관측소 목록 로드: {observatories.Count}개");

            if (ddlObs == null) return;

            ddlObs.ClearOptions();

            var options = observatories.Select(o => new TMP_Dropdown.OptionData(o.ObsName)).ToList();
            ddlObs.AddOptions(options);

            if (observatories.Count > 0)
            {
                // ⭐ 첫 번째 관측소 자동 선택
                ddlObs.SetValueWithoutNotify(0);
                OnObsSelected(0);
            }
        }

        private void OnObservatorySettingsLoaded(ObservatorySettingData settings)
        {
            _currentObsId = settings.ObsId;
            LogInfo($"관측소 설정 로드: ObsId={settings.ObsId}");

            // 독성도
            if (txtToxinWarning != null)
                txtToxinWarning.SetTextWithoutNotify(settings.ToxinWarningValue.ToString("F1"));
            if (tglBoardToxin != null)
                tglBoardToxin.SetIsOnWithoutNotify(!settings.ToxinBoardFixed);

            // 화학물질
            if (tglBoardChemical != null)
                tglBoardChemical.SetIsOnWithoutNotify(!settings.ChemicalBoardFixed);
            UpdateSensorList(chlUsingChemical, settings.ChemicalSensors, 2);

            // 수질
            if (tglBoardQuality != null)
                tglBoardQuality.SetIsOnWithoutNotify(!settings.QualityBoardFixed);
            UpdateSensorList(chlUsingQuality, settings.QualitySensors, 3);

            // CCTV
            if (txtCctvEquipment != null)
                txtCctvEquipment.SetTextWithoutNotify(settings.CctvEquipmentUrl);
            if (txtCctvOutdoor != null)
                txtCctvOutdoor.SetTextWithoutNotify(settings.CctvOutdoorUrl);

            // 화학물질 검색만 초기화
            if (txtSearchChemical != null)
                txtSearchChemical.SetTextWithoutNotify("");
            OnSearchChemical("");
        }

        private void OnUpdateSuccess(string message)
        {
            LogInfo($"업데이트 성공: {message}");
        }

        #endregion

        #region UI Event Handlers

        private void OnAreaSelected(int index)
        {
            if (PopupSettingViewModel.Instance != null && index >= 0 && index < _areaList.Count)
            {
                int areaId = _areaList[index].areaId;
                PopupSettingViewModel.Instance.SelectArea(areaId);
            }
        }

        private void OnObsSelected(int index)
        {
            if (PopupSettingViewModel.Instance != null && index >= 0 && index < _obsList.Count)
            {
                int obsId = _obsList[index].ObsId;
                PopupSettingViewModel.Instance.SelectObservatory(obsId);
            }
        }

        private void OnBoardFixingChanged(int boardId, bool isFixed)
        {
            if (PopupSettingViewModel.Instance != null && _currentObsId > 0)
            {
                PopupSettingViewModel.Instance.ToggleBoardFixing(boardId, isFixed);
            }
        }

        private void OnToxinWarningChanged(string value)
        {
            if (float.TryParse(value, out float warningValue))
            {
                if (PopupSettingViewModel.Instance != null && _currentObsId > 0)
                {
                    PopupSettingViewModel.Instance.UpdateToxinWarning(warningValue);
                }
            }
        }

        private void OnSearchChemical(string searchText)
        {
            FilterSensorList(chlUsingChemical, searchText, chlUsingChemicalContainer);
        }

        private void OnCctvUrlChanged(CctvType type, string url)
        {
            if (PopupSettingViewModel.Instance != null && _currentObsId > 0)
            {
                PopupSettingViewModel.Instance.UpdateCctvUrl(type, url);
            }
        }

        #endregion

        #region UI Update

        /// <summary>
        /// 센서 리스트 업데이트 (원본과 동일한 로직)
        /// </summary>
        private void UpdateSensorList(List<PopupSettingSensorItemView> items,
            List<SensorSettingData> sensors, int boardId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                int sensorId = i + 1;
                SensorSettingData sensor = sensors.Find(s => s.SensorId == sensorId);

                if (sensor != null)
                {
                    // ⭐ 로그 추가: SetItem 호출 전에 값 확인
                    LogInfo($"[UpdateSensorList] Board={boardId}, Sensor={sensorId}");
                    LogInfo($"  - Name: {sensor.SensorName}");
                    LogInfo($"  - IsUsing: {sensor.IsUsing}");
                    LogInfo($"  - SeriousValue (hi): {sensor.SeriousValue}");
                    LogInfo($"  - WarningValue (hihi): {sensor.WarningValue}");

                    items[i].SetItem(
                        _currentObsId,
                        boardId,
                        sensorId,
                        sensor.SensorName,
                        sensor.IsUsing,
                        sensor.SeriousValue,    // hi (경계)
                        sensor.WarningValue     // hihi (경보)
                    );
                }
                else
                {
                    items[i].SetItem(_currentObsId, boardId, -1, "불러오는 중...", true, 0f, 0f);
                }
            }

            // 수질은 원본처럼 유효한 것만 표시
            if (boardId == 3)
            {
                items.ForEach(item => item.gameObject.SetActive(item.IsValid));
            }
        }

        /// <summary>
        /// 센서 검색 및 컨테이너 크기 동적 조정 (원본 OnChangeSearchSensor)
        /// </summary>
        private void FilterSensorList(List<PopupSettingSensorItemView> items,
            string searchText, Transform container)
        {
            int visibleCount = 0;

            foreach (var item in items)
            {
                bool isMatch = string.IsNullOrEmpty(searchText) ||
                               item.SensorName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                bool shouldShow = isMatch && item.IsValid;
                item.gameObject.SetActive(shouldShow);

                if (shouldShow)
                    visibleCount++;
            }

            // 원본처럼 화학물질 컨테이너만 크기 동적 조정
            if (container != null && items.Count > 0)
            {
                Transform verticalLayoutParent = container.parent;
                if (verticalLayoutParent != null)
                {
                    VerticalLayoutGroup layoutGroup = verticalLayoutParent.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup != null)
                    {
                        RectTransform itemContainer = layoutGroup.GetComponent<RectTransform>();
                        if (itemContainer != null && items[0] != null)
                        {
                            float itemHeight = items[0].GetComponent<RectTransform>().sizeDelta.y;
                            itemContainer.sizeDelta = new Vector2(itemContainer.sizeDelta.x, itemHeight * visibleCount);
                        }
                    }
                }
            }

            LogInfo($"검색 결과: {visibleCount}개 표시");
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[PopupSettingTabObsView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PopupSettingTabObsView] {message}");
        }

        #endregion
    }
}