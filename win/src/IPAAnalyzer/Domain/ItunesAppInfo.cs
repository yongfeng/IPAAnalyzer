using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPAAnalyzer.Domain
{
    public class ItunesAppInfo
    {
        public string kind { get; set; }
        public string[] features { get; set; }
        public string[] supportedDevices { get; set; }
        public bool isGameCenterEnabled { get; set; }
        public string artistViewUrl { get; set; }
        public string artworkUrl60 { get; set; }
        public string[] screenshotUrls { get; set; }
        public string[] ipadScreenshotUrls { get; set; }
        public string artworkUrl512 { get; set; }
        public string artistId { get; set; }
        public string artistName { get; set; }
        public double price { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string currency { get; set; }
        public string[] genres { get; set; }
        public string[] genreIds { get; set; }
        public string releaseDate { get; set; }
        public string sellerName { get; set; }
        public string bundleId { get; set; }
        public string trackId { get; set; }
        public string trackName { get; set; }
        public string primaryGenreName { get; set; }
        public string primaryGenreId { get; set; }
        public string releaseNotes { get; set; }
        public string formattedPrice { get; set; }
        public string wrapperType { get; set; }
        public string trackCensoredName { get; set; }
        public string trackViewUrl { get; set; }
        public string contentAdvisoryRating { get; set; }
        public string artworkUrl100 { get; set; }
        public string[] languageCodesISO2A { get; set; }
        public string fileSizeBytes { get; set; }
        public string sellerUrl { get; set; }
        public string averageUserRatingForCurrentVersion { get; set; }
        public string userRatingCountForCurrentVersion { get; set; }
        public string trackContentRating { get; set; }
        public string averageUserRating { get; set; }
        public string userRatingCount { get; set; }

        public string FormattedGenres
        {
            get
            {
                return string.Join(", ", genres);
            }
            set {

            }
        }
    }

    public class ItunesSearchResult
    {
        public int resultCount { get; set; }
        public ItunesAppInfo[] results { get; set; } 
    }
}
