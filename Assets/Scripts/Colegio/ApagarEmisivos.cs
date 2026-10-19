using System.Collections;
using UnityEngine;
using EstanDentro.Interaction;

namespace EstanDentro.Colegio
{
    /// <summary>En el apagón, los tubos y bombillos también dejan de brillar (antes se apagaba la luz pero el tubo seguía encendido).</summary>
    public class ApagarEmisivos : MonoBehaviour
    {
        static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");

        IEnumerator Start()
        {
            while (BlackoutEvent.Instance == null) yield return null;
            BlackoutEvent.Instance.OnBlackout += Apagar;
        }

        void OnDestroy()
        {
            if (BlackoutEvent.Instance != null) BlackoutEvent.Instance.OnBlackout -= Apagar;
        }

        void Apagar()
        {
            int n = 0;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty(EmissiveColor)) continue;
                    var c = m.GetColor(EmissiveColor);
                    if (c.maxColorComponent <= 0.001f) continue;
                    var copia = new Material(m);
                    copia.SetColor(EmissiveColor, Color.black);
                    mats[i] = copia; cambio = true;
                }
                if (cambio) { r.sharedMaterials = mats; n++; }
            }
            Debug.Log($"[Apagon] objetos emisivos apagados: {n}");
        }
    }
}
