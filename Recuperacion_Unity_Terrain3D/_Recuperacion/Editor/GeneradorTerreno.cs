using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea y esculpe el Terrain: relieve (pradera + rio + montana nevada),
/// Terrain Layers con Normal Map, pintado de texturas, arboles y hierba.
/// </summary>
public static class GeneradorTerreno
{
    const string RUTA_TEX = "Assets/_Recuperacion/Texturas/";
    const string RUTA_GEN = "Assets/_Recuperacion/Generado/";

    // ======================================================================
    //  PUNTO DE ENTRADA
    // ======================================================================
    public static Terrain Crear(Transform padre, GameObject[] prefabsArboles)
    {
        PrepararCarpetas();
        ConfigurarImportadores();

        // --- 1. Datos del terreno -----------------------------------------
        TerrainData td = new TerrainData();
        td.heightmapResolution = ConfigRecuperacion.ResAltura;   // primero la resolucion
        td.size = new Vector3(ConfigRecuperacion.TamX,           // y luego el tamano
                              ConfigRecuperacion.AlturaMax,
                              ConfigRecuperacion.TamZ);
        td.alphamapResolution = ConfigRecuperacion.ResAlpha;
        td.baseMapResolution = 1024;
        td.SetDetailResolution(ConfigRecuperacion.ResDetalle, ConfigRecuperacion.ParcheDetalle);
        td.name = "TerrenoRecuperacion";

        string rutaTerreno = RUTA_GEN + "TerrenoRecuperacion.asset";
        AssetDatabase.DeleteAsset(rutaTerreno);
        AssetDatabase.CreateAsset(td, rutaTerreno);

        // --- 2. Relieve ----------------------------------------------------
        EditorUtility.DisplayProgressBar("Generando escena", "Esculpiendo el relieve...", 0.1f);
        float[,] alturas = GenerarAlturas();
        td.SetHeights(0, 0, alturas);

        // --- 3. Capas de textura (con Normal Map) --------------------------
        EditorUtility.DisplayProgressBar("Generando escena", "Creando Terrain Layers...", 0.3f);
        td.terrainLayers = CrearCapas();

        // --- 4. Pintado ----------------------------------------------------
        EditorUtility.DisplayProgressBar("Generando escena", "Pintando texturas...", 0.4f);
        PintarTexturas(td);

        // --- 5. Objeto en la escena ----------------------------------------
        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terreno";
        go.transform.SetParent(padre, false);
        go.transform.position = Vector3.zero;
        Terrain terreno = go.GetComponent<Terrain>();

        // --- 6. Vegetacion --------------------------------------------------
        EditorUtility.DisplayProgressBar("Generando escena", "Plantando arboles...", 0.6f);
        ColocarArboles(td, prefabsArboles);

        EditorUtility.DisplayProgressBar("Generando escena", "Sembrando hierba...", 0.75f);
        ColocarHierba(td);

        // --- 7. Ajustes de calidad -----------------------------------------
        terreno.heightmapPixelError = 3f;
        terreno.basemapDistance = 1500f;
        terreno.treeDistance = 3000f;
        terreno.treeBillboardDistance = 180f;
        terreno.treeCrossFadeLength = 25f;
        terreno.detailObjectDistance = 220f;
        terreno.detailObjectDensity = 1f;

        TerrainCollider col = go.GetComponent<TerrainCollider>();
        if (col != null) col.terrainData = td;

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        return terreno;
    }

    // ======================================================================
    //  RELIEVE
    // ======================================================================
    static float[,] GenerarAlturas()
    {
        int res = ConfigRecuperacion.ResAltura;
        float[,] h = new float[res, res];

        for (int iz = 0; iz < res; iz++)
        {
            float z = iz / (float)(res - 1) * ConfigRecuperacion.TamZ;
            for (int ix = 0; ix < res; ix++)
            {
                float x = ix / (float)(res - 1) * ConfigRecuperacion.TamX;
                h[iz, ix] = AlturaEn(x, z);
            }
        }

        NivelarCamino(h);
        Suavizar(h, 1);
        return h;
    }

