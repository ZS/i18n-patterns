using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using ICFieldView.Localized;

namespace ICFieldView.Core.Globalization {

    public interface ICultureProvider {
        IEnumerable<CultureInfo> GetSupportedCultures { get; }
    }

    public class CultureProvider : ICultureProvider
    {
        private readonly Lazy<IEnumerable<CultureInfo>> _cultures;
        
        //Provides the set of specific cultures that have resource file translations.  This determiniation is moderately expensive
        //to perform (~100ms) and should always be cached by injecting the class as a singleton in any web endpoints.
        public CultureProvider() {
            var cultureLocator = new Func<IEnumerable<CultureInfo>>(() => GetCulturesFromResourceFile(typeof (ClientFacingText)));
            _cultures = new Lazy<IEnumerable<CultureInfo>>(cultureLocator);
        }    

        public IEnumerable<CultureInfo> GetSupportedCultures {
            get { return _cultures.Value; }
        }

        private static IEnumerable<CultureInfo> GetCulturesFromResourceFile(Type resourceFile) {
            var rm = new ResourceManager(resourceFile);
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => {
                    if (c.IsSameCultureAs(CultureInfo.InvariantCulture)) {
                        return false;
                    }
                    return rm.GetResourceSet(c, createIfNotExists: true, tryParents: false) != null;
            }).ToList();
            return cultures;
        }
    }
    
    public static class CultureInfoExtensions
    {
        public static bool IsSameLanguageAs(this CultureInfo c1, CultureInfo c2) {
            return c1.TwoLetterISOLanguageName.Equals(c2.TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsSameCultureAs(this CultureInfo c1, CultureInfo c2) {
            return c1.LCID.Equals(c2.LCID);
        } 
    }
}
