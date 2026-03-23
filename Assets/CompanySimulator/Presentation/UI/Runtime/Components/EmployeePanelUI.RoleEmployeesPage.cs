using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class EmployeeRoleEmployeesPage
    {
        public static void Render(IReadOnlyList<EmployeeRuntimeData> employees, Action<string, float> createInfoCard, Action<EmployeeRuntimeData> createEmployeeCard)
        {
            if (employees.Count == 0)
            {
                createInfoCard("Bu meslekte çalışan bulunmuyor.", 58f);
            }
            else
            {
                for (var i = 0; i < employees.Count; i++)
                {
                    createEmployeeCard(employees[i]);
                }
            }
        }
    }
}