    /// <summary>
    /// Altura (0..1) de un punto del mapa. Aqui esta toda la "forma" del escenario.
    /// </summary>
    static float AlturaEn(float x, float z)
    {
        // --- Pradera: base baja con colinas muy suaves ---------------------
        float y = ConfigRecuperacion.BasePradera;
        y += (ConfigRecuperacion.Fbm(x * 0.004f, z * 0.004f, 4, 1f) - 0.5f) * 0.05f;

        // --- Zona montanosa: sube segun nos alejamos del rio ---------------
        float subida = ConfigRecuperacion.Paso(295f, 470f, z);
        if (subida > 0f)
        {
            float crestas = ConfigRecuperacion.Crestas(x * 0.0045f, z * 0.0045f, 5, 1f);
            float masa = ConfigRecuperacion.Fbm(x * 0.0022f, z * 0.0022f, 3, 1f);
            y += subida * (0.08f + 0.30f * masa + 0.30f * crestas);
        }

        // --- Pico principal nevado (como en las referencias) ---------------
        y += Cono(x, z, 318f, 455f, 175f, 0.36f, 2.1f);
        // --- Picos secundarios para dar fondo -------------------------------
        y += Cono(x, z, 130f, 470f, 140f, 0.26f, 2.2f);
        y += Cono(x, z, 440f, 430f, 130f, 0.22f, 2.2f);
        y += Cono(x, z, 240f, 390f, 90f, 0.10f, 2.4f);

        // --- Bordes elevados: cierran el mapa de forma natural -------------
        float dBorde = Mathf.Min(Mathf.Min(x, ConfigRecuperacion.TamX - x),
                                 Mathf.Min(z, ConfigRecuperacion.TamZ - z));
        float borde = 1f - ConfigRecuperacion.Paso(0f, 75f, dBorde);
        // En la pradera los bordes son colinas; en la montana, paredes altas.
        float fuerzaBorde = 0.42f + 0.58f * ConfigRecuperacion.Paso(200f, 330f, z);
        y += borde * fuerzaBorde * (0.26f + 0.14f * ConfigRecuperacion.Fbm(x * 0.01f, z * 0.01f, 3, 1f));

        // --- Cauce del rio: se excava al final, asi corta tambien los bordes
        float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
        float excavar = 1f - ConfigRecuperacion.Paso(ConfigRecuperacion.AnchoRioMitad,
                                                     ConfigRecuperacion.AnchoRioMitad + 28f, dRio);
        if (excavar > 0f)
        {
            // El fondo sube un poco hacia las orillas: queda forma de cuenco.
            float fondo = ConfigRecuperacion.FondoRio +
                          0.012f * ConfigRecuperacion.Paso(0f, ConfigRecuperacion.AnchoRioMitad, dRio);
            y = Mathf.Lerp(y, fondo, excavar);
        }

        return Comprimir(y);
    }

    /// <summary>
    /// Evita que las cumbres se queden planas al chocar con el tope de altura:
    /// a partir de cierto punto la subida se va frenando poco a poco.
    /// </summary>
    static float Comprimir(float y)
    {
        const float umbral = 0.70f;
        const float techo = 0.97f;
        if (y <= umbral) return Mathf.Clamp01(y);
        float exceso = y - umbral;
        return umbral + (techo - umbral) * (1f - Mathf.Exp(-exceso * 2.2f));
    }

