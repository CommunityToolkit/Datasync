using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TodoApp.Uno.Droid;
[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{

    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
        // Uno Platform 6.0 handles image loading entirely with the Skia Rendering mode, so the
        // UniversalImageLoader configuration previously required here has been removed.
        // See https://platform.uno/docs/articles/migrating-to-uno-6.html.
    }
}

