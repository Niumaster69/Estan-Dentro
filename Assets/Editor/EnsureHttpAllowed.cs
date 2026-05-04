using UnityEditor;
using UnityEngine;

namespace EstanDentro.EditorTools
{
    // Garantiza que PlayerSettings permita HTTP (necesario porque la API esta en http://estandentro.somee.com sin SSL).
    // Se ejecuta automaticamente al abrir Unity o al recompilar scripts. Idempotente.
    [InitializeOnLoad]
    public static class EnsureHttpAllowed
    {
        static EnsureHttpAllowed()
        {
            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                Debug.Log("[EnsureHttpAllowed] PlayerSettings.insecureHttpOption -> AlwaysAllowed (necesario para http://estandentro.somee.com).");
            }
        }
    }
}
