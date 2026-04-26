using System.Collections.Generic;
using CompanySimulator.Features.Furniture.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    [DisallowMultipleComponent]
    public sealed class OfficeDeskCapacityService : MonoBehaviour
    {
        public IReadOnlyList<SeatController> GetAllEmployeeSeats()
        {
            var desks = FindObjectsOfType<OfficeDeskController>(true);
            var seats = new List<SeatController>(16);
            for (var i = 0; i < desks.Length; i++)
            {
                var desk = desks[i];
                if (desk == null)
                {
                    continue;
                }

                var deskSeats = desk.GetEmployeeSeats();
                for (var j = 0; j < deskSeats.Count; j++)
                {
                    var seat = deskSeats[j];
                    if (seat != null)
                    {
                        seats.Add(seat);
                    }
                }
            }

            return seats;
        }

        public int GetTotalCapacity()
        {
            return GetAllEmployeeSeats().Count;
        }
    }
}
