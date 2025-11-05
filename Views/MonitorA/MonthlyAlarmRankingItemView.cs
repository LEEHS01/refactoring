using UnityEngine;
using TMPro;
using HNS.MonitorA.Models;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 월간 알람 랭킹 아이템 View
    /// 개별 순위 항목 표시 (4자리 숫자 지원)
    /// </summary>
    public class MonthlyAlarmRankingItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text areaNameText;      // 지역이름
        [SerializeField] private TMP_Text percentageText;    // 백분율 (00)

        [Header("Digit Panels - 4자리")]
        [SerializeField] private TMP_Text thousandsDigitText; // 천의 자리
        [SerializeField] private TMP_Text hundredDigitText;   // 백의 자리
        [SerializeField] private TMP_Text tensDigitText;      // 십의 자리
        [SerializeField] private TMP_Text unitsDigitText;     // 일의 자리

        [Header("Display Settings")]
        [SerializeField] private string emptyText = "지역이름";

        private MonthlyAlarmRankingData currentData;

        /// <summary>
        /// 랭킹 데이터로 UI 업데이트
        /// </summary>
        public void UpdateData(MonthlyAlarmRankingData data)
        {
            if (data == null)
            {
                // 데이터 없으면 빈 상태로
                ClearData();
                return;
            }

            currentData = data;

            // 지역명
            if (areaNameText != null)
                areaNameText.text = data.AreaName;

            // 백분율 (소수점 없이 정수로)
            if (percentageText != null)
                percentageText.text = Mathf.RoundToInt(data.Percentage).ToString("00");

            // 알람 횟수 (4자리 분리)
            UpdateDigitDisplay(data.AlarmCount);

            // 활성화
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 알람 횟수를 4자리로 분리 표시 (0000~9999)
        /// </summary>
        private void UpdateDigitDisplay(int count)
        {
            // 4자리로 제한 (최대 9999)
            count = Mathf.Clamp(count, 0, 9999);

            // 각 자릿수 계산
            int thousands = count / 1000;           // 천의 자리
            int hundreds = (count % 1000) / 100;    // 백의 자리
            int tens = (count % 100) / 10;          // 십의 자리
            int units = count % 10;                 // 일의 자리

            // UI 업데이트
            if (thousandsDigitText != null)
                thousandsDigitText.text = thousands.ToString();

            if (hundredDigitText != null)
                hundredDigitText.text = hundreds.ToString();

            if (tensDigitText != null)
                tensDigitText.text = tens.ToString();

            if (unitsDigitText != null)
                unitsDigitText.text = units.ToString();

            // 디버그 로그
            Debug.Log($"[MonthlyAlarmRankingItemView] 숫자 표시: {count} → {thousands}/{hundreds}/{tens}/{units}");
        }

        /// <summary>
        /// 데이터 초기화 (빈 슬롯)
        /// </summary>
        private void ClearData()
        {
            if (areaNameText != null)
                areaNameText.text = emptyText;

            if (percentageText != null)
                percentageText.text = "00";

            // 모든 숫자를 0으로
            UpdateDigitDisplay(0);

            // 비활성화 또는 반투명 처리
            gameObject.SetActive(false);
        }

    }
}