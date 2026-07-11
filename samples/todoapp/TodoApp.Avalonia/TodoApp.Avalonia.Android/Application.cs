using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace TodoApp.Avalonia.Android;

// Avalonia 12 changed Android app initialization so that CreateAppBuilder()/CustomizeAppBuilder()
// are handled by an AvaloniaAndroidApplication<TApp> subclass instead of AvaloniaMainActivity<TApp>.
// See https://docs.avaloniaui.net/docs/avalonia12-breaking-changes#android.
[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
