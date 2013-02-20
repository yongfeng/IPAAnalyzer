using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ionic.Zip;
using IPAAnalyzer.Domain;
using Newtonsoft.Json;
using PlistCS;

namespace IPAAnalyzer.Service
{
    public class PackageService
    {
        public static PackageService Instance = new PackageService();

        // Singleton
        private PackageService()
        {
            // does nothing
        }

        public const string ITUNES_WS_URL_PREFIX = "http://ax.itunes.apple.com/WebObjects/MZStoreServices.woa/wa/wsLookup?id=";

        // Key in Info.plist
        public const string BUNDLE_DISPLAY_NAME = "CFBundleDisplayName";
        public const string BUNDLE_IDENTIFIER = "CFBundleIdentifier";
        public const string BUNDLE_VERSION = "CFBundleVersion";
        public const string BUNDLE_NAME = "CFBundleName";
        public const string BUNDLE_EXECUTABLE = "CFBundleExecutable";
        public const string BUNDLE_MININUM_OS_VERSION = "MinimumOSVersion";
        public const string BUNDLE_SHORT_VERSION_STRING = "CFBundleShortVersionString";
        public const string UI_DEVICE_FAMILY = "UIDeviceFamily";

        // Key in iTunesMetadata.plist
        public const string METADATA_ITEM_ID = "itemId";

        // Value for UI_DEVICE_FAMILY
        public const int FAMILY_IPHONE = 1;
        public const int FAMILY_IPAD = 2;

        // internal value
        public const string APP_TYPE_UNIVERSAL = "universal";
        public const string APP_TYPE_IPHONE = "iphone";
        public const string APP_TYPE_IPAD = "ipad";

        public PackageInfo GetPackageInfo(string ipaFilePath)
        {
            string payloadName;
            PackageInfo packageInfo = null;

            using (ZipFile zip = ZipFile.Read(ipaFilePath, new ReadOptions {Encoding = Encoding.UTF8})) {
                ZipEntry infoPlistZipEntry = zip.Entries.Where(e => e.FileName.EndsWith(@".app/Info.plist")).FirstOrDefault();

                // Info.plist
                if (infoPlistZipEntry != null) {
                    // zipentry filename example: "Payload/GoodReaderIPad.app/Info.plist"
                    payloadName = infoPlistZipEntry.FileName.Substring(8, infoPlistZipEntry.FileName.IndexOf(@".app/Info.plist") - 8);
                    //payloadName = infoPlistZipEntry.FileName;
                    
                    using (var ms = new MemoryStream()) {
                        infoPlistZipEntry.Extract(ms);

                        ms.Seek(0, SeekOrigin.Begin);
                        plistType plType = Plist.getPlistType(ms);

                        ms.Seek(0, SeekOrigin.Begin);
                        object results = Plist.readPlist(ms, plType);

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
                }

                // iTunesMetadata.plist
                if (packageInfo != null) {
                    ZipEntry metaPlistZipEntry = zip.Entries.Where(e => e.FileName == "iTunesMetadata.plist").FirstOrDefault();

                    if (metaPlistZipEntry != null) {
                        using (var ms = new MemoryStream()) {
                            metaPlistZipEntry.Extract(ms);

                            ms.Seek(0, SeekOrigin.Begin);
                            plistType plType = Plist.getPlistType(ms);

                            ms.Seek(0, SeekOrigin.Begin);
                            object results = Plist.readPlist(ms, plType);

                            if (results != null) {
                                Dictionary<string, object> dict = results as Dictionary<string, object>;
                                packageInfo.ItunesId = ReadPropertyAsInt(dict, METADATA_ITEM_ID);
                            }
                        }
                    }
                }
            }

            return packageInfo;
        }

        public ItunesAppInfo FetchOnlineItunesDetails(int itunesId)
        {
            if (itunesId > 0) {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(ITUNES_WS_URL_PREFIX + itunesId);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream())) {
                    string jsonData = sr.ReadToEnd();
                    ItunesSearchResult searchResults = JsonConvert.DeserializeObject<ItunesSearchResult>(jsonData);
                    if (searchResults.resultCount > 0) {
                        return searchResults.results[0];
                    }
                }
            }
            return null;
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
