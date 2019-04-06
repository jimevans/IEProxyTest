using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SHDocVw;
using FryProxy;
using FryProxy.Logging;

namespace IEProxyTest
{
    class Program
    {
        private static List<string> uris = new List<string>();
        private static HttpProxyServer server;

        [STAThread]
        static void Main(string[] args)
        {
            // Note: The "local" URLs below can be achieved by adding entries to one's
            // hosts file (C:\Windows\System32\drivers\etc\hosts), similar to the following:
            //     127.0.0.1   www.seleniumhq-test.test
            //     127.0.0.1   www.seleniumhq- test-alternate.test
            // The "remote" URL can be any URL to a server not hosted on localhost.
            const string LocalBypassedUrl = "http://www.seleniumhq-test-alternate.test:2310/common/simpleTest.html";
            const string LocalProxiedUrl = "http://www.seleniumhq-test.test:2310/common/simpleTest.html";
            const string RemoteUrl = "http://webdriver-herald.herokuapp.com";

            // To avoid logging all of the traffic put through the proxy, set to false.
            bool isLoggingDebugInfoFromProxy = true;
            string hostName = "127.0.0.1";

            Console.WriteLine("Starting proxy instance");
            StartProxyServer(hostName, isLoggingDebugInfoFromProxy);
            Console.WriteLine("Proxy started on {0}:{1}", hostName, server.ProxyEndPoint.Port);

            string proxySetting = string.Format("http={0}:{1}", hostName, server.ProxyEndPoint.Port);

            Console.WriteLine("Getting current proxy settings");
            var originalProxySettings = GetSystemProxySettings();

            Console.WriteLine("Generating custom proxy settings");
            var customProxySettings = CreateCustomProxySettings(proxySetting, "www.seleniumhq-test-alternate.test");

            Console.WriteLine("Launching Internet Explorer");
            InternetExplorer ie = new InternetExplorer();
            ie.Visible = true;

            Console.WriteLine("Setting proxy settings to custom settings");
            SetSystemProxySettings(customProxySettings);
            Thread.Sleep(5000);

            Console.WriteLine("Navigating to bypassed URL (should not be proxied)");
            ie.Navigate2(LocalBypassedUrl);
            Thread.Sleep(5000);

            Console.WriteLine("Navigating to local URL with hosts file alias (should be proxied)");
            ie.Navigate2(LocalProxiedUrl);
            Thread.Sleep(5000);

            Console.WriteLine("Navigating to non-local URL (should be proxied)");
            ie.Navigate2(RemoteUrl);
            Thread.Sleep(5000);

            Console.WriteLine("Restoring proxy settings to orignal settings");
            SetSystemProxySettings(originalProxySettings);

            Console.WriteLine("Closing Internet Explorer");
            ie.Quit();

            Console.WriteLine("Shutting down proxy on {0}:{1}", server.ProxyEndPoint.Address.ToString(), server.ProxyEndPoint.Port);
            StopProxyServer();
            Console.WriteLine("Proxy instance shut down");

            Console.WriteLine();
            Console.WriteLine("==================================");
            Console.WriteLine("Resources requested through proxy:");
            Console.WriteLine("----------------------------------");
            Console.WriteLine(string.Join("\n", uris.ToArray()));
            Console.WriteLine("==================================");
            Console.WriteLine();
            Console.WriteLine("Press <enter> to quit");
            Console.ReadLine();
        }

        private static void StartProxyServer(string hostName, bool logDebugProxyTraffic)
        {
            server = new HttpProxyServer(hostName, new HttpProxy());
            if (logDebugProxyTraffic)
            {
                server.Log += OnServerLog;
            }

            server.Proxy.OnResponseSent = context =>
            {
                string[] parts = context.RequestHeader.RequestURI.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 0)
                {
                    string finalPart = parts[parts.Length - 1];
                    uris.Add(finalPart);
                }
            };

            server.Start().WaitOne();
        }

        private static void StopProxyServer()
        {
            server.Stop();
        }

