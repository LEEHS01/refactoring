using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    public class MonthlyAlarmTop5View : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private List<MonthlyAlarmRankingItemView> rankingItems;

        private void Start()
        {
            if (MonthlyAlarmTop5ViewModel.Instance == null)
            {
                Debug.LogError("[MonthlyAlarmTop5View] MonthlyAlarmTop5ViewModel.Instance가 null입니다!");
                return;
            }

            // 제목 초기화
            UpdateTitle();

            // ViewModel 이벤트 구독 (+=로 변경!)
            MonthlyAlarmTop5ViewModel.Instance.OnTitleUpdated += UpdateTitle;
            MonthlyAlarmTop5ViewModel.Instance.OnRankingUpdated += UpdateRanking;

            // 즉시 데이터 로드 요청
            MonthlyAlarmTop5ViewModel.Instance.RefreshRanking();

            Debug.Log("[MonthlyAlarmTop5View] 초기화 완료");
        }

        private void OnDestroy()
        {
            if (MonthlyAlarmTop5ViewModel.Instance != null)
            {
                // 이벤트 구독 해제 (-=로 변경!)
                MonthlyAlarmTop5ViewModel.Instance.OnTitleUpdated -= UpdateTitle;
                MonthlyAlarmTop5ViewModel.Instance.OnRankingUpdated -= UpdateRanking;
            }
        }

        private void UpdateTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
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

        private void UpdateRanking(List<MonthlyAlarmRankingData> rankings)
        {
            Debug.Log($"[MonthlyAlarmTop5View] 랭킹 업데이트: {rankings?.Count ?? 0}개");

            if (rankings == null || rankingItems == null)
            {
                Debug.LogWarning("[MonthlyAlarmTop5View] rankings 또는 rankingItems가 null입니다!");
                return;
            }

            for (int i = 0; i < rankingItems.Count; i++)
            {
                if (i < rankings.Count)
                {
                    rankingItems[i].UpdateData(rankings[i]);
                    rankingItems[i].gameObject.SetActive(true);
                }
                else
                {
                    rankingItems[i].gameObject.SetActive(false);
                }
            }
        }
    }
}