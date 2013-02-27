using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ionic.Zip;
using IPAAnalyzer.Domain;
using Newtonsoft.Json;
using PlistCS;
using System;
using IPAAnalyzer.Util;
using System.Drawing;

namespace IPAAnalyzer.Service
{
    public class PackageService
    {
        public const string CACHE_DIR = @".\cache";
        public const int THUMBNAIL_SIZE = 32;

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
        public const string BUNDLE_ICON_FILES = "CFBundleIconFiles";
        public const string BUNDLE_ICONS = "CFBundleIcons";
        public const string BUNDLE_PRIMARY_ICON = "CFBundlePrimaryIcon";


        // Key in iTunesMetadata.plist
        public const string METADATA_ITEM_ID = "itemId";

        // Value for UI_DEVICE_FAMILY
        public const int FAMILY_IPHONE = 1;
        public const int FAMILY_IPAD = 2;

        // internal value
        public const string APP_TYPE_UNIVERSAL = "universal";
        public const string APP_TYPE_IPHONE = "iphone";
        public const string APP_TYPE_IPAD = "ipad";
        public const string APP_TYPE_UNKNOWN = "unknown";

        public PackageInfo GetPackageInfo(string ipaFilePath)
        {
            string payloadName;
            PackageInfo packageInfo = null;

            try {
                using (ZipFile zip = ZipFile.Read(ipaFilePath, new ReadOptions { Encoding = Encoding.UTF8 })) {
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

                                string identifier = ReadPropertyAsString(dict, BUNDLE_IDENTIFIER);

                                // icon file
                                CacheIcon(dict, zip, identifier);

                                packageInfo = new PackageInfo
                                {
                                    OriginalFile = ipaFilePath,
                                    DisplayName = ReadPropertyAsString(dict, BUNDLE_DISPLAY_NAME),
                                    PayloadName = payloadName,
                                    Identifier = identifier,
                                    ShortVersion = ReadPropertyAsString(dict, BUNDLE_SHORT_VERSION_STRING),
                                    Version = ReadPropertyAsString(dict, BUNDLE_VERSION),
                                    Name = ReadPropertyAsString(dict, BUNDLE_NAME),
                                    Excutbale = ReadPropertyAsString(dict, BUNDLE_EXECUTABLE),
                                    MinimumOsVersion = ReadPropertyAsString(dict, BUNDLE_MININUM_OS_VERSION),
                                    AppType = ReadDeviceFamily(dict),
                                    IsProcessed = true
                                };
                            }
                        }
                    }

                    if (packageInfo != null) {
                        // iTunesMetadata.plist
                        ZipEntry metaPlistZipEntry = zip.Entries.Where(e => e.FileName == "iTunesMetadata.plist").FirstOrDefault();
                        if (metaPlistZipEntry != null) {
                            try {
                                // initialize its id to -1, even if the later processing fails to find the id,
                                // this will indicate iTunesMetadata.plist is found and processed
                                packageInfo.ItunesId = -1;
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
                            catch (Exception e) {
                                // TODO
                            }
                        }


                        // iTunesArtwork
                        ZipEntry itunesArtworkZipEntry = zip.Entries.Where(e => e.FileName == "iTunesArtwork").FirstOrDefault();
                        if (itunesArtworkZipEntry != null) {
                            try {
                                string localArtworkFile = string.Format(@"{0}\{1}_artwork.png", CACHE_DIR, packageInfo.Identifier);
                                string localArtworkFileSmall = string.Format(@"{0}\{1}_artwork_small.png", CACHE_DIR, packageInfo.Identifier);
                                if (!File.Exists(localArtworkFile)) {
                                    using (var fs = new FileStream(localArtworkFile, FileMode.Create)) {
                                        itunesArtworkZipEntry.Extract(fs);
                                    }

                                    using (var source = Image.FromFile(localArtworkFile)) {
                                        var bmp = new Bitmap(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
                                        using (var g = Graphics.FromImage(bmp)) {
                                            g.DrawImage(source, new Rectangle(0, 0, THUMBNAIL_SIZE, THUMBNAIL_SIZE));
                                            g.Save();
                                        }
                                        bmp.Save(localArtworkFileSmall);
                                    }

                                    //PNGConvertor.Convert(localIconFile);
                                }
                            }
                            catch (Exception e) {
                                // TODO
                            }
                        }

                    }
                }
            }
            catch (Exception e) {
                if (packageInfo == null) {
                    packageInfo = new PackageInfo
                    {
                        OriginalFile = ipaFilePath,
                        IsProcessed = false,
                        ProcessingRemarks = e.Message
                    };
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

        private void CacheIcon(Dictionary<string, object> dict, ZipFile zip, string identifier)
        {
            if (dict.ContainsKey(BUNDLE_ICON_FILES)) {
                List<object> iconFiles = (List<object>)dict[BUNDLE_ICON_FILES];

                if (iconFiles != null && iconFiles.Count > 0) {
                    string bundleIconFile = (string)iconFiles[0];

                    if (!string.IsNullOrEmpty(bundleIconFile)) {
                        ZipEntry iconFileZipEntry = zip.Entries
                            .Where(e => e.FileName.EndsWith(@".app/" + bundleIconFile))
                            .FirstOrDefault();

                        if (iconFileZipEntry != null) {
                            string localIconFile = string.Format(@"{0}\{1}.png", CACHE_DIR, identifier);

                            if (!File.Exists(localIconFile)) {
                                using (var fs = new FileStream(localIconFile, FileMode.Create)) {
                                    iconFileZipEntry.Extract(fs);
                                }
                                //PNGConvertor.Convert(localIconFile);
                            }
                        }
                    }
                }
            }
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

                return isSupportIphone ? (isSupportIpad ? APP_TYPE_UNIVERSAL : APP_TYPE_IPHONE) : (isSupportIpad ? APP_TYPE_IPAD : APP_TYPE_UNKNOWN);
            }
            else {
                string minVer = ReadPropertyAsString(dict, BUNDLE_MININUM_OS_VERSION);
                if (!string.IsNullOrEmpty(minVer)) {
                    string[] versions = minVer.Split('.');
                    if (versions.Length >= 2) {
                        int majorVer;
                        int minorVer;
                        if (int.TryParse(versions[0], out majorVer) && int.TryParse(versions[1], out minorVer)) {
                            if (majorVer < 3 || (majorVer == 3 && minorVer < 2)) {
                                return APP_TYPE_IPHONE;
                            }
                        }
                    }
                }
            }

            return APP_TYPE_UNKNOWN;
        }
    }
}
