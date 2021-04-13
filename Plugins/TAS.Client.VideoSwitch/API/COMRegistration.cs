using System;
using System.Runtime.InteropServices;

namespace TAS.Server.VideoSwitch.API
{
    /// <summary>
    /// Class not used at the moment. It registers Atem COM DLL.
    /// </summary>
    public class COMRegistration
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate UInt32 DllRegUnRegAPI();

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string strLibraryName);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern Int32 FreeLibrary(IntPtr hModule);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        public static void RegisterBMDSwitcherAPI64()
        {
            IntPtr hModuleDLL = LoadLibrary("BMDSwitcherAPI64.dll");
            if (hModuleDLL == IntPtr.Zero)
            {
                Logger.Error("Could not load library BMDSwitcherAPI64");                
                return;
            }

            IntPtr pExportedFunction = GetProcAddress(hModuleDLL, "DllRegisterServer");
            if (pExportedFunction == IntPtr.Zero)
            {
                Logger.Error("Unable to get required API from DLL.");
                return;
            }

            DllRegUnRegAPI pDelegateRegUnReg = (DllRegUnRegAPI)(Marshal.GetDelegateForFunctionPointer(pExportedFunction, typeof(DllRegUnRegAPI)));
            UInt32 hResult = pDelegateRegUnReg();

            if (hResult != 0)
            {
                Logger.Error("Unable to register COM object");                
            }

            FreeLibrary(hModuleDLL);
            hModuleDLL = IntPtr.Zero;
        }
    }
}
