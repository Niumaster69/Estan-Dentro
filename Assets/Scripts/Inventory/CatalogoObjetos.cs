using System.Collections.Generic;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Qué es cada objeto y para qué sirve, como lo piensa Mateo. Lo usa la tarjeta que aparece al recogerlo
    /// y el inventario. Si un objeto no está aquí, la tarjeta dice algo genérico.
    /// </summary>
    public static class CatalogoObjetos
    {
        public struct Ficha
        {
            public string queEs;     // descripción corta
            public string paraQue;   // pista de uso (lo que el jugador necesita saber)
            public string tecla;     // control asociado, si lo hay
        }

        static readonly Dictionary<string, Ficha> fichas = new Dictionary<string, Ficha>
        {
            ["destornillador"] = new Ficha { queEs = "Un destornillador de pala, con el mango rojo gastado.",
                                             paraQue = "Sirve para quitar tornillos… como los de una rejilla." },
            ["linterna"]       = new Ficha { queEs = "La linterna vieja de Pa. La luz tiembla un poco.",
                                             paraQue = "Con ella se ven cosas que a oscuras no estaban.", tecla = "[F] encender / apagar" },
            ["baterias"]       = new Ficha { queEs = "Pilas AA. Unas de marca, otras no tanto.",
                                             paraQue = "Cuando la linterna se apague, las cambio solo.", tecla = "(automático)" },
            ["tiza"]           = new Ficha { queEs = "Un pedazo de tiza blanca del tablero.",
                                             paraQue = "Marco las paredes para saber por dónde ya pasé.", tecla = "[T] marcar la pared" },
            ["pata_de_cabra"]  = new Ficha { queEs = "Una pata de cabra pesada, con la pintura roja pelada.",
                                             paraQue = "Abre candados sin la clave… pero se oye en todo el piso. Ocupa las dos manos.",
                                             tecla = "[R] forzar, mirando un candado" },
            ["llaves"]         = new Ficha { queEs = "Un manojo de llaves con etiquetas borrosas.", paraQue = "Alguna abre una puerta de este piso." },
            ["moneda"]         = new Ficha { queEs = "Una moneda de 500.", paraQue = "Las máquinas de la cafetería todavía funcionan." },
            ["gaseosa"]        = new Ficha { queEs = "Una gaseosa tibia.", paraQue = "Tomarla calma un poco los nervios." },
            ["radio"]          = new Ficha { queEs = "Un radio de pilas que solo da estática.", paraQue = "A veces, entre la estática, alguien habla." },
            ["brick_game"]     = new Ficha { queEs = "Un Brick Game. Todavía prende.", paraQue = "No sirve para salir. Pero distrae." },
            ["galon"]          = new Ficha { queEs = "Un galón con algo de líquido.", paraQue = "Pesa. Ocupa las dos manos." },
            ["mochila"]        = new Ficha { queEs = "Mi mochila. Todavía tiene el llavero que me dio Pa.",
                                             paraQue = "Con ella puedo cargar más cosas (6 en vez de 2)." },
        };

        public static Ficha De(string id)
        {
            if (!string.IsNullOrEmpty(id) && fichas.TryGetValue(id, out var f)) return f;
            return new Ficha { queEs = "No sé bien qué es.", paraQue = "Lo guardo. Puede servir." };
        }
    }
}
