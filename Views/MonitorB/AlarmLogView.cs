using Core;
using Models;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorB;
using Views.MonitorB;

public class AlarmLogView : BaseView
{
    [Header("Filter UI")]
    [SerializeField] private TMP_Dropdown dropdownMap;      // 지역 필터
    [SerializeField] private TMP_Dropdown dropdownStatus;   // 상태 필터

    [Header("Connected Views")]
    [SerializeField] private SensorView monitorBSensorView;
    [SerializeField] private PopupAlarmDetailView popupAlarmDetail;
    [SerializeField] private SensorChartView sensorChartView;

    [Header("Sort UI - Containers")]
    [SerializeField] private GameObject sortTimeContainer;
    [SerializeField] private GameObject sortContentContainer;
    [SerializeField] private GameObject sortAreaContainer;
    [SerializeField] private GameObject sortObsContainer;
    [SerializeField] private GameObject sortStatusContainer;

    [Header("List UI")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private AlarmLogItemView itemPrefab;

    [Header("Pagination UI")]
    [SerializeField] private Button btnFirstPage;
    [SerializeField] private Button btnPrevPage;
    [SerializeField] private Button btnNextPage;
    [SerializeField] private Button btnLastPage;
    [SerializeField] private Transform pageNumbersContainer;
    [SerializeField] private Button pageNumberButtonPrefab;

    [Header("Pagination Settings")]
    [SerializeField] private int pageSize = 15;
    [SerializeField] private Color normalPageColor = Color.white;
    [SerializeField] private Color selectedPageColor = Color.cyan;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.white;

    private List<AlarmLogItemView> itemPool = new List<AlarmLogItemView>();
    private List<Button> pageButtons = new List<Button>();
    private int currentPage = 1;

    private Button btnTimeUp, btnTimeDown;
    private Button btnContentUp, btnContentDown;
    private Button btnAreaUp, btnAreaDown;
    private Button btnObsUp, btnObsDown;
    private Button btnStatusUp, btnStatusDown;

    private int TotalCount => AlarmLogViewModel.Instance?.FilteredLogs?.Count ?? 0;
    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt(TotalCount / (float)pageSize));

    #region BaseView 추상 메서드 구현

    protected override void InitializeUIComponents()
    {
        if (itemPrefab != null)
            itemPrefab.gameObject.SetActive(false);

        if (pageNumberButtonPrefab != null)
            pageNumberButtonPrefab.gameObject.SetActive(false);

        EnsureItemPool();
    }

    protected override void SetupViewEvents()
    {
        InitializeSortButtons();
        InitializePaginationButtons();
        InitializeFilterDropdowns();  // ⭐ 드롭다운 초기화 추가

        foreach (var item in itemPool)
        {
            if (item != null)
            {
                item.OnItemClicked += OnAlarmItemClicked;
            }
        }
    }

    protected override void ConnectToViewModel()
    {
        if (AlarmLogViewModel.Instance == null)
        {
            LogError("AlarmLogViewModel.Instance가 null입니다!");
            return;
        }

        AlarmLogViewModel.Instance.OnLogsChanged += HandleLogsChanged;
        AlarmLogViewModel.Instance.LoadAlarmLogs();
    }

    protected override void DisconnectFromViewModel()
    {
        if (AlarmLogViewModel.Instance != null)
        {
            AlarmLogViewModel.Instance.OnLogsChanged -= HandleLogsChanged;
        }
    }

    protected override void DisconnectViewEvents()
    {
        RemoveSortButtonListeners();
        RemovePaginationButtonListeners();
        RemoveFilterDropdownListeners();  // ⭐ 드롭다운 리스너 제거 추가

        foreach (var item in itemPool)
        {
            if (item != null)
            {
                item.OnItemClicked -= OnAlarmItemClicked;
            }
        }
    }

    #endregion

    #region Filtering

