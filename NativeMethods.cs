using System;
using System.Runtime.InteropServices;

namespace IEProxyTest
{
    class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct InternetPerConnectionOptionList
        {
            public int Size;

            // The connection to be set. NULL means LAN.
            public IntPtr Connection;

            public int OptionCount;
            public int OptionError;

            // List of INTERNET_PER_CONN_OPTIONs.
            public IntPtr OptionsBufferPointer;
        }

        public enum InternetOption
        {
            // Sets or retrieves an INTERNET_PER_CONN_OPTION_LIST structure that specifies
            // a list of options for a particular connection.
            PerConnectionOption = 75,

            // Notify the system that the registry settings have been changed so that
            // it verifies the settings on the next call to InternetConnect.
            SettingsChanged = 39,

            // Causes the proxy data to be reread from the registry for a handle.
            Refresh = 37

        }

        public enum InternetPerConnectionOptionValue
        {
            Flags = 1,
            ProxyServer = 2,
            ProxyBypass = 3,
            AutoConfigUrl = 4,
            AutoDiscoveryFlags = 5,
            AutoConfigSecondaryUrl = 6,
            AutoConfigReloadDelayMins = 7,
            AutoConfigLastDetectTime = 8,
            AutoConfigLastDetectUrl = 9,
            FlagsUi = 10
        }

        public const int INTERNET_OPEN_TYPE_PROXY = 3;
        public const int INTERNET_OPEN_TYPE_DIRECT = 1;  // direct to net
        public const int INTERNET_OPEN_TYPE_PRECONFIG = 0; // read registry

        /// <summary>
        /// Constants used in INTERNET_PER_CONN_OPTON struct.
        /// </summary>
        public enum InternetPerConnectionProxyFlags
        {
            Direct = 0x00000001,   // direct to net
            Proxy = 0x00000002,   // via named proxy
            AutoProxyUrl = 0x00000004,   // autoproxy URL
            AutoDetect = 0x00000008   // use autoproxy detection
        }

        /// <summary>
        /// Used in INTERNET_PER_CONN_OPTION.
        /// When create a instance of OptionUnion, only one filed will be used.
        /// The StructLayout and FieldOffset attributes could help to decrease the struct size.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct InternetPerConnectionOptionData
        {
            // A value in INTERNET_OPTION_PER_CONN_FLAGS.
            [FieldOffset(0)]
            public int integerValue;
            [FieldOffset(0)]
            public System.IntPtr stringValue;
            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME fileTimeValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InternetPerConnectionOption
        {
            // A value in INTERNET_PER_CONN_OptionEnum.
            public int Option;
            public InternetPerConnectionOptionData Value;
        }

        /// <summary>
        /// Sets an Internet option.
        /// </summary>
        [DllImport("wininet.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool InternetSetOption(IntPtr hInternet, InternetOption dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        /// <summary>
        /// Queries an Internet option on the specified handle. The Handle will be always 0.
        /// </summary>
        [DllImport("wininet.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "InternetQueryOption")]
        public extern static bool InternetQueryOptionList(IntPtr Handle, InternetOption OptionFlag, ref InternetPerConnectionOptionList OptionList, ref int size);


        public const int INTERNET_OPTION_PROXY = 38;
    }
}
