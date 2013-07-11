using System.Globalization;
using System.Linq;
using System.Web;
using ICFieldView.Core.Code;
using System.Threading;

namespace ICFieldView.Core.Globalization {
    public class CultureSelector {

        private readonly ICFVConfiguration _config;
        private readonly ICultureProvider _cultureProvider;
        private readonly static CultureInfo FallbackCulture = new CultureInfo("en-us");

        //Sets the culture for a given web request. This selection process opts for picking the user's most preferred
        //language as specified in their browser, but has multiple fallbacks in cases where an appropriate translation
        //could not be found.
        public CultureSelector(ICFVConfiguration config, ICultureProvider cultureProvider) {
            _config = config;
            _cultureProvider = cultureProvider;
        }

        public void SetCultureForRequest(HttpRequestBase request) {
            if(_config.DisableInternationalization) {
                return;
            }

            var browserCultureMatch = TryGetBrowserCulture(request);
            CultureInfo currentCulture;
            CultureInfo currentUICulture;

            if(browserCultureMatch != null) {
                currentCulture = _config.EnableMixedCultures ? browserCultureMatch.ExactCulture : browserCultureMatch.ClosestLocalizedCulture;
                currentUICulture = browserCultureMatch.ClosestLocalizedCulture;
            }
            else {
                var culture = TryGetConfigCulture() ?? FallbackCulture;
                currentCulture =  currentUICulture = culture;
            }
            
            Thread.CurrentThread.CurrentCulture = currentCulture;
            Thread.CurrentThread.CurrentUICulture = currentUICulture;
        }

        private BrowserCultureMatchInfo TryGetBrowserCulture(HttpRequestBase request)
        {

            foreach (var languageToken in request.UserLanguages ?? new string[]{}) {
                try {
                    //strip off the quality factor (http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html)
                    var cultureToken = languageToken.Split(';').First();
                    var culture = new CultureInfo(cultureToken);
                    
                    var browserCulture = GetClosestMatchingLanguage(culture);
                    if(browserCulture != null) {
                        return new BrowserCultureMatchInfo{ExactCulture = culture, ClosestLocalizedCulture = browserCulture};
                    }
                } catch (CultureNotFoundException) {
                    //ignore invalid culture supplied by the browser
                }
            }
            return null;
        }

        private CultureInfo TryGetConfigCulture() {
            try {
                if(_config.DefaultCulture == null) {
                    return null;
                }
                var defaultCulture = new CultureInfo(_config.DefaultCulture);
                return GetClosestMatchingLanguage(defaultCulture);

            } catch (CultureNotFoundException) {
                //ignore invalid culture set in config
            }
            return null;
        }

        private CultureInfo GetClosestMatchingLanguage(CultureInfo culture) {
            if(culture == null || culture.Equals(CultureInfo.InvariantCulture)) {
                return null;
            }

            var cultures = _cultureProvider.GetSupportedCultures.ToList();

            var specificCulture = cultures.FirstOrDefault(c => c.IsSameCultureAs(culture));
            var neutralCulture = cultures.FirstOrDefault(c => c.IsSameLanguageAs(culture));
            var fallBackCulture = culture.IsSameLanguageAs(FallbackCulture) ? FallbackCulture : null;

            return specificCulture ?? neutralCulture ?? fallBackCulture;
        }

        private class BrowserCultureMatchInfo {
            public CultureInfo ExactCulture { get; set; }
            public CultureInfo ClosestLocalizedCulture { get; set; }
        }
        
    }
}
