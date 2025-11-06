using HNS.MonitorA.Models;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    public class YearlyAlarmRankingItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI areaNameText;
        [SerializeField] private TextMeshProUGUI purpleCountText;  // 설비이상 (가장 심각)
        [SerializeField] private TextMeshProUGUI redCountText;     // 경보 (두번째 심각)
        [SerializeField] private TextMeshProUGUI yellowCountText;  // 경계 (세번째 심각)
        // RankingNumberPanel의 숫자는 색깔 변경 안하므로 따로 참조 불필요

        public void UpdateData(YearlyAlarmRankingData data)
        {
            if (areaNameText != null)
                areaNameText.text = data.AreaName;

            if (purpleCountText != null)
                purpleCountText.text = data.PurpleCount.ToString();

            if (yellowCountText != null)
                yellowCountText.text = data.YellowCount.ToString();

            if (redCountText != null)
                redCountText.text = data.RedCount.ToString();
        }
    }
}