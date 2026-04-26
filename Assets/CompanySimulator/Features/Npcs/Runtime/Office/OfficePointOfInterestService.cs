using System.Collections.Generic;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    [DisallowMultipleComponent]
    public sealed class OfficePointOfInterestService : MonoBehaviour
    {
        public bool TrySelectPoint(OfficeWorkerNpcRuntimeData worker, out OfficePointOfInterest pointOfInterest)
        {
            pointOfInterest = null;
            if (worker == null)
            {
                return false;
            }

            var allPoints = FindObjectsOfType<OfficePointOfInterest>(true);
            if (allPoints == null || allPoints.Length == 0)
            {
                return false;
            }

            var candidates = new List<OfficePointOfInterest>(allPoints.Length);
            var totalWeight = 0f;
            for (var i = 0; i < allPoints.Length; i++)
            {
                var point = allPoints[i];
                if (point == null || !point.isActiveAndEnabled || !point.CanReserve(worker.RuntimeId))
                {
                    continue;
                }

                candidates.Add(point);
                totalWeight += Mathf.Max(0.01f, point.SelectionWeight);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var roll = Random.Range(0f, Mathf.Max(0.01f, totalWeight));
            var cumulative = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                var point = candidates[i];
                cumulative += Mathf.Max(0.01f, point.SelectionWeight);
                if (roll <= cumulative && point.TryReserve(worker.RuntimeId))
                {
                    pointOfInterest = point;
                    return true;
                }
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].TryReserve(worker.RuntimeId))
                {
                    pointOfInterest = candidates[i];
                    return true;
                }
            }

            return false;
        }
    }
}
