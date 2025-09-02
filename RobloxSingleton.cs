using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace RAMRewrite.Utils
{
public static class RobloxSingleton
{
private const uint EVENT_ALL_ACCESS = 0x1F0003;


[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
private static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);


[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool CloseHandle(IntPtr hObject);


public static bool CloseSingletonEvent()
{
IntPtr hEvent = OpenEvent(EVENT_ALL_ACCESS, false, "ROBLOX_singletonEvent");
if (hEvent == IntPtr.Zero)
{
return false; // No singleton found
}
bool result = CloseHandle(hEvent);
return result;
}
}
}