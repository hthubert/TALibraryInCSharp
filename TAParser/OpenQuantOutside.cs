/*
   Copyright [hetao] [thanf.com]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.   
*/
/*
    尽量不要尝试直接修改本文件这样会影响 nuget 对文件的版本升级
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Xml.Serialization;
using System.Text;

namespace SmartQuant
{
    public class OpenQuantOutside
    {
        private static readonly string SmartQuantPath;

        private static string GetSmartQuantPath()
        {
            var uninstall = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            var openQuantKeyName = "{C224DA18-4901-433D-BD94-82D28B640B2C}";
            foreach (var keyName in uninstall.GetSubKeyNames()) {
                if (keyName == openQuantKeyName || keyName.StartsWith(openQuantKeyName)) {
                    openQuantKeyName = keyName;
                    break;
                }
            }
            var key = uninstall.OpenSubKey(openQuantKeyName);
            if (key != null) {
                var names = new List<string>(key.GetValueNames());
                names.Sort();
                return key.GetValue("InstallLocation").ToString();
            }
            return Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).FullName;
        }

        private static Assembly DomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var path = Path.Combine(SmartQuantPath, assemblyName.Name + ".dll");
            if (File.Exists(path)) {
                return Assembly.LoadFile(path);
            }
            Console.WriteLine(@"Not Found: " + assemblyName.Name);
            return null;
        }

        private static bool CheckFileServer(int port)
        {
            try {
                var s = new TcpListener(IPAddress.Any, port);
                s.Start();
                s.Stop();
                return false;
            }
            catch (Exception) {
                return true;
            }
        }

        private static Configuration LoadConfiguration()
        {
            var file = Path.Combine(Installation.ConfigDir.FullName, "configuration.xml");
            if (!File.Exists(file) || new FileInfo(file).Length == 0) {
                return Configuration.DefaultConfiguaration();
            }
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(file)))) {
                var xmlSerializer = new XmlSerializer(typeof(Configuration));
                return (Configuration)xmlSerializer.Deserialize(stream);
            }
        }

        private static void StartFileServer(string fileServerPath)
        {
            if (!File.Exists(fileServerPath)) {
                fileServerPath = Path.Combine(SmartQuantPath, "FileServer.exe");
            }
            try {
                Process.Start(new ProcessStartInfo {
                    UseShellExecute = false,
                    FileName = fileServerPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    Arguments = "-auto"
                });
                //Thread.Sleep(100);
                new EventWaitHandle(false, EventResetMode.AutoReset, "DataFileServerHandle").WaitOne(5000);
            }
            catch (Exception ex) {
                Console.WriteLine(string.Concat("Framework::Init Can not start ", fileServerPath, " ", ex));
            }
        }

        private static void OpenFileServer()
        {
            var configuration = LoadConfiguration();
            if (!configuration.IsDataFileLocal && (configuration.DataFileHost == "localhost" || configuration.DataFileHost == "127.0.0.1")) {
                if (!CheckFileServer(configuration.DataFilePort)) {
                    StartFileServer(configuration.FileServerPath);
                }
            }
        }

        private static void CopyLicenseFile()
        {
            var source = SmartQuantPath;
            var dest = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(source) || source == dest) {
                return;
            }

            foreach (var file in Directory.GetFiles(SmartQuantPath, "*.license", System.IO.SearchOption.TopDirectoryOnly)) {
                File.Copy(file, file.Replace(SmartQuantPath, AppDomain.CurrentDomain.BaseDirectory), true);
            }
        }

        static OpenQuantOutside()
        {
            SmartQuantPath = GetSmartQuantPath();
            AppDomain.CurrentDomain.AssemblyResolve += DomainOnAssemblyResolve;
        }

        public static void Init(bool startFileServer = true)
        {
            CopyLicenseFile();
            if (startFileServer) {
                OpenFileServer();
            }
        }

        private static void Resubscribe(SubscriptionList list, IDataProvider provider)
        {
            if (list != null) {
                var items = list.ToList();
                foreach (var item in items) {
                    if (item.Provider != null) {
                        Console.WriteLine("remove " + item.Symbol + ", " + item.Provider.Name);
                        list.Remove(item);
                    }
                    list.Add(item.Instrument, provider);
                }
            }
        }

        private static void Resubscribe(Strategy root, FieldInfo subscriptionListField, IDataProvider provider)
        {
            foreach (var child in root.Strategies) {
                var subscriptionList = (SubscriptionList)subscriptionListField.GetValue(child);
                Console.WriteLine("resubscribe " + child.Name);
                Resubscribe(subscriptionList, provider);
                if (child.Strategies.Count > 0) {
                    Resubscribe(child, subscriptionListField, provider);
                }
            }
        }

        public static void Resubscribe(Strategy root, IDataProvider provider = null)
        {
            var fields = typeof(Strategy).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo subscriptionListField = null;
            FieldInfo providerField = null;
            foreach (var field in fields) {
                if (field.FieldType.Name == "SubscriptionList") {
                    subscriptionListField = field;
                }
                if (field.FieldType.Name == "IDataProvider") {
                    providerField = field;
                }
            }

            if (subscriptionListField == null)
                return;
            if (provider == null && providerField == null) {
                return;
            }

            provider = provider ?? (IDataProvider)providerField.GetValue(root);
            Resubscribe((SubscriptionList)subscriptionListField.GetValue(root), provider);
            Resubscribe(root, subscriptionListField, provider);
        }
    }
}