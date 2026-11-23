using CoreFoundation;
using Foundation;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Führt Arbeit auf dem Hauptthread aus — tvOS-Pendant zu
    /// <c>MainThread.InvokeOnMainThreadAsync</c>: UIKit-Objekte und die
    /// Rückmeldungen in den MVU-Zyklus sind hauptthread-gebunden.
    /// </summary>
    public static class MainThreadInvoker
    {
        /// <summary>Führt eine asynchrone Arbeit auf dem Hauptthread aus und wartet ihren Abschluss.</summary>
        /// <param name="work">Die auf dem Hauptthread auszuführende Arbeit.</param>
        /// <returns>Eine Aufgabe, die mit dem Abschluss (oder Fehler) der Arbeit endet.</returns>
        public static Task InvokeOnMainThreadAsync(Func<Task> work)
        {
            if (NSThread.IsMain)
            {
                return work();
            }

            var completion = new TaskCompletionSource();
            DispatchQueue.MainQueue.DispatchAsync(async () =>
            {
                try
                {
                    await work();
                    completion.TrySetResult();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });
            return completion.Task;
        }

        /// <summary>Führt eine synchrone Arbeit auf dem Hauptthread aus.</summary>
        /// <param name="work">Die auf dem Hauptthread auszuführende Arbeit.</param>
        public static void InvokeOnMainThread(Action work)
        {
            if (NSThread.IsMain)
            {
                work();
                return;
            }

            DispatchQueue.MainQueue.DispatchAsync(work);
        }
    }
}
