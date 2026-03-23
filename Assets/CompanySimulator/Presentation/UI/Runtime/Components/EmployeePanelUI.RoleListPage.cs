using System;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Definitions;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class EmployeeRoleListPage
    {
        public static void Render(EmployeeManager employeeManager, Action<string> createInfoCard, Action<EmployeeRoleDefinition> createRoleButton)
        {
            if (employeeManager == null)
            {
                createInfoCard("Çalışan sistemi henüz hazır değil.");
                return;
            }

            var roles = employeeManager.Roles;
            if (roles.Count == 0)
            {
                createInfoCard("Henüz meslek listesi bulunmuyor.");
                return;
            }

            for (var i = 0; i < roles.Count; i++)
            {
                createRoleButton(roles[i]);
            }
        }
    }
}
