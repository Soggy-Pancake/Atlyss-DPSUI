using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlyss_DPSUI {
    internal class PluginInfo {
        public const string GUID = "Soggy_Pancake.AtlyssDPSUI";
        public const string NAME = "AtlyssDPSUI";

        // NO TOUCHIE | PUT THE NEW VERSION NUMBER IN THE .csproj
        public const string VERSION = "1.0.3";

        public const int MAX_HELLO_RETRY = 5;
        public const float CLIENT_UPDATE_RATE = 1f;
        public const int FIELD_BOSS_TIMEOUT_MS = 30000;

        internal static readonly string[] FIELDS_WITH_BOSSES = new string[] { 
            "Effold Terrace", 
            "Tuul Valley" 
        };

        internal static readonly string[] FIELD_BOSSES = new string[] { 
            "Slime Diva", 
            "Gahool" 
        };
    }
}
