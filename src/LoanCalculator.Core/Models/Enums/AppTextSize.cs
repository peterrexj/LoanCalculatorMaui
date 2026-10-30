namespace LoanCalculator.Core.Models.Enums
{
    /// <summary>
    /// The four steps of the app-wide text-size setting (Settings ▸ Text size).
    /// </summary>
    /// <remarks>
    /// Persisted by NAME (see <c>SharedServiceCore.GetAppTextSizeAsync</c>), not by the numeric
    /// factor it currently maps to — so a user's saved choice of "Large" stays "Large" even if the
    /// factor FontScaleHandler attaches to it is ever retuned. Four steps, matching what the user
    /// asked for when this feature was planned: not "every role independently adjustable", one
    /// shared scale with a small number of discrete choices.
    /// </remarks>
    public enum AppTextSize
    {
        Small,
        Default,
        Large,
        ExtraLarge,
    }
}
