// Views/MonitorB/PopupTableView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorB;
using System.Collections.Generic;
using System;

namespace Views.MonitorB
{
    /// <summary>
    /// 센서 데이터를 표로 표시하는 팝업 (리팩토링 버전)
    /// </summary>
    public class PopupTableView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;    // 센서명
        [SerializeField] private TMP_Text txtTimeRange;     // 조회 시점
        [SerializeField] private Button btnClose;           // 닫기 버튼

        [Header("Table")]
        [SerializeField] private Transform tableContent;    // Table 오브젝트

        private List<GameObject> tableRows = new List<GameObject>();

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePopup);
            }

            // Table의 모든 row 찾기
            FindAllRows();

            // 초기 비활성화
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveListener(ClosePopup);
            }
        }

        /// <summary>
        /// Table의 모든 row 오브젝트 찾기 (row0 제외)
        /// </summary>
        private void FindAllRows()
        {
            if (tableContent == null)
            {
                Debug.LogError("[PopupTableView] tableContent가 할당되지 않았습니다!");
                return;
            }

            tableRows.Clear();

            // tableContent의 모든 자식 중 "row"로 시작하는 것들 찾기
            foreach (Transform child in tableContent)
            {
                // row0(헤더)은 제외
                if (child.name.StartsWith("row") && child.name != "row0")
                {
                    tableRows.Add(child.gameObject);
                }
            }

            // 이름순 정렬 (row1, row2, ... row72 순서대로)
            tableRows.Sort((a, b) =>
            {
                int numA = int.Parse(a.name.Replace("row", ""));
                int numB = int.Parse(b.name.Replace("row", ""));
                return numA.CompareTo(numB);
            });

            Debug.Log($"[PopupTableView] {tableRows.Count}개 행 찾기 완료");
        }

        /// <summary>
        /// 팝업 열기
        /// </summary>
        public void OpenPopup(string sensorName, string unit, ChartData chartData)
        {
            if (chartData == null || chartData.values.Count == 0)
            {
                Debug.LogWarning("[PopupTableView] 표시할 데이터가 없습니다.");
                return;
            }

            gameObject.SetActive(true);

            // 센서명 표시
            if (txtSensorName != null)
            {
                txtSensorName.text = sensorName;
            }

            // 조회 시점 표시
            UpdateTimeRange(chartData.startTime, chartData.endTime);

            // 헤더 업데이트 (column1에 단위 추가)
            UpdateHeader(unit);

            // 테이블 데이터 채우기
            FillTableData(chartData);

            Debug.Log($"[PopupTableView] 팝업 열기 완료: {chartData.values.Count}개 행");
        }

        /// <summary>
        /// 조회 시점 업데이트
        /// </summary>
        private void UpdateTimeRange(DateTime startTime, DateTime endTime)
        {
            if (txtTimeRange == null) return;

            // 10분 단위로 내림
            DateTime roundedStart = RoundDownToTenMinutes(startTime);
            DateTime roundedEnd = RoundDownToTenMinutes(endTime);

            if (roundedStart.Date == roundedEnd.Date)
            {
                txtTimeRange.text = $"{roundedStart:yyyy-MM-dd}  {roundedStart:HH:mm} ~ {roundedEnd:HH:mm}";
            }
            else
            {
                txtTimeRange.text = $"{roundedStart:yyyy-MM-dd HH:mm} ~ {roundedEnd:yyyy-MM-dd HH:mm}";
            }
        }

        /// <summary>
        /// 시간을 10분 단위로 내림
        /// 13:55 → 13:50, 13:07 → 13:00
        /// </summary>
        private DateTime RoundDownToTenMinutes(DateTime dt)
        {
            int minutes = dt.Minute;
            int roundedMinutes = (minutes / 10) * 10;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, roundedMinutes, 0);
        }

        /// <summary>
        /// 헤더 업데이트 (측정값 컬럼에 단위 추가)
        /// </summary>
        /// <summary>
        /// 헤더 업데이트 (측정값 컬럼에 단위 추가)
        /// </summary>
        private void UpdateHeader(string unit)
        {
            Debug.Log($"[PopupTableView] UpdateHeader 시작: unit={unit}");

            if (tableContent == null)
            {
                Debug.LogError("[PopupTableView] tableContent가 null!");
                return;
            }

            Transform headerRow = tableContent.Find("row0");
            if (headerRow == null)
            {
                Debug.LogError("[PopupTableView] row0를 찾을 수 없습니다!");
                return;
            }

            Debug.Log($"[PopupTableView] row0 찾음: {headerRow.name}");

            // 구조: row0 → cells
            Transform cells = headerRow.Find("cells");
            if (cells == null)
            {
                Debug.LogError("[PopupTableView] row0에 cells가 없습니다!");
                Debug.Log($"[PopupTableView] row0의 자식들:");
                foreach (Transform child in headerRow)
                {
                    Debug.Log($"  - {child.name}");
                }
                return;
            }

            Debug.Log($"[PopupTableView] cells 찾음, 자식 개수: {cells.childCount}");

            if (cells.childCount < 2)
            {
                Debug.LogError($"[PopupTableView] cells의 자식이 {cells.childCount}개뿐!");
                return;
            }

            // column0: 시간
            Transform column0 = cells.GetChild(0);
            if (column0 != null)
            {
                Debug.Log($"[PopupTableView] column0 이름: {column0.name}");

                TMP_Text headerText = column0.GetComponent<TMP_Text>();
                if (headerText != null)
                {
                    headerText.text = "시간";
                    Debug.Log($"[PopupTableView] column0 텍스트 설정: 시간");
                }
                else
                {
                    Debug.LogError($"[PopupTableView] column0에 TMP_Text 없음!");
                }
            }

            // column1: 측정값(단위)
            Transform column1 = cells.GetChild(1);
            if (column1 != null)
            {
                Debug.Log($"[PopupTableView] column1 이름: {column1.name}");

                TMP_Text headerText = column1.GetComponent<TMP_Text>();
                if (headerText != null)
                {
                    string headerTitle = string.IsNullOrEmpty(unit) ? "측정값" : $"측정값({unit})";
                    headerText.text = headerTitle;
                    Debug.Log($"[PopupTableView] column1 텍스트 설정: {headerTitle}");
                }
                else
                {
                    Debug.LogError($"[PopupTableView] column1에 TMP_Text 없음!");
                }
            }
        }

        /// <summary>
        /// 테이블 데이터 채우기 (최신 데이터가 위로 - 역순)
        /// </summary>
        private void FillTableData(ChartData chartData)
        {
            int dataCount = chartData.values.Count;

            Debug.Log($"[PopupTableView] 데이터: {dataCount}개, 행: {tableRows.Count}개");
            Debug.Log($"[PopupTableView] allTimes: {chartData.allTimes?.Count ?? 0}개");

            // 역순으로 데이터 채우기 (최신 데이터가 위로)
            for (int i = 0; i < tableRows.Count; i++)
            {
                if (i < dataCount)
                {
                    // 데이터가 있는 행 활성화 및 채우기
                    tableRows[i].SetActive(true);

                    // 역순 인덱스 (최신 데이터가 위로)
                    int dataIndex = dataCount - 1 - i;

                    // 구조: row → cells
                    Transform cells = tableRows[i].transform.Find("cells");
                    if (cells == null || cells.childCount < 2)
                    {
                        continue;
                    }

                    Transform column0 = cells.GetChild(0);
                    Transform column1 = cells.GetChild(1);

                    // 시간 (allTimes 사용)
                    if (column0 != null && chartData.allTimes != null && dataIndex < chartData.allTimes.Count)
                    {
                        TMP_Text timeText = column0.GetComponent<TMP_Text>();
                        if (timeText != null)
                        {
                            timeText.text = chartData.allTimes[dataIndex].ToString("yyyy-MM-dd HH:mm");
                        }
                    }

                    // 측정값
                    if (column1 != null && dataIndex < chartData.values.Count)
                    {
                        TMP_Text valueText = column1.GetComponent<TMP_Text>();
                        if (valueText != null)
                        {
                            valueText.text = chartData.values[dataIndex].ToString("F2");
                        }
                    }
                }
                else
                {
                    // 데이터가 없는 행 비활성화
                    tableRows[i].SetActive(false);
                    Transform panel = tableRows[i].transform.Find("panel");
                    if (panel != null)
                    {
                        panel.gameObject.SetActive(false);
                    }
                }
            }

            Debug.Log($"[PopupTableView] {dataCount}개 행 표시 완료 (역순)");
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        private void ClosePopup()
        {
            gameObject.SetActive(false);
            Debug.Log("[PopupTableView] 팝업 닫기");
        }
    }
}