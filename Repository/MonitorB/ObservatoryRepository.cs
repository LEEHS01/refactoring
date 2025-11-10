using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Services;

namespace Repositories.MonitorB
{
    /// <summary>
    /// 관측소 데이터 Repository
    /// </summary>
    public class ObservatoryRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 관측소 CCTV URL 조회
        /// </summary>
        public IEnumerator GetObservatoryCCTV(
            int obsId,
            Action<Models.MonitorB.ObservatoryModel> onSuccess,  // ⭐ Models. 명시
            Action<string> onError)
        {
            yield return Database.ExecuteProcedure(
                "GET_OBS",
                null,
                (List<Models.MonitorB.ObservatoryModel> allObs) =>  // ⭐ Models. 명시
                {
                    if (allObs == null || allObs.Count == 0)
                    {
                        onError?.Invoke("관측소 데이터를 불러올 수 없습니다.");
                        return;
                    }

                    var obs = allObs.Find(o => o.OBSIDX == obsId);

                    if (obs != null)
                    {
                        Debug.Log($"[ObservatoryRepository] 관측소 {obsId} 조회 성공");
                        Debug.Log($"[ObservatoryRepository] OUT_CCTVURL: {obs.OUT_CCTVURL}");
                        Debug.Log($"[ObservatoryRepository] IN_CCTVURL: {obs.IN_CCTVURL}");
                        onSuccess?.Invoke(obs);
                    }
                    else
                    {
                        onError?.Invoke($"관측소 {obsId}를 찾을 수 없습니다.");
                    }
                },
                onError
            );
        }
    }
}