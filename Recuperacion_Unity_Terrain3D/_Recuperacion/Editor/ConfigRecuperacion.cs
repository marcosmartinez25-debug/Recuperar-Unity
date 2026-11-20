using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Datos y cuentas comunes de la escena: medidas del terreno, por donde pasa el
/// rio, por donde pasa el camino, etc. Lo usan todos los generadores.
/// </summary>
public static class ConfigRecuperacion
{
    // ---------------------------------------------------------------- Terreno
    public const float TamX = 500f;        // ancho del terreno en metros
    public const float TamZ = 500f;        // largo del terreno en metros
    public const float AlturaMax = 160f;   // altura maxima del terreno

    public const int ResAltura = 513;      // resolucion del mapa de alturas
    public const int ResAlpha = 512;       // resolucion del mapa de texturas
    public const int ResDetalle = 512;     // resolucion del mapa de hierba
    public const int ParcheDetalle = 32;

    // ------------------------------------------------------------------- Rio
    public const float AnchoRioMitad = 26f;   // medio ancho del cauce
    public const float AlturaAgua = 15.0f;    // altura de la lamina de agua
    public const float FondoRio = 0.070f;     // altura normalizada del fondo
    public const float BasePradera = 0.140f; // altura normalizada de la pradera

    // ---------------------------------------------------------------- Camino
    public const float CruceX = 240f;         // x donde el camino cruza el rio
    public const float AnchoCamino = 9f;      // mitad del ancho util del camino

    /// <summary>
    /// Z por la que pasa el centro del rio para una X dada.
    /// Con dos senos conseguimos que el cauce serpentee y no sea una linea recta.
    /// </summary>
    public static float ZRio(float x)
    {
        return 238f + 22f * Mathf.Sin(x * 0.0125f) + 9f * Mathf.Sin(x * 0.031f + 1.3f);
    }

    /// <summary>
    /// Puntos por los que pasa el camino, de la pradera a la montana.
    /// En el cruce del rio van dos puntos alineados en X para que el puente
    /// quede recto y el personaje no tenga que girar encima.
    /// </summary>
    public static Vector2[] PuntosCamino()
    {
        float zr = ZRio(CruceX);
        return new Vector2[]
        {
            new Vector2( 70f,  65f),              // inicio en la pradera
            new Vector2(118f, 118f),
            new Vector2(168f, 162f),
            new Vector2(215f, 196f),
            new Vector2(CruceX, zr - 42f),        // entrada del puente
            new Vector2(CruceX, zr + 42f),        // salida del puente
            new Vector2(272f, 298f),
            new Vector2(302f, 342f),
            new Vector2(322f, 392f),
            new Vector2(334f, 428f)               // final: cofre en la montana
        };
    }

    /// <summary>
    /// Convierte la polilinea del camino en muchos puntos seguidos,
    /// separados por "paso" metros. Sirve para nivelar el camino y pintarlo.
    /// </summary>
    public static List<Vector2> MuestrasCamino(float paso = 2f)
    {
        Vector2[] p = PuntosCamino();
        List<Vector2> lista = new List<Vector2>();

        for (int i = 0; i < p.Length - 1; i++)
        {
            float largo = Vector2.Distance(p[i], p[i + 1]);
            int n = Mathf.Max(1, Mathf.RoundToInt(largo / paso));
            for (int j = 0; j < n; j++)
                lista.Add(Vector2.Lerp(p[i], p[i + 1], j / (float)n));
        }
        lista.Add(p[p.Length - 1]);
        return lista;
    }

    /// <summary>
    /// Distancia de un punto al camino. Devuelve tambien el indice de la muestra
    /// mas cercana, que usamos para saber a que altura hay que nivelar.
    /// Solo mira muestras cercanas, para que no tarde una eternidad.
    /// </summary>
    public static float DistanciaCamino(float x, float z, List<Vector2> muestras,
                                        float maxBusqueda, out int indiceCercano)
    {
        float mejor = float.MaxValue;
        indiceCercano = -1;

        for (int i = 0; i < muestras.Count; i++)
        {
            float dx = x - muestras[i].x;
            if (dx > maxBusqueda || dx < -maxBusqueda) continue;
            float dz = z - muestras[i].y;
            if (dz > maxBusqueda || dz < -maxBusqueda) continue;

            float d2 = dx * dx + dz * dz;
            if (d2 < mejor)
            {
                mejor = d2;
                indiceCercano = i;
            }
        }
        return indiceCercano < 0 ? float.MaxValue : Mathf.Sqrt(mejor);
    }

    // ------------------------------------------------------------- Utilidades

    /// <summary>Interpolacion suave entre dos bordes (tipo smoothstep de shaders).</summary>
    public static float Paso(float borde0, float borde1, float x)
    {
        if (Mathf.Approximately(borde0, borde1)) return x < borde0 ? 0f : 1f;
        float t = Mathf.Clamp01((x - borde0) / (borde1 - borde0));
        return t * t * (3f - 2f * t);
    }

    /// <summary>Ruido Perlin con varias octavas. Devuelve valores entre 0 y 1.</summary>
    public static float Fbm(float x, float y, int octavas, float frecuencia, float persistencia = 0.5f)
    {
        float suma = 0f, amplitud = 1f, normal = 0f, f = frecuencia;
        for (int i = 0; i < octavas; i++)
        {
            suma += amplitud * Mathf.PerlinNoise(x * f + 1000f, y * f + 1000f);
            normal += amplitud;
            amplitud *= persistencia;
            f *= 2f;
        }
        return suma / Mathf.Max(normal, 0.0001f);
    }

    /// <summary>Ruido con crestas: genera cumbres afiladas para las montanas.</summary>
    public static float Crestas(float x, float y, int octavas, float frecuencia)
    {
        float suma = 0f, amplitud = 1f, normal = 0f, f = frecuencia;
        for (int i = 0; i < octavas; i++)
        {
            float n = Mathf.PerlinNoise(x * f + 500f, y * f + 500f) * 2f - 1f;
            suma += amplitud * (1f - Mathf.Abs(n));
            normal += amplitud;
            amplitud *= 0.5f;
            f *= 2f;
        }
        return suma / Mathf.Max(normal, 0.0001f);
    }
}
