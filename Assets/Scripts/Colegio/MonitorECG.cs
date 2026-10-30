using UnityEngine;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// La pantalla del monitor del hospital (final del capítulo 1): la línea del pulso corre al ritmo del pitido.
    /// RecuerdoAula mueve <see cref="Intervalo"/>: el corazón se calma cuando Mateo respira con Pa y se dispara al final.
    /// La textura (Assets/Art/Hospital/ecg.png) trae 4 latidos a lo ancho.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class MonitorECG : MonoBehaviour
    {
        public static float Intervalo = 0.8f;      // segundos por latido
        const int LatidosEnTextura = 4;
        Material m;
        float corrida;

        void Start() { m = GetComponent<Renderer>().material; }

        void Update()
        {
            if (!m) return;
            corrida += Time.deltaTime / Mathf.Max(0.1f, Intervalo) / LatidosEnTextura;
            m.SetTextureOffset("_UnlitColorMap", new Vector2(corrida % 1f, 0f));
        }
    }
}
