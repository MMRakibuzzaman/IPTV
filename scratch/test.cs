using System;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom("C:\Users\rakib\.nuget\packages\libvlcsharp.android\3.8.5\lib\monoandroid90\LibVLCSharp.Android.dll");
        var type = asm.GetType("LibVLCSharp.Platforms.Android.VideoView");
        Console.WriteLine("BaseType: " + type.BaseType.FullName);
    }
}