    /// <summary>Monta un cono suave (una montana) en un punto del mapa.</summary>
    static float Cono(float x, float z, float cx, float cz, float radio, float altura, float dureza)
    {
        float d = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz));
        float t = Mathf.Clamp01(1f - d / radio);
        // Un poco de ruido para que el cono no sea perfecto.
        float ruido = 0.85f + 0.3f * ConfigRecuperacion.Fbm(x * 0.012f, z * 0.012f, 3, 1f);
        return altura * Mathf.Pow(t, dureza) * ruido;
    }

    // ----------------------------------------------------------------------
    //  Nivelado del camino: que se pueda andar sin saltos ni cuestas brutas
    // ----------------------------------------------------------------------
    static void NivelarCamino(float[,] h)
    {
        List<Vector2> muestras = ConfigRecuperacion.MuestrasCamino(2f);
        int n = muestras.Count;
        float[] perfil = new float[n];
        bool[] esPuente = new bool[n];

        // 1) Altura actual del terreno bajo cada punto del camino.
        for (int i = 0; i < n; i++)
        {
            float x = muestras[i].x, z = muestras[i].y;
            float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
            esPuente[i] = dRio < ConfigRecuperacion.AnchoRioMitad + 12f;
            perfil[i] = Muestrear(h, x, z);
        }

        // 2) En el tramo del puente no usamos la altura del cauce:
        //    unimos en linea recta las dos orillas.
        int ini = -1;
        for (int i = 0; i < n; i++)
        {
            if (esPuente[i] && ini < 0) ini = i;
            if (!esPuente[i] && ini >= 0)
            {
                int antes = Mathf.Max(ini - 1, 0);
                int despues = i;
                for (int k = ini; k < despues; k++)
                {
                    float t = (k - antes) / (float)Mathf.Max(despues - antes, 1);
                    perfil[k] = Mathf.Lerp(perfil[antes], perfil[despues], t);
                }
                ini = -1;
            }
        }

        // 3) Suavizamos el perfil (media movil) para que no haya baches.
        for (int pasada = 0; pasada < 4; pasada++)
        {
            float[] copia = (float[])perfil.Clone();
            for (int i = 0; i < n; i++)
            {
                float suma = 0f; int cuenta = 0;
                for (int k = -5; k <= 5; k++)
                {
                    int j = Mathf.Clamp(i + k, 0, n - 1);
                    suma += copia[j]; cuenta++;
                }
                perfil[i] = suma / cuenta;
            }
        }

        // 4) Limitamos la pendiente maxima entre puntos seguidos.
        const float maxDelta = 0.0060f;   // ~1 metro cada 2 metros de avance
        for (int i = 1; i < n; i++)
            perfil[i] = Mathf.Clamp(perfil[i], perfil[i - 1] - maxDelta, perfil[i - 1] + maxDelta);
        for (int i = n - 2; i >= 0; i--)
            perfil[i] = Mathf.Clamp(perfil[i], perfil[i + 1] - maxDelta, perfil[i + 1] + maxDelta);

        // 5) Aplicamos el perfil al terreno alrededor del camino.
        int res = ConfigRecuperacion.ResAltura;
        for (int iz = 0; iz < res; iz++)
        {
            float z = iz / (float)(res - 1) * ConfigRecuperacion.TamZ;
            for (int ix = 0; ix < res; ix++)
            {
                float x = ix / (float)(res - 1) * ConfigRecuperacion.TamX;

                // El cauce no se toca: ahi se cruza por el puente.
                float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
                if (dRio < ConfigRecuperacion.AnchoRioMitad + 4f) continue;

                int idx;
                float d = ConfigRecuperacion.DistanciaCamino(x, z, muestras, 24f, out idx);
                if (idx < 0) continue;

                float peso = 1f - ConfigRecuperacion.Paso(ConfigRecuperacion.AnchoCamino, 22f, d);
                if (peso <= 0.001f) continue;

                h[iz, ix] = Mathf.Lerp(h[iz, ix], perfil[idx], peso);
            }
        }
    }

    /// <summary>Lee la altura del mapa en coordenadas de mundo (interpolada).</summary>
    static float Muestrear(float[,] h, float x, float z)
    {
        int res = ConfigRecuperacion.ResAltura;
        float fx = Mathf.Clamp(x / ConfigRecuperacion.TamX * (res - 1), 0, res - 1.001f);
        float fz = Mathf.Clamp(z / ConfigRecuperacion.TamZ * (res - 1), 0, res - 1.001f);
        int x0 = (int)fx, z0 = (int)fz;
        int x1 = Mathf.Min(x0 + 1, res - 1), z1 = Mathf.Min(z0 + 1, res - 1);
        float tx = fx - x0, tz = fz - z0;

        float a = Mathf.Lerp(h[z0, x0], h[z0, x1], tx);
        float b = Mathf.Lerp(h[z1, x0], h[z1, x1], tx);
        return Mathf.Lerp(a, b, tz);
    }

    /// <summary>Desenfoque sencillo para quitar escalones del relieve.</summary>
    static void Suavizar(float[,] h, int pasadas)
    {
        int res = ConfigRecuperacion.ResAltura;
        for (int p = 0; p < pasadas; p++)
        {
            float[,] copia = (float[,])h.Clone();
            for (int z = 1; z < res - 1; z++)
                for (int x = 1; x < res - 1; x++)
                {
                    float suma = 0f;
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                            suma += copia[z + dz, x + dx];
                    h[z, x] = suma / 9f;
                }
        }
    }

    // ======================================================================
    //  TERRAIN LAYERS (texturas PBR con Normal Map)
    // ======================================================================
    static TerrainLayer[] CrearCapas()
    {
        return new TerrainLayer[]
        {
            Capa("Capa_0_Cesped",   "pradera_cesped",  9f,  0.05f),
            Capa("Capa_1_Tierra",   "pradera_tierra",  7f,  0.05f),
            Capa("Capa_2_Camino",   "camino_tierra",   6f,  0.06f),
            Capa("Capa_3_Gravilla", "camino_gravilla", 4f,  0.10f),
            Capa("Capa_4_Barro",    "rio_barro",       6f,  0.35f),
            Capa("Capa_5_Roca",     "montana_roca",   14f,  0.08f),
            Capa("Capa_6_Nieve",    "cumbre_nieve",   12f,  0.28f)
        };
    }

    static TerrainLayer Capa(string nombreAsset, string baseTextura, float tile, float brillo)
    {
        TerrainLayer capa = new TerrainLayer();
        capa.diffuseTexture = Cargar(baseTextura + "_albedo");
        capa.normalMapTexture = Cargar(baseTextura + "_normal");   // <- el Normal Map
        capa.normalScale = 1f;
        capa.tileSize = new Vector2(tile, tile);
        capa.tileOffset = Vector2.zero;
        capa.smoothness = brillo;
        capa.metallic = 0f;
        capa.specular = new Color(0.07f, 0.07f, 0.07f);

        string ruta = RUTA_GEN + nombreAsset + ".terrainlayer";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(capa, ruta);
        return capa;
    }

    static Texture2D Cargar(string nombre)
    {
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(RUTA_TEX + nombre + ".png");
        if (t == null)
            Debug.LogWarning("[Generador] No encuentro la textura: " + RUTA_TEX + nombre + ".png");
        return t;
    }

    // ======================================================================
    //  PINTADO DE TEXTURAS
    // ======================================================================
    static void PintarTexturas(TerrainData td)
    {
        int res = ConfigRecuperacion.ResAlpha;
        int capas = td.terrainLayers.Length;
        float[,,] mapa = new float[res, res, capas];
        List<Vector2> muestras = ConfigRecuperacion.MuestrasCamino(3f);

        float[] w = new float[capas];

        for (int iz = 0; iz < res; iz++)
        {
            float v = iz / (float)(res - 1);
            float z = v * ConfigRecuperacion.TamZ;

            for (int ix = 0; ix < res; ix++)
            {
                float u = ix / (float)(res - 1);
                float x = u * ConfigRecuperacion.TamX;

                // Datos del punto: altura normalizada y pendiente en grados.
                float y = td.GetInterpolatedHeight(u, v) / ConfigRecuperacion.AlturaMax;
                float pendiente = td.GetSteepness(u, v);

                // Ruidos para que los bordes entre texturas no sean lineas rectas.
                float ruidoA = ConfigRecuperacion.Fbm(x * 0.02f, z * 0.02f, 3, 1f);
                float ruidoB = ConfigRecuperacion.Fbm(x * 0.06f, z * 0.06f, 2, 1f);

                // NIEVE: por encima de cierta altura, solo en el sector de montana
                // y nunca en paredes casi verticales.
                float lineaNieve = 0.46f + (ruidoA - 0.5f) * 0.10f;
                float nieve = ConfigRecuperacion.Paso(lineaNieve, lineaNieve + 0.09f, y);
                nieve *= 1f - ConfigRecuperacion.Paso(44f, 60f, pendiente);
                nieve *= ConfigRecuperacion.Paso(250f, 320f, z);

                // ROCA: en cuestas pronunciadas y en la parte alta de la montana.
                // En la pradera solo asoma roca donde la cuesta es fuerte, asi las
                // colinas del borde siguen siendo verdes y la transicion es suave.
                float zonaMontana = ConfigRecuperacion.Paso(190f, 340f, z);
                float rocaPorAltura = ConfigRecuperacion.Paso(0.28f, 0.54f, y)
                                      * (0.30f + 0.70f * zonaMontana);
                float roca = Mathf.Max(ConfigRecuperacion.Paso(24f, 42f, pendiente), rocaPorAltura);
                roca = Mathf.Clamp01(roca + (ruidoB - 0.5f) * 0.22f + (ruidoA - 0.5f) * 0.12f);

                // BARRO: orillas del rio.
                float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
                float barro = 1f - ConfigRecuperacion.Paso(
                    ConfigRecuperacion.AnchoRioMitad + 1f,
                    ConfigRecuperacion.AnchoRioMitad + 19f + (ruidoB - 0.5f) * 9f, dRio);

                // CAMINO: nucleo de tierra y bordes de gravilla.
                int idx;
                float dCam = ConfigRecuperacion.DistanciaCamino(x, z, muestras, 28f, out idx);
                float nucleo = 1f - ConfigRecuperacion.Paso(
                    ConfigRecuperacion.AnchoCamino * 0.7f,
                    ConfigRecuperacion.AnchoCamino + 2.5f + (ruidoB - 0.5f) * 3f, dCam);
                float bordeCam = (1f - ConfigRecuperacion.Paso(
                    ConfigRecuperacion.AnchoCamino + 1f,
                    ConfigRecuperacion.AnchoCamino + 10f, dCam)) * (1f - nucleo);

                // Sobre el cauce no se pinta camino: ahi va el puente.
                float sobreRio = 1f - ConfigRecuperacion.Paso(
                    ConfigRecuperacion.AnchoRioMitad - 2f,
                    ConfigRecuperacion.AnchoRioMitad + 7f, dRio);
                nucleo *= 1f - sobreRio;
                bordeCam *= 1f - sobreRio;

                // Calvas de tierra en la pradera.
                float calvas = ConfigRecuperacion.Paso(0.58f, 0.76f,
                               ConfigRecuperacion.Fbm(x * 0.012f, z * 0.012f, 3, 1f));

                float libre = Mathf.Clamp01(1f - nieve - roca);
                float sinCamino = Mathf.Clamp01(1f - nucleo - bordeCam);

                w[0] = libre * (1f - barro) * sinCamino * (1f - calvas);   // cesped
                w[1] = libre * (1f - barro) * sinCamino * calvas;          // tierra
                w[2] = nucleo * (1f - nieve * 0.85f);                      // camino
                w[3] = bordeCam * (1f - nieve * 0.85f);                    // gravilla
                w[4] = barro * libre * sinCamino;                          // barro ribera
                w[5] = roca;                                               // roca
                w[6] = nieve;                                              // nieve

                // Normalizamos: la suma de los pesos de un punto debe dar 1.
                float suma = 0f;
                for (int c = 0; c < capas; c++) suma += w[c];
                if (suma < 0.0001f) { w[0] = 1f; suma = 1f; }

                for (int c = 0; c < capas; c++)
                    mapa[iz, ix, c] = w[c] / suma;
            }
        }

        td.SetAlphamaps(0, 0, mapa);
    }

    // ======================================================================
    //  ARBOLES
    // ======================================================================
    static void ColocarArboles(TerrainData td, GameObject[] prefabs)
    {
        List<GameObject> validos = new List<GameObject>();
        if (prefabs != null)
            foreach (GameObject g in prefabs)
                if (g != null) validos.Add(g);

        if (validos.Count == 0)
        {
            Debug.LogWarning("[Generador] No hay prefabs de arbol: me salto los arboles.");
            return;
        }

        TreePrototype[] protos = new TreePrototype[validos.Count];
        for (int i = 0; i < validos.Count; i++)
            protos[i] = new TreePrototype { prefab = validos[i], bendFactor = 0.3f };
        td.treePrototypes = protos;

        List<TreeInstance> arboles = new List<TreeInstance>();
        System.Random r = new System.Random(4321);
        List<Vector2> muestras = ConfigRecuperacion.MuestrasCamino(5f);

        Vector2 posCofre = new Vector2(334f, 428f);
        Vector2 posLlave = new Vector2(205f, 188f);

        int intentos = 14000;
        for (int i = 0; i < intentos; i++)
        {
            float x = (float)r.NextDouble() * ConfigRecuperacion.TamX;
            float z = (float)r.NextDouble() * ConfigRecuperacion.TamZ;
            float u = x / ConfigRecuperacion.TamX;
            float v = z / ConfigRecuperacion.TamZ;

            float yMundo = td.GetInterpolatedHeight(u, v);
            float yNorm = yMundo / ConfigRecuperacion.AlturaMax;
            float pendiente = td.GetSteepness(u, v);

            if (yNorm > 0.40f) continue;                       // linea de arboles
            if (yMundo < ConfigRecuperacion.AlturaAgua + 1.5f) continue;   // dentro del agua
            if (pendiente > 30f) continue;                     // cuesta demasiado fuerte

            float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
            if (dRio < ConfigRecuperacion.AnchoRioMitad + 9f) continue;

            int idx;
            if (ConfigRecuperacion.DistanciaCamino(x, z, muestras, 30f, out idx) < 13f) continue;
            if (Vector2.Distance(new Vector2(x, z), posCofre) < 18f) continue;
            if (Vector2.Distance(new Vector2(x, z), posLlave) < 14f) continue;

            // Mascara de bosque: zonas densas y claros abiertos, como en la referencia.
            float mascara = ConfigRecuperacion.Fbm(x * 0.008f, z * 0.008f, 3, 1f);
            if (mascara < 0.46f) continue;
            float prob = Mathf.Clamp01((mascara - 0.46f) / 0.35f);
            if (r.NextDouble() > prob) continue;

            // Que tipo de arbol toca: mas coniferas cuanto mas arriba.
            int tipo;
            double dado = r.NextDouble();
            if (yNorm > 0.26f) tipo = dado < 0.75 ? 1 % validos.Count : 2 % validos.Count;
            else if (z < ConfigRecuperacion.ZRio(x)) tipo = dado < 0.55 ? 0 : (dado < 0.85 ? 1 % validos.Count : 2 % validos.Count);
            else tipo = dado < 0.45 ? 1 % validos.Count : (dado < 0.8 ? 0 : 2 % validos.Count);

            float escala = 0.75f + (float)r.NextDouble() * 0.65f;

            arboles.Add(new TreeInstance
            {
                position = new Vector3(u, yNorm, v),
                prototypeIndex = Mathf.Clamp(tipo, 0, validos.Count - 1),
                widthScale = escala * (0.9f + (float)r.NextDouble() * 0.2f),
                heightScale = escala,
                color = Color.white,
                lightmapColor = Color.white,
                rotation = (float)r.NextDouble() * Mathf.PI * 2f
            });
        }

        td.SetTreeInstances(arboles.ToArray(), true);
        Debug.Log("[Generador] Arboles colocados: " + arboles.Count);
    }

    // ======================================================================
    //  HIERBA Y MATORRALES (Detail layers)
    // ======================================================================
    static void ColocarHierba(TerrainData td)
    {
        Texture2D[] texturas =
        {
            Cargar("veg_hierba_alta"),
            Cargar("veg_flores"),
            Cargar("veg_matorral")
        };

        List<DetailPrototype> protos = new List<DetailPrototype>();
        foreach (Texture2D tex in texturas)
        {
            if (tex == null) continue;
            protos.Add(new DetailPrototype
            {
                prototypeTexture = tex,
                usePrototypeMesh = false,
                renderMode = DetailRenderMode.GrassBillboard,
                healthyColor = new Color(0.78f, 0.92f, 0.70f),
                dryColor = new Color(0.78f, 0.75f, 0.45f),
                minWidth = 1.1f,
                maxWidth = 2.2f,
                minHeight = 0.8f,
                maxHeight = 1.8f,
                noiseSpread = 0.4f
            });
        }
        if (protos.Count == 0) return;
        td.detailPrototypes = protos.ToArray();

        int res = ConfigRecuperacion.ResDetalle;
        int[][,] capas = new int[protos.Count][,];
        for (int c = 0; c < protos.Count; c++) capas[c] = new int[res, res];

        List<Vector2> muestras = ConfigRecuperacion.MuestrasCamino(5f);
        System.Random r = new System.Random(777);

        for (int iz = 0; iz < res; iz++)
        {
            float v = (iz + 0.5f) / res;
            float z = v * ConfigRecuperacion.TamZ;

            for (int ix = 0; ix < res; ix++)
            {
                float u = (ix + 0.5f) / res;
                float x = u * ConfigRecuperacion.TamX;

                float yMundo = td.GetInterpolatedHeight(u, v);
                float yNorm = yMundo / ConfigRecuperacion.AlturaMax;
                float pendiente = td.GetSteepness(u, v);

                if (yNorm > 0.34f) continue;                    // muy alto: roca y nieve
                if (yMundo < ConfigRecuperacion.AlturaAgua + 0.6f) continue;
                if (pendiente > 32f) continue;

                float dRio = Mathf.Abs(z - ConfigRecuperacion.ZRio(x));
                if (dRio < ConfigRecuperacion.AnchoRioMitad + 3f) continue;

                int idx;
                float dCam = ConfigRecuperacion.DistanciaCamino(x, z, muestras, 26f, out idx);
                if (dCam < ConfigRecuperacion.AnchoCamino + 1.5f) continue;

                // Menos hierba justo al lado del camino, para que se note el sendero.
                float factorCamino = ConfigRecuperacion.Paso(ConfigRecuperacion.AnchoCamino + 1.5f,
                                                             ConfigRecuperacion.AnchoCamino + 9f, dCam);
                float praderaFactor = 1f - ConfigRecuperacion.Paso(0.22f, 0.34f, yNorm);

                // Hierba alta: por toda la pradera.
                float densidadHierba = ConfigRecuperacion.Fbm(x * 0.015f, z * 0.015f, 3, 1f);
                int hierba = Mathf.RoundToInt(Mathf.Clamp01(densidadHierba * 1.3f - 0.25f)
                                              * 7f * factorCamino * praderaFactor);
                if (hierba > 0) capas[0][iz, ix] = hierba;

                // Flores: en manchas sueltas.
                if (protos.Count > 1)
                {
                    float manchas = ConfigRecuperacion.Fbm(x * 0.035f + 50f, z * 0.035f + 50f, 2, 1f);
                    if (manchas > 0.66f)
                        capas[1][iz, ix] = Mathf.RoundToInt((manchas - 0.66f) * 12f * factorCamino * praderaFactor);
                }

                // Matorrales: cerca del rio y en los claros del bosque.
                if (protos.Count > 2)
                {
                    float cercaRio = 1f - ConfigRecuperacion.Paso(ConfigRecuperacion.AnchoRioMitad + 3f,
                                                                 ConfigRecuperacion.AnchoRioMitad + 45f, dRio);
                    float arbustos = ConfigRecuperacion.Fbm(x * 0.022f + 90f, z * 0.022f + 90f, 2, 1f);
                    float cantidad = Mathf.Max(cercaRio * 0.8f, Mathf.Clamp01(arbustos * 1.2f - 0.55f));
                    int n = Mathf.RoundToInt(cantidad * 5f * factorCamino);
                    if (n > 0) capas[2][iz, ix] = n;
                }
            }
        }

        for (int c = 0; c < protos.Count; c++)
            td.SetDetailLayer(0, 0, c, capas[c]);

        // Viento en la hierba, para que no se vea estatica.
        td.wavingGrassAmount = 0.35f;
        td.wavingGrassSpeed = 0.4f;
        td.wavingGrassStrength = 0.5f;
        td.wavingGrassTint = new Color(0.75f, 0.82f, 0.65f, 1f);
    }

    // ======================================================================
    //  AUXILIARES
    // ======================================================================
    public static void PrepararCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Recuperacion"))
            AssetDatabase.CreateFolder("Assets", "_Recuperacion");
        if (!AssetDatabase.IsValidFolder("Assets/_Recuperacion/Generado"))
            AssetDatabase.CreateFolder("Assets/_Recuperacion", "Generado");
    }

    /// <summary>
    /// Marca los *_normal como Normal Map y deja los billboards con transparencia.
    /// Si no se hace, Unity los trata como texturas normales y el relieve no se ve.
    /// </summary>
    static void ConfigurarImportadores()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Recuperacion/Texturas"))
        {
            Debug.LogError("[Generador] No existe la carpeta Assets/_Recuperacion/Texturas. " +
                           "Copia la carpeta _Recuperacion entera dentro de Assets.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Recuperacion/Texturas" });
        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
            bool cambiado = false;

            if (nombre.EndsWith("_normal"))
            {
                if (imp.textureType != TextureImporterType.NormalMap)
                {
                    imp.textureType = TextureImporterType.NormalMap;
                    cambiado = true;
                }
            }
            else if (nombre.StartsWith("veg_"))
            {
                if (!imp.alphaIsTransparency)
                {
                    imp.textureType = TextureImporterType.Default;
                    imp.alphaIsTransparency = true;
                    imp.wrapMode = TextureWrapMode.Clamp;
                    cambiado = true;
                }
            }

            if (cambiado)
            {
                imp.SaveAndReimport();
            }
        }
    }
}
