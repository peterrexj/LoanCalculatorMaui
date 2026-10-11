using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Services
{
    public class NameValueDataService(ILocalStorage localStorage) : INameValueDataService
    {
        private NameValueDataModel? _nameValueDataModel;
        private readonly object _gate = new();
        private Task<NameValueDataModel?>? _loadTask;

        // Serialises writes to namevaluedata.json. SaveNameValueData is fire-and-forget by
        // design (callers must not block on a metadata write), but two un-coordinated
        // File.WriteAllTextAsync calls to the SAME path — e.g. SetAppLaunchDisclaimerShown and
        // SetWizardShown both saving in close succession — can genuinely interleave at the OS
        // level. That is exactly how this file was found corrupted on a real device:
        //   {"HasShowAppLaunchDisclaimer":true,"HasShownWizard":true}}
        // — a stray trailing brace, both flags present, from two overlapping writes. Once on
        // disk, EVERY later launch's read of this file throws (see the read-side fix below),
        // which crashed on the native marshal boundary with no inner-exception detail
        // recoverable from the crash report — hours of searching for a thread-pool-contention
        // or Sentry-SDK cause before a temporary AppDomain.FirstChanceException hook finally
        // surfaced the real JsonException. This semaphore makes concurrent SaveNameValueData
        // calls queue instead of race.
        private readonly SemaphoreSlim _saveLock = new(1, 1);

        public NameValueDataModel NameValueDataModel
        {
            get
            {
                if (_nameValueDataModel != null) return _nameValueDataModel;

                // Single-flight the disk read. SplashPage.PreWarmAsync reads this property on a
                // background thread specifically to cache it before LoanView's disclaimer popup
                // binds IsOpen on the main thread — without this gate, the main thread's read can
                // land while that background read is still in flight, see _nameValueDataModel
                // still null, and start a second concurrent read of the same file. Every caller
                // now waits on the SAME in-flight Task instead of starting its own — called during
                // App startup on the main thread, so Task.Run (not a direct await) still avoids a
                // sync-context deadlock on Android/iOS when the read completes.
                Task<NameValueDataModel?> loadTask;
                lock (_gate)
                {
                    if (_nameValueDataModel != null) return _nameValueDataModel;
                    _loadTask ??= Task.Run(() => localStorage.GetData<NameValueDataModel>());
                    loadTask = _loadTask;
                }

                // A malformed file (see the comment on _saveLock above for how one such file was
                // actually produced) must not crash every future launch. Treat a read/parse
                // failure exactly like "file doesn't exist yet": fall back to a fresh default and
                // overwrite the bad file with a clean one, so the app self-heals on the very next
                // save instead of throwing on every single launch forever.
                NameValueDataModel? loaded;
                try
                {
                    loaded = loadTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NameValueDataService] Corrupt or unreadable namevaluedata.json, resetting: {ex.Message}");
                    loaded = null;
                }

                lock (_gate)
                {
                    if (_nameValueDataModel == null)
                    {
                        _nameValueDataModel = loaded ?? new NameValueDataModel
                        {
                            HasShowAppLaunchDisclaimer = false,
                        };
                        if (loaded == null) SaveNameValueData(_nameValueDataModel);
                    }
                    return _nameValueDataModel;
                }
            }
        }

        public void SaveNameValueData(NameValueDataModel value = null)
        {
            // Captured before handing off to the background task: NameValueDataModel (the
            // fallback below) re-enters this same lazy getter, and evaluating it here, on the
            // caller's thread, keeps that re-entrancy exactly where it already was rather than
            // moving it onto the background task.
            var toSave = value ?? NameValueDataModel;

            _ = Task.Run(async () =>
            {
                await _saveLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await localStorage.SaveData(toSave).ConfigureAwait(false);
                }
                catch { /* non-critical metadata — swallow */ }
                finally
                {
                    _saveLock.Release();
                }
            });
        }
    }
}