        private static void OnServerLog(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.LogMessage);
        }

        private static NativeMethods.InternetPerConnectionOptionList GetSystemProxySettings()
        {
            // Query following options.
            NativeMethods.InternetPerConnectionOption[] options = new NativeMethods.InternetPerConnectionOption[3];

            options[0] = new NativeMethods.InternetPerConnectionOption();
            options[0].Option = (int)NativeMethods.InternetPerConnectionOptionValue.Flags;
            options[1] = new NativeMethods.InternetPerConnectionOption();
            options[1].Option = (int)NativeMethods.InternetPerConnectionOptionValue.ProxyServer;
            options[2] = new NativeMethods.InternetPerConnectionOption();
            options[2].Option = (int)NativeMethods.InternetPerConnectionOptionValue.ProxyBypass;

            // Allocate a block of memory of the options.
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(options[0]) + Marshal.SizeOf(options[1]) + Marshal.SizeOf(options[2]));

            IntPtr current = (IntPtr)buffer;

            // Marshal data from a managed object to an unmanaged block of memory.
            for (int i = 0; i < options.Length; i++)
            {
                Marshal.StructureToPtr(options[i], current, false);
                current = (IntPtr)((int)current + Marshal.SizeOf(options[i]));
            }

            // Initialize a INTERNET_PER_CONN_OPTION_LIST instance.
            NativeMethods.InternetPerConnectionOptionList proxySettings = new NativeMethods.InternetPerConnectionOptionList();

            // Point to the allocated memory.
            proxySettings.OptionsBufferPointer = buffer;

            proxySettings.Size = Marshal.SizeOf(proxySettings);

            // IntPtr.Zero means LAN connection.
            proxySettings.Connection = IntPtr.Zero;

            proxySettings.OptionCount = options.Length;
            proxySettings.OptionError = 0;
            int size = Marshal.SizeOf(proxySettings);

            // Query internet options.
            bool result = NativeMethods.InternetQueryOptionList(IntPtr.Zero, NativeMethods.InternetOption.PerConnectionOption, ref proxySettings, ref size);
            if (!result)
            {
                throw new ApplicationException(" Set Internet Option Failed! ");
            }

            return proxySettings;
        }

        private static NativeMethods.InternetPerConnectionOptionList CreateCustomProxySettings(string proxyString, string proxyBypassString)
        {
            // Create 3 options.
            NativeMethods.InternetPerConnectionOption[] options = new NativeMethods.InternetPerConnectionOption[3];

            // Set PROXY flags.
            options[0] = new NativeMethods.InternetPerConnectionOption();
            options[0].Option = (int)NativeMethods.InternetPerConnectionOptionValue.Flags;
            options[0].Value.integerValue = (int)NativeMethods.InternetPerConnectionProxyFlags.Proxy;

            // Set proxy name.
            options[1] = new NativeMethods.InternetPerConnectionOption();
            options[1].Option = (int)NativeMethods.InternetPerConnectionOptionValue.ProxyServer;
            options[1].Value.stringValue = Marshal.StringToHGlobalAnsi(proxyString);

            // Set proxy bypass.
            options[2] = new NativeMethods.InternetPerConnectionOption();
            options[2].Option = (int)NativeMethods.InternetPerConnectionOptionValue.ProxyBypass;
            options[2].Value.stringValue = Marshal.StringToHGlobalAnsi(proxyBypassString);

            // Allocate a block of memory of the options.
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(options[0]) + Marshal.SizeOf(options[1]) + Marshal.SizeOf(options[2]));

            IntPtr current = buffer;

            // Marshal data from a managed object to an unmanaged block of memory.
            for (int i = 0; i < options.Length; i++)
            {
                Marshal.StructureToPtr(options[i], current, false);
                current = (IntPtr)((int)current + Marshal.SizeOf(options[i]));
            }

            // Initialize a INTERNET_PER_CONN_OPTION_LIST instance.
            NativeMethods.InternetPerConnectionOptionList proxySettings = new NativeMethods.InternetPerConnectionOptionList();

            // Point to the allocated memory.
            proxySettings.OptionsBufferPointer = buffer;

            // Return the unmanaged size of an object in bytes.
            proxySettings.Size = Marshal.SizeOf(proxySettings);

            // IntPtr.Zero means LAN connection.
            proxySettings.Connection = IntPtr.Zero;

            proxySettings.OptionCount = options.Length;
            proxySettings.OptionError = 0;
            return proxySettings;
        }

        private static bool SetSystemProxySettings(NativeMethods.InternetPerConnectionOptionList proxySettings)
        {
            int size = Marshal.SizeOf(proxySettings);

            // Allocate memory.
            IntPtr proxySettingBufferPointer = Marshal.AllocCoTaskMem(size);

            // Convert structure to IntPtr
            Marshal.StructureToPtr(proxySettings, proxySettingBufferPointer, true);

            // Set internet options.
            bool bReturn = NativeMethods.InternetSetOption(IntPtr.Zero, NativeMethods.InternetOption.PerConnectionOption, proxySettingBufferPointer, size);

            // Free the allocated memory.
            Marshal.FreeCoTaskMem(proxySettings.OptionsBufferPointer);
            Marshal.FreeCoTaskMem(proxySettingBufferPointer);

            if (!bReturn)
            {
                throw new ApplicationException(" Set Internet Option Failed! ");
            }

            // Notify the system that the registry settings have been changed and cause
            // the proxy data to be reread from the registry for a handle.
            NativeMethods.InternetSetOption(IntPtr.Zero, NativeMethods.InternetOption.SettingsChanged, IntPtr.Zero, 0);
            NativeMethods.InternetSetOption(IntPtr.Zero, NativeMethods.InternetOption.Refresh, IntPtr.Zero, 0);

            return bReturn;
        }
    }
}
