using System.Text;
using System.Text.RegularExpressions;

namespace IPAAnalyzer.Domain
{
    public class PackageInfo
    {
        private static string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars());
        private Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));

        public virtual string OriginalFile { get; set; }
        public virtual string PayloadName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string Identifier { get; set; }
        public virtual string Version { get; set; }
        public virtual string Name { get; set; }
        public virtual string Excutbale { get; set; }
        public virtual string MinimumOsVersion { get; set; }
        public virtual string ShortVersion { get; set; }
        public virtual string AppType { get; set; }
        public virtual int ItunesId { get; set; }
        public virtual bool IsProcessed { get; set; }
        public virtual string ProcessingRemarks { get; set; }

        public virtual bool Same
        {
            get
            {
                if (System.IO.Path.GetFileName(OriginalFile) == RecommendedFileName) {
                    return true;
                }
                else {
                    return false;
                }
            }
        }


        private string _originalBaseFilename = null;
        public string OriginalBaseFilename
        {
            get
            {
                if (_originalBaseFilename == null) {
                    if (!string.IsNullOrEmpty(OriginalFile)) {
                        _originalBaseFilename = OriginalFile.Substring(OriginalFile.LastIndexOf('\\') + 1);
                    }
                }

                return _originalBaseFilename;
            }
        }

        private string _recommendedFilename = null;
        public string RecommendedFileName
        {
            get
            {
                if (_recommendedFilename == null) {
                    if (!IsProcessed) {
                        _recommendedFilename = string.Empty;
                    }
                    else {
                        StringBuilder sb = new StringBuilder();

                        // name
                        string mainName = DisplayName;
                        string codeName = Excutbale ?? PayloadName;

                        if (string.IsNullOrEmpty(mainName)) {
                            mainName = codeName;
                        }
                        sb.Append(mainName);

                        if (mainName != codeName) {
                            sb.Append("_(").Append(codeName).Append(")");
                        }

                        // version
                        string mainVersion = ShortVersion;
                        if (string.IsNullOrEmpty(mainVersion)) {
                            mainVersion = Version;
                        }

                        sb.Append("_v").Append(mainVersion);
                        if (mainVersion != Version) {
                            sb.Append("_(").Append(Version).Append(")");
                        }

                        // os version
                        if (!string.IsNullOrEmpty(MinimumOsVersion)) {
                            sb.Append("_os").Append(MinimumOsVersion);
                        }

                        // app type
                        if (!string.IsNullOrEmpty(AppType)) {
                            sb.Append("_").Append(AppType);
                        }

                        // identifier
                        if (!string.IsNullOrEmpty(Identifier)) {
                            sb.Append("_").Append(Identifier);
                        }

                        sb.Append(".ipa");

                        _recommendedFilename = sb.ToString();
                        _recommendedFilename = r.Replace(_recommendedFilename, "_");
                    }
                }
                return _recommendedFilename;
            }
        }

        public string GetFormattedName()
        {
            string mainName = DisplayName;
            string codeName = Excutbale ?? PayloadName;

            if (string.IsNullOrEmpty(mainName)) {
                mainName = codeName;
            }

            if (mainName != codeName) {
                mainName += "_(" + codeName + ")";
            }

            return mainName;
        }

        public override string ToString()
        {
            return string.Format(
@"DisplayName:      {0};
PayloadName:      {1};
Identifier:       {2};
ShortVersion:     {3};
Version:          {4};
Name:             {5};
Excutbale:        {6};
MinimumOsVersion: {7};
AppType:          {8};
ItunesId:         {9}",
                DisplayName, PayloadName, Identifier, ShortVersion,
                Version, Name, Excutbale, MinimumOsVersion, AppType, ItunesId);
        }
    }
}
