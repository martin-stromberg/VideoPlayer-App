using ObjCRuntime;
using UIKit;

namespace VideoPlayer.Maui
{
    /// <summary>Einstiegspunkt der iOS-App.</summary>
    public class Program
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class than "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
