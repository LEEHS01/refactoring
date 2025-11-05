using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 월간 알람 Top 5 패널 View
    /// </summary>
    public class MonthlyAlarmTop5View : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private List<MonthlyAlarmRankingItemView> rankingItems;

        [Header("Chart (Optional)")]
        [SerializeField] private GameObject doughnutChart;

        private void Start()
        {
            UpdateTitle();

            // ViewModel 이벤트 구독
            if (MonthlyAlarmTop5ViewModel.Instance != null)
            {
                MonthlyAlarmTop5ViewModel.Instance.OnRankingUpdated.AddListener(UpdateRanking);

                // 즉시 데이터 로드 요청 (이미 로드되었을 수 있으니)
                MonthlyAlarmTop5ViewModel.Instance.RefreshRanking();

                Debug.Log("[MonthlyAlarmTop5View] ViewModel 이벤트 구독 완료 및 데이터 요청");
            }
            else
            {
                Debug.LogError("❌ MonthlyAlarmTop5ViewModel을 찾을 수 없습니다!");
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (MonthlyAlarmTop5ViewModel.Instance != null)
            {
                MonthlyAlarmTop5ViewModel.Instance.OnRankingUpdated.RemoveListener(UpdateRanking);
            }
        }

        private void UpdateTitle()
        {
            if (titleText != null)
            {
                int currentMonth = DateTime.Now.Month;
                titleText.text = $"월간 발생 건수 ({currentMonth}월)";
            }
        }

        public void UpdateRanking(List<MonthlyAlarmRankingData> rankingData)
        {
            Debug.Log($"[MonthlyAlarmTop5View] UpdateRanking 호출됨! 데이터 개수: {rankingData?.Count ?? 0}");

            if (rankingData == null || rankingItems == null)
            {
                Debug.LogWarning("[MonthlyAlarmTop5View] rankingData 또는 rankingItems가 null입니다!");
                return;
            }

            if (rankingItems.Count == 0)
            {
                Debug.LogError("[MonthlyAlarmTop5View] rankingItems가 비어있습니다! Inspector에서 연결하세요!");
                return;
            }

            // 각 아이템 업데이트
            for (int i = 0; i < rankingItems.Count; i++)
            {
                if (rankingItems[i] == null)
                {
                    Debug.LogWarning($"[MonthlyAlarmTop5View] rankingItems[{i}]가 null입니다!");
                    continue;
                }

                if (i < rankingData.Count)
                {
                    Debug.Log($"[MonthlyAlarmTop5View] 아이템 {i} 업데이트: {rankingData[i].AreaName} - {rankingData[i].AlarmCount}건");
                    rankingItems[i].UpdateData(rankingData[i]);
                }
                else
                {
                    Debug.Log($"[MonthlyAlarmTop5View] 아이템 {i} 비우기");
                    rankingItems[i].UpdateData(null);
                }
            }

            UpdateTitle();
            Debug.Log($"📊 월간 Top 5 UI 업데이트 완료: {rankingData.Count}개 지역");
        }
    }
}