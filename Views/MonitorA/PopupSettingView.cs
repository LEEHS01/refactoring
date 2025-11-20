using Core;
using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using Onthesys;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ObsData = HNS.MonitorA.Models.ObsData;
using AreaDataModel = HNS.MonitorA.Models.AreaData;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 환경설정 팝업 View
    /// BaseView를 상속하여 MVVM 패턴 적용
    /// </summary>
    public class PopupSettingView : BaseView
    {
        #region Inspector - Tab Buttons

        [Header("Tab Buttons")]
        [SerializeField] private Button btnTabObs;
        [SerializeField] private Button btnTabSystem;

        #endregion

        #region Inspector - Tab Panels

        [Header("Tab Panels")]
        [SerializeField] private GameObject pnlTabObs;
        [SerializeField] private GameObject pnlTabSystem;

        #endregion

        #region Inspector - Obs Tab - Dropdowns

        [Header("Obs Tab - Dropdowns")]
        [SerializeField] private TMP_Dropdown ddlArea;
        [SerializeField] private TMP_Dropdown ddlObs;

        #endregion

        #region Inspector - Obs Tab - Board Settings

        [Header("Obs Tab - Board Settings")]
        [SerializeField] private Toggle tglBoardToxin;
        [SerializeField] private Toggle tglBoardQuality;
        [SerializeField] private Toggle tglBoardChemical;
        [SerializeField] private TMP_InputField txtToxinWarning;

        #endregion

        #region Inspector - Obs Tab - Sensor Lists

        [Header("Obs Tab - Sensor Lists")]
        [SerializeField] private TMP_InputField txtSearchQuality;
        [SerializeField] private TMP_InputField txtSearchChemical;
        [SerializeField] private Transform containerQuality;
        [SerializeField] private Transform containerChemical;

        #endregion

        #region Inspector - Obs Tab - CCTV

        [Header("Obs Tab - CCTV")]
        [SerializeField] private TMP_InputField txtCctvEquipment;
        [SerializeField] private TMP_InputField txtCctvOutdoor;

        #endregion

        #region Inspector - System Tab

        [Header("System Tab")]
        [SerializeField] private Slider sldAlarmPopup;
        [SerializeField] private Image imgSliderHandle;
        [SerializeField] private TMP_InputField txtDbAddress;

        #endregion

        #region Inspector - Close Button

        [Header("Close Button")]
        [SerializeField] private Button btnClose;

        #endregion

        #region Private Fields

        // 센서 아이템들
        private List<PopupSettingItem> chlUsingQuality;
        private List<PopupSettingItem> chlUsingChemical;

        // 현재 선택된 ID
        private int _currentObsId = -1;
        private int _currentAreaId = -1;

        // 팝업 기본 위치
        private Vector3 _defaultPosition;

        // 슬라이더 색상 (정적)
        private static Dictionary<ToxinStatus, Color> statusColorDic = new();

        // 탭 스프라이트
        private Sprite _sprTabOn;
        private Sprite _sprTabOff;
        private Color _colorTabOn = Color.white;
        private Color _colorTabOff;

        #endregion

        #region Static Constructor

        static PopupSettingView()
        {
            Dictionary<ToxinStatus, string> rawColorSets = new Dictionary<ToxinStatus, string>
            {
                { ToxinStatus.Green,    "#FFF600"},
                { ToxinStatus.Yellow,   "#FF0000"},
                { ToxinStatus.Red,      "#6C00E2"},
                { ToxinStatus.Purple,   "#C6C6C6"},
            };

            foreach (var pair in rawColorSets)
            {
                if (ColorUtility.TryParseHtmlString(pair.Value, out Color color))
                {
                    statusColorDic[pair.Key] = color;
                }
            }
        }

        #endregion

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            LogInfo("========================================");
            LogInfo("=== InitializeUIComponents 시작 ===");
            LogInfo($"GameObject 이름: {gameObject.name}");
            LogInfo("========================================");

            // Inspector 연결 검증
            bool isValid = true;

            // Buttons
            isValid &= ValidateComponent(btnTabObs, "btnTabObs");
            isValid &= ValidateComponent(btnTabSystem, "btnTabSystem");

            // GameObjects - 별도로 null 체크
            if (pnlTabObs == null)
            {
                LogError("pnlTabObs가 Inspector에서 연결되지 않았습니다!");
                isValid = false;
            }
            if (pnlTabSystem == null)
            {
                LogError("pnlTabSystem이 Inspector에서 연결되지 않았습니다!");
                isValid = false;
            }

            // Dropdowns
            isValid &= ValidateComponent(ddlArea, "ddlArea");
            isValid &= ValidateComponent(ddlObs, "ddlObs");

            // Toggles
            isValid &= ValidateComponent(tglBoardToxin, "tglBoardToxin");
            isValid &= ValidateComponent(tglBoardQuality, "tglBoardQuality");
            isValid &= ValidateComponent(tglBoardChemical, "tglBoardChemical");

            // InputFields
            isValid &= ValidateComponent(txtToxinWarning, "txtToxinWarning");
            isValid &= ValidateComponent(txtSearchQuality, "txtSearchQuality");
            isValid &= ValidateComponent(txtSearchChemical, "txtSearchChemical");
            isValid &= ValidateComponent(txtCctvEquipment, "txtCctvEquipment");
            isValid &= ValidateComponent(txtCctvOutdoor, "txtCctvOutdoor");
            isValid &= ValidateComponent(txtDbAddress, "txtDbAddress");

            // Transforms
            isValid &= ValidateComponent(containerQuality, "containerQuality");
            isValid &= ValidateComponent(containerChemical, "containerChemical");

            // Slider & Image
            isValid &= ValidateComponent(sldAlarmPopup, "sldAlarmPopup");
            isValid &= ValidateComponent(imgSliderHandle, "imgSliderHandle");

            // Close Button
            isValid &= ValidateComponent(btnClose, "btnClose");

            if (!isValid)
            {
                LogError("필수 컴포넌트 연결 실패!");
                return;
            }

            // 센서 아이템 리스트 수집
            CollectSensorItems();

            // 리소스 로드
            LoadResources();

            // 기본 위치 저장
            _defaultPosition = transform.position;

            // 초기 상태 설정
            OpenTab(pnlTabObs);

            // 대신 CanvasGroup으로 숨김
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            LogInfo("UI 컴포넌트 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정 시작...");

            // Tab 버튼
            btnTabObs.onClick.AddListener(() => OpenTab(pnlTabObs));
            btnTabSystem.onClick.AddListener(() => OpenTab(pnlTabSystem));
            btnClose.onClick.AddListener(OnClickClose);

            // Dropdowns
            ddlArea.onValueChanged.AddListener(OnAreaSelected);
            ddlObs.onValueChanged.AddListener(OnObsSelected);

            // Toggles
            tglBoardToxin.onValueChanged.AddListener(isOn => OnToggleBoard(1, !isOn));
            tglBoardQuality.onValueChanged.AddListener(isOn => OnToggleBoard(3, !isOn));
            tglBoardChemical.onValueChanged.AddListener(isOn => OnToggleBoard(2, !isOn));

            // InputFields
            txtToxinWarning.onEndEdit.AddListener(OnToxinWarningChanged);
            txtSearchQuality.onValueChanged.AddListener(OnSearchSensorQuality);
            txtSearchChemical.onValueChanged.AddListener(OnSearchSensorChemical);
            txtCctvEquipment.onValueChanged.AddListener(url => OnCctvUrlChanged(CctvType.EQUIPMENT, url));
            txtCctvOutdoor.onValueChanged.AddListener(url => OnCctvUrlChanged(CctvType.OUTDOOR, url));

            // System Settings
            sldAlarmPopup.onValueChanged.AddListener(OnAlarmSliderChanged);
            txtDbAddress.onValueChanged.AddListener(OnDbAddressChanged);

            LogInfo("View 이벤트 설정 완료");
        }

        protected override void ConnectToViewModel()
        {
            LogInfo("ConnectToViewModel 호출됨");

            if (PopupSettingViewModel.Instance == null)
            {
                LogError("PopupSettingViewModel.Instance가 null입니다!");
                return;
            }

            var vm = PopupSettingViewModel.Instance;
            vm.OnAreasLoaded += HandleAreasLoaded;
            vm.OnObsListLoaded += HandleObsListLoaded;
            vm.OnObsSettingLoaded += HandleObsSettingLoaded;
            vm.OnSensorSettingsLoaded += HandleSensorSettingsLoaded;
            vm.OnSystemSettingLoaded += HandleSystemSettingLoaded;
            vm.OnError += HandleError;

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            LogInfo("DisconnectFromViewModel 호출됨");

            if (PopupSettingViewModel.Instance == null) return;

            var vm = PopupSettingViewModel.Instance;
            vm.OnAreasLoaded -= HandleAreasLoaded;
            vm.OnObsListLoaded -= HandleObsListLoaded;
            vm.OnObsSettingLoaded -= HandleObsSettingLoaded;
            vm.OnSensorSettingsLoaded -= HandleSensorSettingsLoaded;
            vm.OnSystemSettingLoaded -= HandleSystemSettingLoaded;
            vm.OnError -= HandleError;

            LogInfo("ViewModel 이벤트 구독 해제 완료");
        }

        protected override void DisconnectViewEvents()
        {
            LogInfo("View 이벤트 해제 시작...");

            btnTabObs.onClick.RemoveAllListeners();
            btnTabSystem.onClick.RemoveAllListeners();
            btnClose.onClick.RemoveAllListeners();
            ddlArea.onValueChanged.RemoveAllListeners();
            ddlObs.onValueChanged.RemoveAllListeners();
            tglBoardToxin.onValueChanged.RemoveAllListeners();
            tglBoardQuality.onValueChanged.RemoveAllListeners();
            tglBoardChemical.onValueChanged.RemoveAllListeners();
            txtToxinWarning.onEndEdit.RemoveAllListeners();
            txtSearchQuality.onValueChanged.RemoveAllListeners();
            txtSearchChemical.onValueChanged.RemoveAllListeners();
            txtCctvEquipment.onValueChanged.RemoveAllListeners();
            txtCctvOutdoor.onValueChanged.RemoveAllListeners();
            sldAlarmPopup.onValueChanged.RemoveAllListeners();
            txtDbAddress.onValueChanged.RemoveAllListeners();

            LogInfo("View 이벤트 해제 완료");
        }

        #endregion

        #region 초기화 헬퍼 메서드

        /// <summary>
        /// 센서 아이템 리스트 수집
        /// </summary>
        private void CollectSensorItems()
        {
            chlUsingQuality = containerQuality.GetComponentsInChildren<PopupSettingItem>(true).ToList();
            chlUsingChemical = containerChemical.GetComponentsInChildren<PopupSettingItem>(true).ToList();

            LogInfo($"수질 센서 아이템 {chlUsingQuality.Count}개 수집");
            LogInfo($"화학물질 센서 아이템 {chlUsingChemical.Count}개 수집");

            // 초기 상태 설정
            foreach (var item in chlUsingQuality)
            {
                item.SetItem(-1, 3, -1, "불러오는 중...", true, 0f, 0f);
            }

            foreach (var item in chlUsingChemical)
            {
                item.SetItem(-1, 2, -1, "불러오는 중...", true, 0f, 0f);
            }
        }

        /// <summary>
        /// 리소스 로드
        /// </summary>
        private void LoadResources()
        {
            _sprTabOn = Resources.Load<Sprite>("Image/UI/Btn_Search_p");
            _sprTabOff = Resources.Load<Sprite>("Image/UI/Btn_Search_n");

            if (!ColorUtility.TryParseHtmlString("#99B1CB", out _colorTabOff))
            {
                LogError("탭 오프 색상 파싱 실패!");
            }
        }

        #endregion

        #region Public 메서드

        /// <summary>
        /// 팝업 열기 (외부에서 호출)
        /// </summary>
        public void OpenPopup(int obsId = 0)
        {
            LogInfo($"OpenPopup 호출 - ObsId:{obsId}");

            bool isFromObs = (obsId >= 1);

            // ObsMonitoring을 통해 켜질 때, 해당 관측소의 탭만을 제공
            if (isFromObs)
            {
                OpenTab(pnlTabObs);
                NavigateToObs(obsId);
            }
            else
            {
                // 초기 데이터 로드
                LoadInitialData();
            }

            btnTabSystem.gameObject.SetActive(!isFromObs);

            transform.position = _defaultPosition;

            // ✅ CanvasGroup으로 표시
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 특정 관측소로 이동
        /// </summary>
        public void NavigateToObs(int obsId)
        {
            LogInfo($"NavigateToObs 호출 - ObsId:{obsId}");

            _currentObsId = obsId;

            // ViewModel에서 관측소 정보 가져오기
            PopupSettingViewModel.Instance.LoadObsSetting(obsId);
            PopupSettingViewModel.Instance.LoadSensorSettings(obsId);

            // 드롭다운 자동 설정은 HandleObsSettingLoaded에서 처리
        }

        #endregion

        #region Private 메서드

        /// <summary>
        /// 초기 데이터 로드
        /// </summary>
        private void LoadInitialData()
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.LoadAreas();
                PopupSettingViewModel.Instance.LoadSystemSetting();
            }
        }

        /// <summary>
        /// 탭 열기
        /// </summary>
        private void OpenTab(GameObject targetTab)
        {
            // 버튼 스프라이트 변경
            btnTabObs.GetComponentInChildren<Image>().sprite = pnlTabObs == targetTab ? _sprTabOn : _sprTabOff;
            btnTabSystem.GetComponentInChildren<Image>().sprite = pnlTabSystem == targetTab ? _sprTabOn : _sprTabOff;

            // 버튼 텍스트 색상 변경
            btnTabObs.GetComponentInChildren<TMP_Text>().color = pnlTabObs == targetTab ? _colorTabOn : _colorTabOff;
            btnTabSystem.GetComponentInChildren<TMP_Text>().color = pnlTabSystem == targetTab ? _colorTabOn : _colorTabOff;

            // 패널 활성화
            pnlTabObs.SetActive(pnlTabObs == targetTab);
            pnlTabSystem.SetActive(pnlTabSystem == targetTab);

            LogInfo($"탭 전환 완료: {targetTab.name}");
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        private void HandleAreasLoaded(List<AreaDataModel> areas)
        {
            LogInfo($"HandleAreasLoaded - {areas.Count}개 지역");

            ddlArea.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var area in areas)
            {
                options.Add(new TMP_Dropdown.OptionData(area.areaName));
            }

            ddlArea.AddOptions(options);

            // 기본 지역 선택 (첫 번째)
            if (areas.Count > 0)
            {
                ddlArea.value = 0;
                OnAreaSelected(0);
            }
        }

        private void HandleObsListLoaded(List<ObsData> obsList)
        {
            LogInfo($"HandleObsListLoaded - {obsList.Count}개 관측소");

            ddlObs.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var obs in obsList)
            {
                options.Add(new TMP_Dropdown.OptionData(obs.obsName));
            }

            ddlObs.AddOptions(options);

            // 기본 관측소 선택 (첫 번째)
            if (obsList.Count > 0)
            {
                ddlObs.value = 0;
                OnObsSelected(0);
            }
        }

        private void HandleObsSettingLoaded(ObsSettingData setting)
        {
            LogInfo($"HandleObsSettingLoaded - {setting.ObsName}");

            // 보드 고정 상태 설정
            tglBoardToxin.SetIsOnWithoutNotify(!setting.ToxinBoardFixed);
            tglBoardChemical.SetIsOnWithoutNotify(!setting.ChemicalBoardFixed);
            tglBoardQuality.SetIsOnWithoutNotify(!setting.QualityBoardFixed);

            // 독성도 경보값 설정
            txtToxinWarning.SetTextWithoutNotify(setting.ToxinWarning.ToString("F1"));

            // CCTV URL 설정
            txtCctvEquipment.SetTextWithoutNotify(setting.CctvEquipmentUrl);
            txtCctvOutdoor.SetTextWithoutNotify(setting.CctvOutdoorUrl);

            // 드롭다운 자동 설정
            SetDropdownToObs(setting.AreaName, setting.ObsName);
        }

        private void HandleSensorSettingsLoaded(List<SensorSettingData> sensors)
        {
            LogInfo($"HandleSensorSettingsLoaded - {sensors.Count}개 센서");

            // Chemical 센서들 업데이트
            var chemicals = sensors.Where(s => s.BoardId == 2).ToList();
            for (int i = 0; i < chemicals.Count && i < chlUsingChemical.Count; i++)
            {
                var sensor = chemicals[i];
                chlUsingChemical[i].SetItem(
                    sensor.ObsId, sensor.BoardId, sensor.HnsId,
                    sensor.HnsName, sensor.IsUsing,
                    sensor.Serious, sensor.Warning
                );
            }

            // Quality 센서들 업데이트
            var qualities = sensors.Where(s => s.BoardId == 3).ToList();
            for (int i = 0; i < qualities.Count && i < chlUsingQuality.Count; i++)
            {
                var sensor = qualities[i];
                chlUsingQuality[i].SetItem(
                    sensor.ObsId, sensor.BoardId, sensor.HnsId,
                    sensor.HnsName, sensor.IsUsing,
                    sensor.Serious, sensor.Warning
                );
            }

            // 검색 초기화
            OnSearchSensorQuality("");
            OnSearchSensorChemical("");
        }

        private void HandleSystemSettingLoaded(SystemSettingData setting)
        {
            LogInfo($"HandleSystemSettingLoaded - AlarmThreshold:{setting.AlarmThreshold}");

            // 알람 임계값 슬라이더 설정
            int selectionCount = Enum.GetNames(typeof(ToxinStatus)).Length;
            float normalizedValue = (float)(int)setting.AlarmThreshold / (selectionCount - 1);
            sldAlarmPopup.SetValueWithoutNotify(1f - normalizedValue);

            if (statusColorDic.ContainsKey(setting.AlarmThreshold))
            {
                imgSliderHandle.color = statusColorDic[setting.AlarmThreshold];
            }

            // 데이터베이스 URL 설정
            txtDbAddress.SetTextWithoutNotify(setting.DatabaseUrl);
        }

        private void HandleError(string errorMsg)
        {
            LogError($"ViewModel 에러: {errorMsg}");
        }

        #endregion

        #region UI 이벤트 핸들러

        /// <summary>
        /// 지역 선택 이벤트
        /// </summary>
        private void OnAreaSelected(int index)
        {
            _currentAreaId = index + 1;
            LogInfo($"지역 선택 - Index:{index}, AreaId:{_currentAreaId}");

            PopupSettingViewModel.Instance?.LoadObsByArea(_currentAreaId);
        }

        /// <summary>
        /// 관측소 선택 이벤트
        /// </summary>
        private void OnObsSelected(int index)
        {
            if (ddlObs.options.Count == 0) return;

            string obsName = ddlObs.options[index].text;
            LogInfo($"관측소 선택 - Index:{index}, ObsName:{obsName}");

            // ModelProvider에서 obsId 찾기
            var allObs = UiManager.Instance?.modelProvider?.GetObss();
            if (allObs != null)
            {
                var obs = allObs.Find(o => o.obsName == obsName);
                if (obs != null)
                {
                    _currentObsId = obs.id;
                    PopupSettingViewModel.Instance?.LoadObsSetting(_currentObsId);
                    PopupSettingViewModel.Instance?.LoadSensorSettings(_currentObsId);
                }
            }
        }

        /// <summary>
        /// 보드 고정 토글 이벤트
        /// </summary>
        private void OnToggleBoard(int boardId, bool isFixing)
        {
            LogInfo($"보드 고정 토글 - BoardId:{boardId}, IsFixing:{isFixing}");
            PopupSettingViewModel.Instance?.UpdateBoardFixing(_currentObsId, boardId, isFixing);
        }

        /// <summary>
        /// 독성도 경보값 변경 이벤트
        /// </summary>
        private void OnToxinWarningChanged(string value)
        {
            if (float.TryParse(value, out float warning))
            {
                LogInfo($"독성도 경보값 변경 - Warning:{warning}");
                PopupSettingViewModel.Instance?.UpdateToxinWarning(_currentObsId, warning);
            }
            else
            {
                LogWarning($"유효하지 않은 경보값: {value}");
            }
        }

        /// <summary>
        /// 수질 센서 검색 이벤트
        /// </summary>
        private void OnSearchSensorQuality(string searchText)
        {
            int visibleCount = 0;

            foreach (var item in chlUsingQuality)
            {
                bool isVisible = item.isValid &&
                                (string.IsNullOrEmpty(searchText) ||
                                 item.lblSensorName.text.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));

                item.gameObject.SetActive(isVisible);
                if (isVisible) visibleCount++;
            }

            // 컨테이너 크기 조정
            if (chlUsingQuality.Count > 0)
            {
                var itemHeight = chlUsingQuality[0].GetComponent<RectTransform>().sizeDelta.y;
                var containerRect = containerQuality.GetComponent<RectTransform>();
                containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, itemHeight * visibleCount);
            }

            LogInfo($"수질 센서 검색 - 검색어:{searchText}, 표시:{visibleCount}개");
        }

        /// <summary>
        /// 화학물질 센서 검색 이벤트
        /// </summary>
        private void OnSearchSensorChemical(string searchText)
        {
            int visibleCount = 0;

            foreach (var item in chlUsingChemical)
            {
                bool isVisible = item.isValid &&
                                (string.IsNullOrEmpty(searchText) ||
                                 item.lblSensorName.text.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));

                item.gameObject.SetActive(isVisible);
                if (isVisible) visibleCount++;
            }

            // 컨테이너 크기 조정
            if (chlUsingChemical.Count > 0)
            {
                var itemHeight = chlUsingChemical[0].GetComponent<RectTransform>().sizeDelta.y;
                var containerRect = containerChemical.GetComponent<RectTransform>();
                containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, itemHeight * visibleCount);
            }

            LogInfo($"화학물질 센서 검색 - 검색어:{searchText}, 표시:{visibleCount}개");
        }

        /// <summary>
        /// CCTV URL 변경 이벤트
        /// </summary>
        private void OnCctvUrlChanged(CctvType type, string url)
        {
            LogInfo($"CCTV URL 변경 - Type:{type}, Url:{url}");
            PopupSettingViewModel.Instance?.UpdateCctvUrl(_currentObsId, type, url);
        }

        /// <summary>
        /// 알람 슬라이더 변경 이벤트
        /// </summary>
        private void OnAlarmSliderChanged(float value)
        {
            int selectionCount = Enum.GetNames(typeof(ToxinStatus)).Length;
            int chosenIdx = Mathf.RoundToInt((1 - value) * (selectionCount - 1));

            float normalizedSliderValue = (float)chosenIdx / (selectionCount - 1);
            sldAlarmPopup.SetValueWithoutNotify(1f - normalizedSliderValue);

            ToxinStatus threshold = (ToxinStatus)chosenIdx;

            if (statusColorDic.ContainsKey(threshold))
            {
                imgSliderHandle.color = statusColorDic[threshold];
            }

            LogInfo($"알람 임계값 변경 - Threshold:{threshold}");
            PopupSettingViewModel.Instance?.UpdateAlarmThreshold(threshold);
        }

        /// <summary>
        /// 데이터베이스 주소 변경 이벤트
        /// </summary>
        private void OnDbAddressChanged(string address)
        {
            LogInfo($"데이터베이스 URL 변경 - Url:{address}");
            PopupSettingViewModel.Instance?.UpdateDatabaseUrl(address);
        }

        /// <summary>
        /// 닫기 버튼 클릭 이벤트
        /// </summary>
        private void OnClickClose()
        {
            LogInfo("팝업 닫기");

            // ✅ CanvasGroup으로 숨김
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 드롭다운을 특정 관측소로 설정
        /// </summary>
        private void SetDropdownToObs(string areaName, string obsName)
        {
            // 지역 드롭다운 설정
            var areaOption = ddlArea.options.Find(opt => opt.text == areaName);
            if (areaOption != null)
            {
                int areaIndex = ddlArea.options.IndexOf(areaOption);
                ddlArea.SetValueWithoutNotify(areaIndex);
            }

            // 관측소 드롭다운 설정
            var obsOption = ddlObs.options.Find(opt => opt.text == obsName);
            if (obsOption != null)
            {
                int obsIndex = ddlObs.options.IndexOf(obsOption);
                ddlObs.SetValueWithoutNotify(obsIndex);
            }
        }

        #endregion
    }
}