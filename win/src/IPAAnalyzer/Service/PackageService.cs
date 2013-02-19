using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using System.IO;
using PlistCS;
using System.Text.RegularExpressions;
using IPAAnalyzer.Domain;

namespace IPAAnalyzer.Service
{
    public class PackageService
    {
        public const string BUNDLE_DISPLAY_NAME = "CFBundleDisplayName";
        public const string BUNDLE_IDENTIFIER = "CFBundleIdentifier";
        public const string BUNDLE_VERSION = "CFBundleVersion";
        public const string BUNDLE_NAME = "CFBundleName";
        public const string BUNDLE_EXECUTABLE = "CFBundleExecutable";
        public const string BUNDLE_MININUM_OS_VERSION = "MinimumOSVersion";
        public const string BUNDLE_SHORT_VERSION_STRING = "CFBundleShortVersionString";
        public const string UI_DEVICE_FAMILY = "UIDeviceFamily";

        public const string METADATA_ITEM_ID = "itemId";

        public const int FAMILY_IPHONE = 1;
        public const int FAMILY_IPAD = 2;

        public const string APP_TYPE_UNIVERSAL = "universal";
        public const string APP_TYPE_IPHONE = "iphone";
        public const string APP_TYPE_IPAD = "ipad";

        public PackageInfo GetPackageInfo(string ipaFilePath)
        {
            string payloadName;
            PackageInfo packageInfo = null;

            using (ZipFile zip = ZipFile.Read(ipaFilePath)) {
                ZipEntry infoPlistZipEntry = zip.Entries.Where(e => e.FileName.EndsWith(@".app/Info.plist")).FirstOrDefault();
                ZipEntry metaPlistZipEntry = zip.Entries.Where(e => e.FileName == "iTunesMetadata.plist").FirstOrDefault();

                // Info.plist
                if (infoPlistZipEntry != null) {
                    // zipentry filename example: "Payload/GoodReaderIPad.app/Info.plist"
                    payloadName = infoPlistZipEntry.FileName.Substring(8, infoPlistZipEntry.FileName.IndexOf(@".app/Info.plist") - 8);
                    //using (var ms = new MemoryStream()) {
                    using (var ms = new FileStream("tmp.plist", FileMode.Create, FileAccess.ReadWrite)) {
                        infoPlistZipEntry.Extract(ms);
                    }

                    object results = Plist.readPlist("tmp.plist");

                    if (results != null) {
                        Dictionary<string, object> dict = results as Dictionary<string, object>;

                        packageInfo = new PackageInfo
                        {
                            OriginalFile = ipaFilePath,
                            DisplayName = ReadPropertyAsString(dict, BUNDLE_DISPLAY_NAME),
                            PayloadName = payloadName,
                            Identifier = ReadPropertyAsString(dict, BUNDLE_IDENTIFIER),
                            ShortVersion = ReadPropertyAsString(dict, BUNDLE_SHORT_VERSION_STRING),
                            Version = ReadPropertyAsString(dict, BUNDLE_VERSION),
                            Name = ReadPropertyAsString(dict, BUNDLE_NAME),
                            Excutbale = ReadPropertyAsString(dict, BUNDLE_EXECUTABLE),
                            MinimumOsVersion = ReadPropertyAsString(dict, BUNDLE_MININUM_OS_VERSION),
                            AppType = ReadDeviceFamily(dict)
                        };
                    }
                }

                // iTunesMetadata.plist
                if (packageInfo != null) {
                    if (metaPlistZipEntry != null) {
                        using (var ms = new FileStream("tmp.plist", FileMode.Create, FileAccess.ReadWrite)) {
                            metaPlistZipEntry.Extract(ms);
                        }
                        object results = Plist.readPlist("tmp.plist");
                        if (results != null) {
                            Dictionary<string, object> dict = results as Dictionary<string, object>;
                            packageInfo.ItunesId = ReadPropertyAsInt(dict, METADATA_ITEM_ID);
                        }
                    }
                }
            }

            return packageInfo;
        }

        private string ReadPropertyAsString(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key)) {
                object obj = dict[key];
                if (obj != null) {
                    if (obj.GetType() == typeof(string)) {
                        return obj as string;
                    }
                }
            }

            return null;
        }

        private int ReadPropertyAsInt(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key)) {
                object obj = dict[key];
                if (obj != null) {
                    if (obj.GetType() == typeof(int)) {
                        return (int)obj;
                    }
                }
            }

            return -1;
        }

        private string ReadDeviceFamily(Dictionary<string, object> dict)
        {
            if (dict.ContainsKey(UI_DEVICE_FAMILY)) {
                List<object> familyList = (List<object>)dict[UI_DEVICE_FAMILY];
                bool isSupportIphone = false;
                bool isSupportIpad = false;
                int flag;
                foreach (object supportedDevice in familyList) {
                    if (int.TryParse(supportedDevice.ToString(), out flag)) {
                        if (flag == FAMILY_IPHONE) {
                            isSupportIphone = true;
                        }
                        else if (flag == FAMILY_IPAD) {
                            isSupportIpad = true;
                        }
                    }
                }

                return isSupportIphone ? (isSupportIpad ? APP_TYPE_UNIVERSAL : APP_TYPE_IPHONE) : (isSupportIpad ? APP_TYPE_IPAD : string.Empty);
            }
            return null;
        }
    }
}
