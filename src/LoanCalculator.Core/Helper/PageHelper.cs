namespace LoanCalculator.Core.Helper
{
    public static class PageHelper
    {
        private static bool _isFormLoading { get; set; }
        public static bool IsFormLoading => _isFormLoading;

        public static void PageIsLoading()
        {
            _isFormLoading = true;
        }

        public static void PageLoadingComplete()
        {
            _isFormLoading = false;
        }
    }
}
