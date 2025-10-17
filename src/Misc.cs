using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlyss_DPSUI;

public class Boolable {
    public static implicit operator bool(Boolable obj) {
        return obj != null;
    }
}

public static class UnityNullFix {
    public static T? NC<T>(this T obj) where T : UnityEngine.Object => obj ? obj : null;
}
