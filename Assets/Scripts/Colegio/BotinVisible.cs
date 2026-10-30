using UnityEngine;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Lo que hay adentro de un casillero, lonchera o cajón SE VE (Henry: "yo no recogí ningún destornillador").
    /// Pone el modelo del objeto (Resources/Objetos/&lt;id&gt;) apoyado en el fondo del contenedor; el jugador lo coge con E.
    /// </summary>
    public static class BotinVisible
    {
        public static GameObject Poner(string id, GameObject contenedor)
        {
            if (string.IsNullOrEmpty(id) || !contenedor) return null;
            var molde = Resources.Load<GameObject>("Objetos/" + (Recuerdos.Es(id) ? "papel" : Inventory.DibujosObjetos.Modelo(id)));
            if (!molde) return null;
            // el fondo del contenedor: rayo hacia abajo desde su centro contra sus propias mallas
            var rs = contenedor.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return null;
            Bounds b = rs[0].bounds; foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
            Vector3 desde = b.center;
            float suelo = b.min.y + 0.04f;
            foreach (var h in Physics.RaycastAll(desde, Vector3.down, b.extents.y + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.transform.IsChildOf(contenedor.transform) && h.normal.y > 0.7f && h.point.y < desde.y) { suelo = Mathf.Max(suelo, h.point.y); }
            var go = Object.Instantiate(molde);
            go.name = "Botin_" + id;
            go.transform.SetParent(contenedor.transform, true);
            go.transform.position = new Vector3(desde.x, suelo + 0.003f, desde.z);
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(contenedor.transform.right, Vector3.up).normalized, Vector3.up)
                                    * Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);     // lo coge el contenedor (E), no estorba al rayo
            return go;
        }
    }
}
