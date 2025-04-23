using System.Collections.ObjectModel;
using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Themes;

public interface IThemeHandler
{
    Task<AppThemes?> GetCurrentThemeAsync();
    void LoadDefaultStyle();
    void LoadDefaultStyle(AppThemes appTheme);
    ObservableCollection<Brush> GetChartColors();
}