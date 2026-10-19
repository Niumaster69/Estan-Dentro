using System.Collections.Generic;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Recuerdos escondidos en casilleros (uno por casillero, 10 en todo el colegio). Van a PISTAS en el cuaderno.
    /// Cuentan la historia en pedacitos sin decirla toda (narrativa unificada: coma + Pa vivo). El último anticipa el final.
    /// </summary>
    public static class Recuerdos
    {
        static readonly Dictionary<string, (string titulo, string texto)> lista = new Dictionary<string, (string, string)>
        {
            ["recuerdo_1"]  = ("Foto del 4°B", "Todo el curso en la cancha, en uniforme de educación física.\n\nAlguien encerró mi cara con lapicero rojo."),
            ["recuerdo_2"]  = ("Lista de asistencia", "17 de mayo.\n\nMendoza, Mateo: (en blanco)\n\nAl lado, en lápiz, alguien escribió: «accidente»."),
            ["recuerdo_3"]  = ("Boleto de bus", "Ruta 46 · Estudiante\n17/05/2023\n\nSin usar."),
            ["recuerdo_4"]  = ("Tarjeta de cumpleaños", "¡Feliz cumple 16, Mateo!\nFirmamos todos.\n\n(Hay cuatro firmas tachadas. No reconozco ninguna.)"),
            ["recuerdo_5"]  = ("Un dibujo viejo", "Lápices de colores. Un carro rojo pequeño y un camión enorme.\n\nAbajo, con letra de niño: «papá y yo»."),
            ["recuerdo_6"]  = ("Nota de mamá", "Mi amor: te dejé arepas en la lonchera.\nPórtate bien y hazle caso a tu papá.\n\n— Mamá\n\n(Es de hace años. Todavía la guardaba.)"),
            ["recuerdo_7"]  = ("Carné estudiantil", "Mateo Mendoza · 4°B\n\nLa foto está borrosa, como si me hubiera movido. Yo nunca me muevo en las fotos."),
            ["recuerdo_8"]  = ("Cuaderno de un compañero", "Hoy no vino Mateo. Dicen que tuvo un accidente con el papá.\n\nLa profe lloró en clase. Nadie dijo nada."),
            ["recuerdo_9"]  = ("Papelito doblado", "¿Vas al cumple de Mateo el sábado?\n\n☐ sí     ☐ no\n\n(Nadie marcó nada.)"),
            ["recuerdo_10"] = ("Recorte de periódico", "Choque en la Autopista Norte deja a un menor herido de gravedad.\n\nEl conductor, su padre, salió ileso y permanece en el hospital junto a él."),
        };

        public static bool Es(string id) => !string.IsNullOrEmpty(id) && id.StartsWith("recuerdo_");
        public static (string titulo, string texto) De(string id) => lista.TryGetValue(id, out var r) ? r : ("Un papel", "No se entiende la letra.");
    }
}
