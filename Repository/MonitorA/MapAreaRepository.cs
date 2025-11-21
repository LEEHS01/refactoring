using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.Common.Models;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//  명시적으로 Models의 타입 사용
using AlarmLogModel = Models.AlarmLogModel;
using ObservatoryModel = Models.MonitorB.ObservatoryModel;

namespace HNS.MonitorA.Repositories
{
    public class MapAreaRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        private static readonly string[] AreaImagePaths = new string[]
        {
            "Image/AreaBackground/InCheon",
            "Image/AreaBackground/PyeongTaek",
            "Image/AreaBackground/YeoSu",
            "Image/AreaBackground/BuSan",
            "Image/AreaBackground/UlSan",
            "Image/AreaBackground/BoRyeong",
            "Image/AreaBackground/YeongGwang",
            "Image/AreaBackground/SaCheon",
            "Image/AreaBackground/GoRi",
            "Image/AreaBackground/DongHae"
        };

        private List<List<Vector3>> _observatoryPositions;

        public MapAreaRepository()
        {
            LoadObservatoryPositions();
        }

        /// <summary>
        /// 지역 정보 조회
        /// </summary>
        public IEnumerator GetAreaInfo(
            int areaId,
            Action<AreaInfoData> onSuccess,
            Action<string> onError)
        {
            if (areaId < 1 || areaId > 10)
            {
                onError?.Invoke("AreaId는 1~10 사이여야 합니다.");
                yield break;
            }

            // GET_OBS 호출
            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (obsModels) =>
                {
                    var areaObs = obsModels.Where(o => o.AREAIDX == areaId).ToList();

                    if (areaObs.Count == 0)
                    {
                        onError?.Invoke($"지역 정보를 찾을 수 없습니다: AreaId={areaId}");
                        return;
                    }

                    onSuccess?.Invoke(new AreaInfoData
                    {
                        AreaId = areaId,
                        AreaName = areaObs[0].AREANM,
                        ImagePath = AreaImagePaths[areaId - 1]
                    });
                },
                onError
            );
        }

        /// <summary>
        /// 관측소 마커 데이터 조회
        /// </summary>
        public IEnumerator GetObservatoryMarkers(
            int areaId,
            Action<List<ObsMarkerData>> onSuccess,
            Action<string> onError)
        {
            if (areaId < 1 || areaId > 10)
            {
                onError?.Invoke("AreaId는 1~10 사이여야 합니다.");
                yield break;
            }

            int areaIdx = areaId - 1;
            var positions = _observatoryPositions[areaIdx];

            List<ObservatoryModel> areaObs = null;
            List<AlarmLogModel> alarms = null;

            // 1. GET_OBS 호출
            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (models) => areaObs = models.Where(o => o.AREAIDX == areaId).ToList(),
                onError
            );

            if (areaObs == null)
            {
                onError?.Invoke("관측소 조회 실패");
                yield break;
            }

            // 2. GET_CURRENT_ALARM_LOG 호출
            yield return Database.ExecuteProcedure<AlarmLogModel>(
                "GET_CURRENT_ALARM_LOG",
                null,
                (models) => alarms = models,
                onError
            );

            if (alarms == null)
            {
                onError?.Invoke("알람 조회 실패");
                yield break;
            }

            // 3. 마커 데이터 생성
            var markerDataList = new List<ObsMarkerData>();

            for (int i = 0; i < areaObs.Count; i++)
            {
                var obs = areaObs[i];
                var position = i < positions.Count ? positions[i] : Vector3.zero;
                ToxinStatus status = CalculateObsStatus(obs.OBSIDX, alarms);

                markerDataList.Add(new ObsMarkerData
                {
                    ObsId = obs.OBSIDX,
                    ObsName = obs.OBSNM,
                    Status = status,
                    LocalPosition = position
                });
            }

            onSuccess?.Invoke(markerDataList);
        }

        /// <summary>
        /// 관측소 상태 계산
        /// </summary>
        private ToxinStatus CalculateObsStatus(int obsId, List<AlarmLogModel> alarms)
        {
            var obsAlarms = alarms.Where(a => a.OBSIDX == obsId).ToList();

            if (obsAlarms.Count == 0)
                return ToxinStatus.Green;

            int maxAlaCode = obsAlarms.Max(a => a.ALACODE);

            return maxAlaCode switch
            {
                2 => ToxinStatus.Red,
                1 => ToxinStatus.Yellow,
                0 => ToxinStatus.Purple,
                _ => ToxinStatus.Green
            };
        }

        /// <summary>
        /// Prefab에서 관측소 위치 로드
        /// </summary>
        private void LoadObservatoryPositions()
        {
            string prefabPath = "Prefab/MapAreaLoadout";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab == null)
            {
                throw new Exception($"[MapAreaRepository] Prefab을 찾을 수 없습니다: {prefabPath}");
            }

            _observatoryPositions = new List<List<Vector3>>();

            foreach (Transform areaChild in prefab.transform)
            {
                List<Vector3> areaPositions = new List<Vector3>();

                foreach (Transform obsChild in areaChild)
                {
                    areaPositions.Add(obsChild.position);
                }

                _observatoryPositions.Add(areaPositions);
            }

            Debug.Log($"[MapAreaRepository] 관측소 위치 로드 완료: {_observatoryPositions.Count}개 지역");
        }
    }
}