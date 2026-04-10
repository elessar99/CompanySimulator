using System;
using CompanySimulator.Features.Finance.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Time.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class TimeManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField, Range(0, 23)] private int workdayStartHour = 8;
        [SerializeField, Range(0, 59)] private int workdayStartMinute;
        [SerializeField, Range(0, 23)] private int workdayEndHour = 16;
        [SerializeField, Range(0, 59)] private int workdayEndMinute;
        [SerializeField, Min(0.1f)] private float realMinutesPerGameHour = 1f;
        [SerializeField] private bool autoAdvanceAtMidnight = true;
        [SerializeField] private bool runClock = true;
        [SerializeField, Min(0)] private int currentTotalMinutes;

        private float minuteProgress;

        public event Action<int, int> TimeChanged;

        public int CurrentTotalMinutes => Mathf.Clamp(currentTotalMinutes, 0, MinutesPerDay);
        public int CurrentHour => CurrentTotalMinutes >= MinutesPerDay ? 0 : CurrentTotalMinutes / 60;
        public int CurrentMinute => CurrentTotalMinutes >= MinutesPerDay ? 0 : CurrentTotalMinutes % 60;
        public string CurrentTimeLabel => $"{CurrentHour:00}:{CurrentMinute:00}";
        public int WorkdayStartTotalMinutes => workdayStartHour * 60 + workdayStartMinute;
        public int WorkdayEndTotalMinutes => workdayEndHour * 60 + workdayEndMinute;
        public float WorkdayDurationHours => Mathf.Max(1f, (GetSafeWorkdayEndTotalMinutes() - WorkdayStartTotalMinutes) / 60f);

        private const int MinutesPerDay = 24 * 60;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            ClampCurrentTime();
            EnsureValidWorkdayRange();
        }

        private void OnEnable()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
            }

            EmitTimeChanged();
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
            }
        }

        private void OnValidate()
        {
            EnsureValidWorkdayRange();
            ClampCurrentTime();
        }

        private void Start()
        {
            if (currentTotalMinutes <= 0)
            {
                ResetToWorkdayStart();
            }
            else
            {
                EmitTimeChanged();
            }
        }

        private void Update()
        {
            if (!runClock || realMinutesPerGameHour <= 0f)
            {
                return;
            }

            minuteProgress += UnityEngine.Time.deltaTime / realMinutesPerGameHour;
            while (minuteProgress >= 1f)
            {
                minuteProgress -= 1f;
                AdvanceMinute();
            }
        }

        public void ResetToWorkdayStart()
        {
            currentTotalMinutes = WorkdayStartTotalMinutes;
            minuteProgress = 0f;
            EmitTimeChanged();
        }

        public float GetOfficeOvertimeHours()
        {
            var overtimeMinutes = Mathf.Max(0, CurrentTotalMinutes - GetSafeWorkdayEndTotalMinutes());
            return overtimeMinutes / 60f;
        }

        private void AdvanceMinute()
        {
            if (currentTotalMinutes < MinutesPerDay)
            {
                currentTotalMinutes++;
            }

            EmitTimeChanged();

            if (currentTotalMinutes < MinutesPerDay || !autoAdvanceAtMidnight)
            {
                return;
            }

            economyManager ??= FindObjectOfType<EconomyManager>();
            if (economyManager != null)
            {
                economyManager.AdvanceDay();
            }
        }

        private void HandleDayAdvanced(int _)
        {
            ResetToWorkdayStart();
        }

        private void EmitTimeChanged()
        {
            TimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        private void EnsureValidWorkdayRange()
        {
            var startTotal = WorkdayStartTotalMinutes;
            var endTotal = WorkdayEndTotalMinutes;
            if (endTotal > startTotal)
            {
                return;
            }

            endTotal = Mathf.Min(MinutesPerDay - 1, startTotal + (8 * 60));
            workdayEndHour = endTotal / 60;
            workdayEndMinute = endTotal % 60;
        }

        private void ClampCurrentTime()
        {
            currentTotalMinutes = Mathf.Clamp(currentTotalMinutes, 0, MinutesPerDay);
        }

        private int GetSafeWorkdayEndTotalMinutes()
        {
            return Mathf.Max(WorkdayStartTotalMinutes + 60, WorkdayEndTotalMinutes);
        }
    }
}