    private void InitializeFilterDropdowns()
    {
        if (dropdownMap != null)
        {
            dropdownMap.onValueChanged.AddListener(OnAreaFilterChanged);
        }

        if (dropdownStatus != null)
        {
            dropdownStatus.onValueChanged.AddListener(OnStatusFilterChanged);
        }
    }

    private void RemoveFilterDropdownListeners()
    {
        if (dropdownMap != null)
        {
            dropdownMap.onValueChanged.RemoveAllListeners();
        }

        if (dropdownStatus != null)
        {
            dropdownStatus.onValueChanged.RemoveAllListeners();
        }
    }

    private void OnAreaFilterChanged(int index)
    {
        Debug.Log($"[AlarmLogView] OnAreaFilterChanged 호출! index={index}");  // ⭐

        if (AlarmLogViewModel.Instance == null) return;

        if (index == 0)
        {
            Debug.Log("[AlarmLogView] 전체 선택 - null 전달");  // ⭐
            AlarmLogViewModel.Instance.FilterByArea(null);
        }
        else
        {
            string selectedArea = dropdownMap.options[index].text;
            Debug.Log($"[AlarmLogView] 지역 선택 - {selectedArea} 전달");  // ⭐
            AlarmLogViewModel.Instance.FilterByArea(selectedArea);
        }

        currentPage = 1;
        RenderPage();
    }

    private void OnStatusFilterChanged(int index)
    {
        Debug.Log($"[AlarmLogView] OnStatusFilterChanged 호출! index={index}");  // ⭐

        if (AlarmLogViewModel.Instance == null) return;

        if (index == 0)
        {
            Debug.Log("[AlarmLogView] 전체 선택 - null 전달");  // ⭐
            AlarmLogViewModel.Instance.FilterByStatus(null);
        }
        else
        {
            int status = index - 1;
            Debug.Log($"[AlarmLogView] 상태 선택 - status={status} 전달");  // ⭐
            AlarmLogViewModel.Instance.FilterByStatus(status);
        }

        currentPage = 1;
        RenderPage();
    }

