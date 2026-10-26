using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// One row of the PDF export's section list.
    /// </summary>
    /// <remarks>
    /// Read-only by design for now. The export produces a fixed document — <c>PdfInsightsGenerator</c>
    /// builds its own <c>PdfDataInsightsModel</c> and takes no section list — so <see cref="IsIncluded"/>
    /// describes what the report already contains rather than offering a choice. The checkboxes in the
    /// UI are disabled to match, which is why this type has no setters and raises no notifications:
    /// nothing can change it yet, and a settable flag would invite wiring a control to a value the
    /// generator ignores.
    ///
    /// When per-section export does land, this gains a notifying <c>IsIncluded</c> and the generator
    /// gains a parameter. Until then the honesty is in the disabled control plus the note beside it.
    /// </remarks>
    public class ExportSectionOption
    {
        public ExportSectionOption(string automationId, string title, string detail, bool isIncluded = true)
        {
            AutomationId = automationId;
            Title = title;
            Detail = detail;
            IsIncluded = isIncluded;
        }

        /// <summary>Stable id for the UI tests, since the rows are generated from this list.</summary>
        [JsonIgnore] public string AutomationId { get; }

        [JsonIgnore] public string Title { get; }

        [JsonIgnore] public string Detail { get; }

        [JsonIgnore] public bool IsIncluded { get; }
    }
}
