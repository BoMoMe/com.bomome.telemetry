using System;
using System.Collections.Generic;

namespace Bomome.Telemetry
{
    public enum TutorialProgress { Started, StepStarted, StepCompleted, StepSkipped, Completed, Skipped }
    /// <summary>Explicit instrumentation only; never controls tutorial state.</summary>
    public sealed class TutorialTelemetry
    {
        private readonly ITelemetryService telemetry;
        public TutorialTelemetry(ITelemetryService telemetry)
        { this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry)); }
        public void Record(string tutorialId, string attemptId, TutorialProgress progress, string stepId = "")
        {
            if (string.IsNullOrWhiteSpace(tutorialId) || string.IsNullOrWhiteSpace(attemptId))
                throw new ArgumentException("Tutorial and attempt IDs are required.");
            if (!Enum.IsDefined(typeof(TutorialProgress), progress)) throw new ArgumentOutOfRangeException(nameof(progress));
            if ((progress == TutorialProgress.StepStarted || progress == TutorialProgress.StepCompleted ||
                 progress == TutorialProgress.StepSkipped) && string.IsNullOrWhiteSpace(stepId))
                throw new ArgumentException("Step events need a stable step ID.");
            string suffix = progress == TutorialProgress.StepStarted ? "step_started"
                : progress == TutorialProgress.StepCompleted ? "step_completed"
                : progress == TutorialProgress.StepSkipped ? "step_skipped" : progress.ToString().ToLowerInvariant();
            telemetry.Track(new TelemetryEvent("tutorial_" + suffix,
                TelemetryCategory.Tutorial, new Dictionary<string, object> {
                    ["tutorial_id"] = tutorialId, ["attempt_id"] = attemptId, ["step_id"] = stepId ?? ""
                }));
        }
    }
}
