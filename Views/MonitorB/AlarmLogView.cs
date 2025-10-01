using Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorB;

public class AlarmLogView : BaseView
{
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

        if (btnTimeUp) btnTimeUp.onClick.AddListener(() => SortByTime(true));
        if (btnTimeDown) btnTimeDown.onClick.AddListener(() => SortByTime(false));
        if (btnContentUp) btnContentUp.onClick.AddListener(() => SortByContent(true));      
        if (btnContentDown) btnContentDown.onClick.AddListener(() => SortByContent(false)); 
        if (btnAreaUp) btnAreaUp.onClick.AddListener(() => SortByArea(true));
        if (btnAreaDown) btnAreaDown.onClick.AddListener(() => SortByArea(false));
        if (btnObsUp) btnObsUp.onClick.AddListener(() => SortByObservatory(true));
        if (btnObsDown) btnObsDown.onClick.AddListener(() => SortByObservatory(false));
        if (btnStatusUp) btnStatusUp.onClick.AddListener(() => SortByStatus(true));
        if (btnStatusDown) btnStatusDown.onClick.AddListener(() => SortByStatus(false));
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
        else
            LogWarning($"Image_UP not found in {container.name}");

        if (downTransform != null)
            downButton = downTransform.GetComponent<Button>();
        else
            LogWarning($"Image_Down not found in {container.name}");
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
        ToggleSortUI(sortContentContainer, ascending);
        AlarmLogViewModel.Instance.SortByArea(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByObservatory(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortContentContainer, ascending);
        AlarmLogViewModel.Instance.SortByObservatory(ascending);
        currentPage = 1;
        RenderPage();
    }

    private void SortByStatus(bool ascending)
    {
        if (AlarmLogViewModel.Instance == null) return;
        ToggleSortUI(sortContentContainer, ascending);
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
}