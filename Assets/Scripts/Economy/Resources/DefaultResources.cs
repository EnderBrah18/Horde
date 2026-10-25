using System;
using UnityEngine;

// Somente os dois recursos usados pelo início da partida e expansão da vila.
// Outros recursos são referenciados diretamente nos assets de construção/receita.
public static class DefaultResources
{
    static ResourceData wood;
    static ResourceData crystals;
    public static ResourceData Wood => Load(ref wood, "ResourceDefinitions/Materials/Wood");
    public static ResourceData Crystals => Load(ref crystals, "ResourceDefinitions/Magical/ManaCrystal");

    static ResourceData Load(ref ResourceData cached, string path)
    {
        if (cached == null) cached = Resources.Load<ResourceData>(path);
        if (cached == null) throw new InvalidOperationException("Recurso padrão ausente em Resources/" + path);
        return cached;
    }
}
