using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Models;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    internal static class EmployeeApplicationsPage
    {
        public static void Render(IReadOnlyList<EmployeeRuntimeData> applicants, Action<string> createInfoCard, Action<EmployeeRuntimeData> createApplicantButton)
        {
            if (applicants.Count == 0)
            {
                createInfoCard("Bu meslek için bekleyen başvuru bulunmuyor.");
                return;
            }

            for (var i = 0; i < applicants.Count; i++)
            {
                createApplicantButton(applicants[i]);
            }
        }
    }
}
