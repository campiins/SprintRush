using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    // Para la fuente NoizeSport y tamaño 36, funciona el espaciado de 28px.
    // Esta funcion aplica un monoespaciado de 28px a la fuente para evitar efecto de vibracion de la fuente al actualizarse.
    public static string Monospace<T>(this T original, bool enabled = true) => enabled ? $"<mspace=28px>{original}</mspace>" : original.ToString();
}