    private void PopulateDropdownOptions()
    {
        if (AlarmLogViewModel.Instance == null || AlarmLogViewModel.Instance.AllLogs == null)
            return;

        // 지역 드롭다운 옵션 채우기
        if (dropdownMap != null)
        {
            // 현재 선택된 값 저장
            int currentValue = dropdownMap.value;

            // 리스너 임시 제거
            dropdownMap.onValueChanged.RemoveListener(OnAreaFilterChanged);

            dropdownMap.ClearOptions();

            var areaNames = AlarmLogViewModel.Instance.AllLogs
                .Select(log => log.areaName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            areaNames.Insert(0, "전체");
            dropdownMap.AddOptions(areaNames);

            // 값 복원 (범위 체크)
            dropdownMap.value = Mathf.Clamp(currentValue, 0, areaNames.Count - 1);

            // 리스너 다시 추가
            dropdownMap.onValueChanged.AddListener(OnAreaFilterChanged);

            // 수동으로 Refresh
            dropdownMap.RefreshShownValue();
        }

        // 상태 드롭다운 옵션 채우기
        if (dropdownStatus != null)
        {
            // 현재 선택된 값 저장
            int currentValue = dropdownStatus.value;

            // 리스너 임시 제거
            dropdownStatus.onValueChanged.RemoveListener(OnStatusFilterChanged);

            dropdownStatus.ClearOptions();

            var statusOptions = new List<string> { "전체", "설비이상", "경계", "경보" };
            dropdownStatus.AddOptions(statusOptions);

            // 값 복원 (범위 체크)
            dropdownStatus.value = Mathf.Clamp(currentValue, 0, statusOptions.Count - 1);

            // 리스너 다시 추가
            dropdownStatus.onValueChanged.AddListener(OnStatusFilterChanged);

            // 수동으로 Refresh
            dropdownStatus.RefreshShownValue();
        }
    }

    #endregion

    #region Sort Buttons

    private void InitializeSortButtons()
    {
        FindSortButtons(sortTimeContainer, out btnTimeUp, out btnTimeDown);
        FindSortButtons(sortContentContainer, out btnContentUp, out btnContentDown);
        FindSortButtons(sortAreaContainer, out btnAreaUp, out btnAreaDown);
        FindSortButtons(sortObsContainer, out btnObsUp, out btnObsDown);
        FindSortButtons(sortStatusContainer, out btnStatusUp, out btnStatusDown);

        if (btnTimeUp) btnTimeUp.onClick.AddListener(() => SortByTime(false));
        if (btnTimeDown) btnTimeDown.onClick.AddListener(() => SortByTime(true));

        if (btnContentUp) btnContentUp.onClick.AddListener(() => SortByContent(false));
        if (btnContentDown) btnContentDown.onClick.AddListener(() => SortByContent(true));

        if (btnAreaUp) btnAreaUp.onClick.AddListener(() => SortByArea(false));
        if (btnAreaDown) btnAreaDown.onClick.AddListener(() => SortByArea(true));

        if (btnObsUp) btnObsUp.onClick.AddListener(() => SortByObservatory(false));
        if (btnObsDown) btnObsDown.onClick.AddListener(() => SortByObservatory(true));

        if (btnStatusUp) btnStatusUp.onClick.AddListener(() => SortByStatus(false));
        if (btnStatusDown) btnStatusDown.onClick.AddListener(() => SortByStatus(true));
    }

    private void FindSortButtons(GameObject container, out Button upButton, out Button downButton)
    {
        upButton = null;
        downButton = null;

        if (container == null)
        {
            LogWarning("Sort container is null");
            return;
        }

        var upTransform = container.transform.Find("Image_UP");
        var downTransform = container.transform.Find("Image_Down");

        if (upTransform != null)
            upButton = upTransform.GetComponent<Button>();

        if (downTransform != null)
            downButton = downTransform.GetComponent<Button>();
    }

    private void RemoveSortButtonListeners()
    {
        if (btnTimeUp) btnTimeUp.onClick.RemoveAllListeners();
        if (btnTimeDown) btnTimeDown.onClick.RemoveAllListeners();
        if (btnContentUp) btnContentUp.onClick.RemoveAllListeners();
        if (btnContentDown) btnContentDown.onClick.RemoveAllListeners();
        if (btnAreaUp) btnAreaUp.onClick.RemoveAllListeners();
        if (btnAreaDown) btnAreaDown.onClick.RemoveAllListeners();
        if (btnObsUp) btnObsUp.onClick.RemoveAllListeners();
        if (btnObsDown) btnObsDown.onClick.RemoveAllListeners();
        if (btnStatusUp) btnStatusUp.onClick.RemoveAllListeners();
        if (btnStatusDown) btnStatusDown.onClick.RemoveAllListeners();
    }

    #endregion

    #region Sort Methods

    private void SortByTime(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortTimeContainer, ascending);
        AlarmLogViewModel.Instance.SortByTime(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByContent(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortContentContainer, ascending);
        AlarmLogViewModel.Instance.SortByContent(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByArea(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortAreaContainer, ascending);
        AlarmLogViewModel.Instance.SortByArea(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByObservatory(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortObsContainer, ascending);
        AlarmLogViewModel.Instance.SortByObservatory(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByStatus(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortStatusContainer, ascending);
        AlarmLogViewModel.Instance.SortByStatus(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void ToggleSortUI(GameObject container, bool ascending)
    {
        if (container == null) return;

        var upImage = container.transform.Find("Image_UP");
        var downImage = container.transform.Find("Image_Down");

        if (upImage != null)
            upImage.gameObject.SetActive(ascending);

        if (downImage != null)
            downImage.gameObject.SetActive(!ascending);
    }

    #endregion

    #region Pagination

    private void InitializePaginationButtons()
    {
        if (btnFirstPage) btnFirstPage.onClick.AddListener(() => GoToPage(1));
        if (btnPrevPage) btnPrevPage.onClick.AddListener(() => GoToPage(currentPage - 1));
        if (btnNextPage) btnNextPage.onClick.AddListener(() => GoToPage(currentPage + 1));
        if (btnLastPage) btnLastPage.onClick.AddListener(() => GoToPage(TotalPages));
    }

    private void RemovePaginationButtonListeners()
    {
        if (btnFirstPage) btnFirstPage.onClick.RemoveAllListeners();
        if (btnPrevPage) btnPrevPage.onClick.RemoveAllListeners();
        if (btnNextPage) btnNextPage.onClick.RemoveAllListeners();
        if (btnLastPage) btnLastPage.onClick.RemoveAllListeners();
    }

    private void GoToPage(int page)
    {
        currentPage = Mathf.Clamp(page, 1, TotalPages);
        RenderPage();
    }

    #endregion

    #region Rendering

    private void HandleLogsChanged()
    {
        currentPage = 1;
        EnsureItemPool();
        PopulateDropdownOptions();  // ⭐ 드롭다운 옵션 채우기
        RenderPage();
    }

    private void EnsureItemPool()
    {
        if (itemPrefab == null || listContainer == null) return;

        while (itemPool.Count < pageSize)
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.gameObject.SetActive(false);
            itemPool.Add(item);
        }
    }

    private void RenderPage()
    {
        if (AlarmLogViewModel.Instance == null || AlarmLogViewModel.Instance.FilteredLogs == null)
        {
            LogWarning("ViewModel 또는 FilteredLogs가 null입니다.");
            return;
        }

        int startIndex = (currentPage - 1) * pageSize;
        int endIndex = Mathf.Min(startIndex + pageSize, TotalCount);
        int itemCount = Mathf.Max(0, endIndex - startIndex);

        for (int i = 0; i < pageSize; i++)
        {
            if (i >= itemPool.Count) continue;

            var item = itemPool[i];
            if (i < itemCount && startIndex + i < AlarmLogViewModel.Instance.FilteredLogs.Count)
            {
                var data = AlarmLogViewModel.Instance.FilteredLogs[startIndex + i];
                item.gameObject.SetActive(true);
                item.SetData(data);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }

        UpdatePaginationButtons();
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        if (btnFirstPage) btnFirstPage.interactable = currentPage > 1;
        if (btnPrevPage) btnPrevPage.interactable = currentPage > 1;
        if (btnNextPage) btnNextPage.interactable = currentPage < TotalPages;
        if (btnLastPage) btnLastPage.interactable = currentPage < TotalPages;
    }

    private void UpdatePaginationButtons()
    {
        if (pageNumbersContainer == null || pageNumberButtonPrefab == null) return;

        foreach (var btn in pageButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        pageButtons.Clear();

        CreatePageNumberButtons();
    }

    private void CreatePageNumberButtons()
    {
        int maxVisibleButtons = 7;

        if (TotalPages <= maxVisibleButtons)
        {
            for (int i = 1; i <= TotalPages; i++)
            {
                CreatePageButton(i);
            }
            return;
        }

        List<int> pagesToShow = new List<int>();
        pagesToShow.Add(1);

        if (currentPage <= 4)
        {
            for (int i = 2; i <= 5; i++)
            {
                if (i < TotalPages) pagesToShow.Add(i);
            }
            pagesToShow.Add(-1);
            pagesToShow.Add(TotalPages);
        }
        else if (currentPage >= TotalPages - 3)
        {
            pagesToShow.Add(-1);
            for (int i = TotalPages - 4; i <= TotalPages; i++)
            {
                if (i > 1) pagesToShow.Add(i);
            }
        }
        else
        {
            pagesToShow.Add(-1);
            for (int i = currentPage - 1; i <= currentPage + 1; i++)
            {
                pagesToShow.Add(i);
            }
            pagesToShow.Add(-1);
            pagesToShow.Add(TotalPages);
        }

        foreach (int pageNum in pagesToShow)
        {
            if (pageNum == -1)
                CreateEllipsisButton();
            else
                CreatePageButton(pageNum);
        }
    }

    private void CreatePageButton(int pageNumber)
    {
        var btnObj = Instantiate(pageNumberButtonPrefab.gameObject, pageNumbersContainer);
        btnObj.SetActive(true);

        var btn = btnObj.GetComponent<Button>();
        var txt = btnObj.GetComponentInChildren<TMP_Text>();

        if (txt != null)
            txt.text = pageNumber.ToString();

        bool isCurrentPage = pageNumber == currentPage;
        var btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = isCurrentPage ? selectedPageColor : normalPageColor;
        if (txt != null)
            txt.color = isCurrentPage ? selectedTextColor : normalTextColor;

        btn.interactable = !isCurrentPage;

        int page = pageNumber;
        btn.onClick.AddListener(() => GoToPage(page));

        pageButtons.Add(btn);
    }

    private void CreateEllipsisButton()
    {
        var btnObj = Instantiate(pageNumberButtonPrefab.gameObject, pageNumbersContainer);
        btnObj.SetActive(true);

        var btn = btnObj.GetComponent<Button>();
        var txt = btnObj.GetComponentInChildren<TMP_Text>();

        if (txt != null)
            txt.text = "...";

        btn.interactable = false;

        var btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = normalPageColor;
        if (txt != null)
            txt.color = normalTextColor;

        pageButtons.Add(btn);
    }

    #endregion

    #region Alarm Item Click Handler

    /// <summary>
    /// 알람 아이템 클릭 시 - 해당 관측소의 센서 데이터 표시
    /// </summary>
    private void OnAlarmItemClicked(AlarmLogData alarmData)
    {
        if (alarmData == null)
        {
            LogError("AlarmLogData가 null입니다!");
            return;
        }

        LogInfo($"알람 로그 클릭: {alarmData.areaName} - {alarmData.obsName}");

        // ⭐⭐⭐ 핵심: ViewModel에 알림 (Monitor A/B 자동 업데이트!)
        if (AlarmLogViewModel.Instance != null)
        {
            AlarmLogViewModel.Instance.SelectAlarm(alarmData.logId);
        }

        // 팝업 열기
        if (popupAlarmDetail != null)
        {
            popupAlarmDetail.OpenPopup(
                alarmData.obsId,
                alarmData.boardId,
                alarmData.sensorId,
                alarmData.time,
                alarmData.alarmValue,
                alarmData.obsName,
                alarmData.areaName
            );
        }

        // 센서 뷰 로드
        if (monitorBSensorView != null)
        {
            monitorBSensorView.LoadObservatory(
                alarmData.obsId,
                alarmData.areaName,
                alarmData.obsName
            );
        }

        // 차트 로드
        LoadDefaultToxicityChart(alarmData.obsId);
    }

    /// <summary>
    /// 기본 차트 로드: 독성도 (Board 1, HNS 1)
    /// </summary>
    private void LoadDefaultToxicityChart(int obsId)
    {
        if (sensorChartView == null)
        {
            LogWarning("SensorChartView가 연결되지 않았습니다.");
            return;
        }

        // 독성도: Board 1, HNS 1
        const int DEFAULT_BOARD_ID = 1;
        const int DEFAULT_HNS_ID = 1;
        const string DEFAULT_SENSOR_NAME = "독성도";

        LogInfo($"기본 차트 로드: {DEFAULT_SENSOR_NAME} (obsId={obsId})");

        sensorChartView.LoadSensorChart(
            obsId,
            DEFAULT_BOARD_ID,
            DEFAULT_HNS_ID,
            DEFAULT_SENSOR_NAME
        );
    }

    #endregion
}