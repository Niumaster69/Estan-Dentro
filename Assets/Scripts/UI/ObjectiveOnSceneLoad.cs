using System.Collections;
using UnityEngine;

namespace EstanDentro.UI
{
    /// <summary>
    /// Muestra un mensaje en ObjectiveHUD apenas carga la escena (con delay opcional).
    /// Util para dar al jugador un objetivo claro al entrar en una zona nueva.
    /// </summary>
    public class ObjectiveOnSceneLoad : MonoBehaviour
    {
        [Header("Mensaje a mostrar")]
        [SerializeField, TextArea(1, 4)]
        private string objectiveMessage = "Encuentra la salida.";
        [SerializeField, Tooltip("Segundos que el mensaje permanece visible.")]
        private float displaySeconds = 5f;
        [SerializeField, Tooltip("Delay antes de mostrar (segundos). Da tiempo a que la escena termine de cargar visualmente.")]
        private float delayBeforeShow = 1.5f;
        [SerializeField, Tooltip("Si true, solo se muestra una vez por sesion.")]
        private bool onceOnly = true;

        private static bool shownThisSession;

        private void Start()
        {
            if (onceOnly && shownThisSession) return;
            StartCoroutine(ShowAfterDelay());
        }

        private IEnumerator ShowAfterDelay()
        {
            if (delayBeforeShow > 0f) yield return new WaitForSeconds(delayBeforeShow);
            ObjectiveHUD.Show(objectiveMessage, displaySeconds);
            shownThisSession = true;
        }
    }
}
