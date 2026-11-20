using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Crea todo lo que no es el Terrain: agua del rio, puente, rocas, troncos,
/// la llave, el cofre final, la flecha guia, la interfaz, las luces, la niebla
/// y los limites invisibles del mapa.
/// </summary>
public static class GeneradorObjetos
{
    const string RUTA_GEN = "Assets/_Recuperacion/Generado/";
    const string RUTA_TEX = "Assets/_Recuperacion/Texturas/";

    // ======================================================================
    //  MATERIALES
    // ======================================================================
    static bool UsaURP()
    {
        return GraphicsSettings.currentRenderPipeline != null;
    }

    public static Material NuevoMaterial(string nombre, Color color, float metalico, float brillo,
                                         Texture2D albedo = null, Texture2D normal = null,
                                         float tiling = 1f)
    {
        Shader sh = UsaURP() ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Diffuse");

        Material m = new Material(sh);

        if (UsaURP())
        {
            m.SetColor("_BaseColor", color);
            if (albedo != null) m.SetTexture("_BaseMap", albedo);
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", brillo);
        }
        else
        {
            m.SetColor("_Color", color);
            if (albedo != null) m.SetTexture("_MainTex", albedo);
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Glossiness", brillo);
        }

        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
        }

        m.mainTextureScale = new Vector2(tiling, tiling);

        string ruta = RUTA_GEN + nombre + ".mat";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    /// <summary>Material transparente, para el agua del rio.</summary>
    static Material MaterialAgua()
    {
        Shader sh = UsaURP() ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);

        Color agua = new Color(0.16f, 0.52f, 0.62f, 0.72f);

        if (UsaURP())
        {
            m.SetColor("_BaseColor", agua);
            m.SetFloat("_Metallic", 0.15f);
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Surface", 1f);        // 1 = transparente
            m.SetFloat("_Blend", 0f);          // 0 = alpha
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetFloat("_ZWrite", 0f);
        }
        else
        {
            m.SetColor("_Color", agua);
            m.SetFloat("_Metallic", 0.15f);
            m.SetFloat("_Glossiness", 0.92f);
            // Pasamos el shader Standard a modo Transparente "a mano".
            m.SetFloat("_Mode", 3f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        m.renderQueue = 3000;

        string ruta = RUTA_GEN + "Mat_Agua.mat";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static Texture2D Tex(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(RUTA_TEX + nombre + ".png");
    }

    // ======================================================================
    //  AGUA DEL RIO
    // ======================================================================
    public static GameObject CrearRio(Transform padre)
    {
        float medio = ConfigRecuperacion.AnchoRioMitad + 7f;
        int pasos = 120;
        float x0 = -25f, x1 = ConfigRecuperacion.TamX + 25f;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangulos = new List<int>();

        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            float x = Mathf.Lerp(x0, x1, t);
            float zc = ConfigRecuperacion.ZRio(x);

            vertices.Add(new Vector3(x, 0f, zc - medio));
            vertices.Add(new Vector3(x, 0f, zc + medio));
            uvs.Add(new Vector2(t * 14f, 0f));
            uvs.Add(new Vector2(t * 14f, 1f));

            if (i < pasos)
            {
                int b = i * 2;
                triangulos.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
            }
        }

        Mesh malla = new Mesh { name = "MallaAgua" };
        malla.SetVertices(vertices);
        malla.SetUVs(0, uvs);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();

        string rutaMalla = RUTA_GEN + "MallaAgua.asset";
        AssetDatabase.DeleteAsset(rutaMalla);
        AssetDatabase.CreateAsset(malla, rutaMalla);

        GameObject go = new GameObject("Agua_Rio");
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(0f, ConfigRecuperacion.AlturaAgua, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        go.AddComponent<MeshRenderer>().sharedMaterial = MaterialAgua();
        return go;
    }

    // ======================================================================
    //  PUENTE
    // ======================================================================
    public static GameObject CrearPuente(Transform padre, Terrain terreno)
    {
        float x = ConfigRecuperacion.CruceX;
        float zc = ConfigRecuperacion.ZRio(x);

        // Altura de las dos orillas (donde el camino ya esta nivelado).
        float hA = terreno.SampleHeight(new Vector3(x, 0f, zc - 38f));
        float hB = terreno.SampleHeight(new Vector3(x, 0f, zc + 38f));
        float altura = Mathf.Max(hA, hB) + 0.35f;

        Material madera = NuevoMaterial("Mat_Madera", new Color(0.42f, 0.30f, 0.19f), 0f, 0.12f,
                                        Tex("arbol_corteza_albedo"), Tex("arbol_corteza_normal"), 3f);

        GameObject raiz = new GameObject("Puente");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = new Vector3(x, altura, zc);

        // Tablero (este si tiene collider: es por donde se anda).
        Cubo("Tablero", raiz.transform, Vector3.zero,
             new Vector3(8f, 0.5f, 76f), madera, true);

        // Barandillas a los lados.
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Cubo("Barandilla_" + (lado < 0 ? "Izq" : "Der"), raiz.transform,
                 new Vector3(lado * 3.8f, 1.15f, 0f), new Vector3(0.3f, 0.3f, 76f), madera, false);

            for (int i = -5; i <= 5; i++)
            {
                Cubo("Poste", raiz.transform,
                     new Vector3(lado * 3.8f, 0.65f, i * 7f), new Vector3(0.35f, 1.3f, 0.35f), madera, false);
            }
        }

        // Vigas por debajo, para que no parezca una tabla flotando.
        for (int i = -2; i <= 2; i++)
        {
            Cubo("Viga", raiz.transform, new Vector3(0f, -0.45f, i * 17f),
                 new Vector3(8.4f, 0.45f, 1.2f), madera, false);
        }

        // Tablas del suelo (solo decorativas).
        for (int i = -12; i <= 12; i++)
        {
            Cubo("Tabla", raiz.transform, new Vector3(0f, 0.28f, i * 3f),
                 new Vector3(7.6f, 0.08f, 2.4f), madera, false);
        }

        return raiz;
    }

    // ======================================================================
    //  ROCAS Y TRONCOS
    // ======================================================================
    public static void CrearRocasYTroncos(Transform padre, Terrain terreno)
    {
        Material roca = NuevoMaterial("Mat_Roca", new Color(0.52f, 0.51f, 0.50f), 0f, 0.1f,
                                      Tex("montana_roca_albedo"), Tex("montana_roca_normal"), 1.5f);
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(RUTA_GEN + "Mat_Madera.mat");
        if (madera == null)
            madera = NuevoMaterial("Mat_Madera", new Color(0.42f, 0.30f, 0.19f), 0f, 0.12f,
                                   Tex("arbol_corteza_albedo"), Tex("arbol_corteza_normal"), 3f);

        GameObject grupo = new GameObject("Rocas_y_Troncos");
        grupo.transform.SetParent(padre, false);

        System.Random r = new System.Random(99);
        List<Vector2> muestras = ConfigRecuperacion.MuestrasCamino(6f);

        int colocadas = 0, intentos = 0;
        while (colocadas < 90 && intentos < 4000)
        {
            intentos++;
            float x = (float)r.NextDouble() * ConfigRecuperacion.TamX;
            float z = (float)r.NextDouble() * ConfigRecuperacion.TamZ;
            float y = terreno.SampleHeight(new Vector3(x, 0f, z));

            if (y < ConfigRecuperacion.AlturaAgua - 0.5f) continue;

            int idx;
            if (ConfigRecuperacion.DistanciaCamino(x, z, muestras, 20f, out idx) < 7f) continue;

            float escala = 1.2f + (float)r.NextDouble() * 4.2f;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Roca";
            g.transform.SetParent(grupo.transform, false);
            // La hundimos un poco para que no parezca una bola posada encima.
            g.transform.position = new Vector3(x, y - escala * 0.35f, z);
            g.transform.localScale = new Vector3(escala * (0.8f + (float)r.NextDouble() * 0.6f),
                                                 escala * (0.5f + (float)r.NextDouble() * 0.4f),
                                                 escala * (0.8f + (float)r.NextDouble() * 0.6f));
            g.transform.rotation = Quaternion.Euler((float)r.NextDouble() * 40f - 20f,
                                                    (float)r.NextDouble() * 360f,
                                                    (float)r.NextDouble() * 40f - 20f);
            g.GetComponent<MeshRenderer>().sharedMaterial = roca;
            colocadas++;
        }

        // Troncos caidos repartidos por el bosque.
        int troncos = 0; intentos = 0;
        while (troncos < 16 && intentos < 2000)
        {
            intentos++;
            float x = (float)r.NextDouble() * ConfigRecuperacion.TamX;
            float z = (float)r.NextDouble() * ConfigRecuperacion.TamZ;
            float y = terreno.SampleHeight(new Vector3(x, 0f, z));
            if (y < ConfigRecuperacion.AlturaAgua + 1f) continue;
            if (y > ConfigRecuperacion.AlturaMax * 0.33f) continue;

            int idx;
            if (ConfigRecuperacion.DistanciaCamino(x, z, muestras, 20f, out idx) < 9f) continue;

            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "Tronco";
            g.transform.SetParent(grupo.transform, false);
            g.transform.position = new Vector3(x, y + 0.35f, z);
            g.transform.rotation = Quaternion.Euler(90f, (float)r.NextDouble() * 360f, 0f);
            g.transform.localScale = new Vector3(0.5f, 2.2f + (float)r.NextDouble() * 1.5f, 0.5f);
            g.GetComponent<MeshRenderer>().sharedMaterial = madera;
            troncos++;
        }
    }

    // ======================================================================
    //  LLAVE
    // ======================================================================
    public static GameObject CrearLlave(Transform padre, Terrain terreno, Vector2 posicion)
    {
        float y = terreno.SampleHeight(new Vector3(posicion.x, 0f, posicion.y));

        Material oro = NuevoMaterial("Mat_Oro", new Color(1f, 0.78f, 0.25f), 1f, 0.85f);
        if (!UsaURP())
        {
            oro.EnableKeyword("_EMISSION");
            oro.SetColor("_EmissionColor", new Color(0.45f, 0.32f, 0.05f));
        }

        GameObject raiz = new GameObject("Llave");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = new Vector3(posicion.x, y + 1.4f, posicion.y);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        // Cuerpo de la llave.
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(visual.transform, false);
        cuerpo.transform.localPosition = Vector3.zero;
        cuerpo.transform.localScale = new Vector3(0.09f, 0.45f, 0.09f);
        Limpiar(cuerpo, oro);

        // Anilla: ocho cubitos puestos en circulo.
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Anilla";
            p.transform.SetParent(visual.transform, false);
            p.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.26f, 0.52f, Mathf.Sin(ang) * 0.26f);
            p.transform.localRotation = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg, 0f);
            p.transform.localScale = new Vector3(0.09f, 0.09f, 0.22f);
            Limpiar(p, oro);
        }

        // Dientes de la llave.
        for (int i = 0; i < 2; i++)
        {
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d.name = "Diente";
            d.transform.SetParent(visual.transform, false);
            d.transform.localPosition = new Vector3(0.13f, -0.28f - i * 0.16f, 0f);
            d.transform.localScale = new Vector3(0.22f, 0.09f, 0.09f);
            Limpiar(d, oro);
        }

        // Luz para que se vea de lejos.
        GameObject luzGO = new GameObject("Luz");
        luzGO.transform.SetParent(raiz.transform, false);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.85f, 0.4f);
        luz.range = 14f;
        luz.intensity = 2.2f;

        // Zona de recogida.
        SphereCollider trigger = raiz.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2.4f;

        RecogerLlave recoger = raiz.AddComponent<RecogerLlave>();
        recoger.parteVisible = visual;

        GiroObjeto giro = raiz.AddComponent<GiroObjeto>();
        giro.velocidadGiro = 75f;
        giro.alturaFlotacion = 0.3f;

        return raiz;
    }

    // ======================================================================
    //  COFRE FINAL
    // ======================================================================
    public static GameObject CrearCofre(Transform padre, Terrain terreno, Vector2 posicion)
    {
        float y = terreno.SampleHeight(new Vector3(posicion.x, 0f, posicion.y));

        Material madera = AssetDatabase.LoadAssetAtPath<Material>(RUTA_GEN + "Mat_Madera.mat");
        if (madera == null)
            madera = NuevoMaterial("Mat_Madera", new Color(0.42f, 0.30f, 0.19f), 0f, 0.12f,
                                   Tex("arbol_corteza_albedo"), Tex("arbol_corteza_normal"), 3f);
        Material oro = AssetDatabase.LoadAssetAtPath<Material>(RUTA_GEN + "Mat_Oro.mat");
        if (oro == null) oro = NuevoMaterial("Mat_Oro", new Color(1f, 0.78f, 0.25f), 1f, 0.85f);

        GameObject raiz = new GameObject("CofreFinal");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = new Vector3(posicion.x, y, posicion.y);
        raiz.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // mirando al camino

        // Caja.
        Cubo("Caja", raiz.transform, new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 1.5f), madera, true);
        // Refuerzos dorados.
        Cubo("Refuerzo1", raiz.transform, new Vector3(-0.9f, 0.6f, 0f), new Vector3(0.18f, 1.25f, 1.56f), oro, false);
        Cubo("Refuerzo2", raiz.transform, new Vector3(0.9f, 0.6f, 0f), new Vector3(0.18f, 1.25f, 1.56f), oro, false);
        Cubo("Cerradura", raiz.transform, new Vector3(0f, 0.95f, -0.78f), new Vector3(0.35f, 0.4f, 0.12f), oro, false);

        // Tapa: va en un pivote colocado en la bisagra trasera, asi gira bien.
        GameObject pivote = new GameObject("PivoteTapa");
        pivote.transform.SetParent(raiz.transform, false);
        pivote.transform.localPosition = new Vector3(0f, 1.2f, 0.75f);

        Cubo("Tapa", pivote.transform, new Vector3(0f, 0.13f, -0.75f), new Vector3(2.4f, 0.26f, 1.5f), madera, false);
        Cubo("TapaRefuerzo", pivote.transform, new Vector3(0f, 0.3f, -0.75f), new Vector3(0.25f, 0.12f, 1.56f), oro, false);

        // Zona que detecta al jugador.
        BoxCollider zona = raiz.AddComponent<BoxCollider>();
        zona.isTrigger = true;
        zona.size = new Vector3(9f, 6f, 9f);
        zona.center = new Vector3(0f, 2f, 0f);

        CofreFinal cofre = raiz.AddComponent<CofreFinal>();
        cofre.tapa = pivote.transform;
        cofre.anguloAbierto = -105f;
        cofre.velocidadApertura = 110f;

        GameObject luzGO = new GameObject("Luz");
        luzGO.transform.SetParent(raiz.transform, false);
        luzGO.transform.localPosition = new Vector3(0f, 2f, 0f);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.9f, 0.6f);
        luz.range = 18f;
        luz.intensity = 1.8f;

        return raiz;
    }

    // ======================================================================
    //  FLECHA GUIA (la mueve el grafo de Visual Scripting)
    // ======================================================================
    public static GameObject CrearFlecha(Transform padre, Vector3 posicion)
    {
        Material naranja = NuevoMaterial("Mat_Flecha", new Color(1f, 0.55f, 0.1f), 0f, 0.5f);
        if (!UsaURP())
        {
            naranja.EnableKeyword("_EMISSION");
            naranja.SetColor("_EmissionColor", new Color(0.9f, 0.4f, 0.05f));
        }

        GameObject raiz = new GameObject("FlechaGuia");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = posicion;

        // Palo de la flecha, tumbado para que apunte hacia delante (+Z).
        GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        palo.name = "Palo";
        palo.transform.SetParent(raiz.transform, false);
        palo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        palo.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        palo.transform.localScale = new Vector3(0.14f, 0.35f, 0.14f);
        Limpiar(palo, naranja);

        // Punta: un cono hecho a mano apuntando a +Z.
        GameObject punta = new GameObject("Punta");
        punta.transform.SetParent(raiz.transform, false);
        punta.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        Mesh cono = CrearCono(0.3f, 0.55f, 16);
        string rutaCono = RUTA_GEN + "MallaCono.asset";
        AssetDatabase.DeleteAsset(rutaCono);
        AssetDatabase.CreateAsset(cono, rutaCono);
        punta.AddComponent<MeshFilter>().sharedMesh = cono;
        punta.AddComponent<MeshRenderer>().sharedMaterial = naranja;

        return raiz;
    }

    /// <summary>Crea una malla de cono que apunta hacia +Z.</summary>
    public static Mesh CrearCono(float radio, float altura, int lados)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();

        v.Add(new Vector3(0f, 0f, altura));   // 0: la punta
        v.Add(Vector3.zero);                  // 1: centro de la base

        for (int i = 0; i < lados; i++)
        {
            float a = i / (float)lados * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a) * radio, Mathf.Sin(a) * radio, 0f));
        }

        for (int i = 0; i < lados; i++)
        {
            int a = 2 + i;
            int b = 2 + (i + 1) % lados;
            t.AddRange(new[] { 0, b, a });    // lateral
            t.AddRange(new[] { 1, a, b });    // base
        }

        Mesh m = new Mesh { name = "Cono" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // ======================================================================
    //  INTERFAZ (UI)
    // ======================================================================
    public static InterfazJuego CrearInterfaz(Transform padre)
    {
        Font fuente = null;
        try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (fuente == null)
        {
            try { fuente = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        if (fuente == null)
        {
            Debug.LogWarning("[Generador] No he encontrado la fuente por defecto de Unity. " +
                             "Si los textos salen vacios, asignales una fuente a mano en el Canvas.");
        }

        GameObject canvasGO = new GameObject("Canvas_Interfaz");
        canvasGO.transform.SetParent(padre, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escala = canvasGO.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Aviso temporal (arriba, centrado) -----------------------------
        Text aviso = NuevoTexto("Texto_Aviso", canvasGO.transform, fuente, 38, TextAnchor.UpperCenter);
        RectTransform rtAviso = aviso.rectTransform;
        rtAviso.anchorMin = new Vector2(0.5f, 1f);
        rtAviso.anchorMax = new Vector2(0.5f, 1f);
        rtAviso.pivot = new Vector2(0.5f, 1f);
        rtAviso.anchoredPosition = new Vector2(0f, -60f);
        rtAviso.sizeDelta = new Vector2(1400f, 120f);
        aviso.text = "";

        // --- Indicador de la llave (arriba a la izquierda) -----------------
        Text llave = NuevoTexto("Texto_Llave", canvasGO.transform, fuente, 32, TextAnchor.UpperLeft);
        RectTransform rtLlave = llave.rectTransform;
        rtLlave.anchorMin = new Vector2(0f, 1f);
        rtLlave.anchorMax = new Vector2(0f, 1f);
        rtLlave.pivot = new Vector2(0f, 1f);
        rtLlave.anchoredPosition = new Vector2(40f, -35f);
        rtLlave.sizeDelta = new Vector2(400f, 60f);
        llave.text = "LLAVE: NO";

        // --- Panel de victoria ---------------------------------------------
        GameObject panel = new GameObject("Panel_Victoria", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;
        Image fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.72f);

        Text victoria = NuevoTexto("Texto_Victoria", panel.transform, fuente, 110, TextAnchor.MiddleCenter);
        RectTransform rtV = victoria.rectTransform;
        rtV.anchorMin = new Vector2(0.5f, 0.5f);
        rtV.anchorMax = new Vector2(0.5f, 0.5f);
        rtV.anchoredPosition = new Vector2(0f, 60f);
        rtV.sizeDelta = new Vector2(1600f, 220f);
        victoria.text = "VICTORIA";
        victoria.color = new Color(1f, 0.86f, 0.3f);

        Text sub = NuevoTexto("Texto_Subtitulo", panel.transform, fuente, 42, TextAnchor.MiddleCenter);
        RectTransform rtS = sub.rectTransform;
        rtS.anchorMin = new Vector2(0.5f, 0.5f);
        rtS.anchorMax = new Vector2(0.5f, 0.5f);
        rtS.anchoredPosition = new Vector2(0f, -70f);
        rtS.sizeDelta = new Vector2(1600f, 140f);
        sub.text = "Has abierto el cofre. Fin del recorrido.";

        panel.SetActive(false);

        InterfazJuego interfaz = canvasGO.AddComponent<InterfazJuego>();
        interfaz.textoAviso = aviso;
        interfaz.textoLlave = llave;
        interfaz.panelVictoria = panel;
        return interfaz;
    }

    static Text NuevoTexto(string nombre, Transform padre, Font fuente, int tamano, TextAnchor alineacion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Text t = go.AddComponent<Text>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        // Sombra para que se lea sobre cualquier fondo.
        Shadow sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    // ======================================================================
    //  LUCES, CIELO Y NIEBLA
    // ======================================================================
    public static Light ConfigurarAmbiente()
    {
        // Luz direccional (el sol).
        // Buscamos la luz direccional que ya trae la escena nueva de Unity.
        Light sol = null;
        GameObject existente = GameObject.Find("Directional Light");
        if (existente == null) existente = GameObject.Find("Luz_Direccional_Sol");
        if (existente != null) sol = existente.GetComponent<Light>();

        if (sol == null)
        {
            GameObject go = new GameObject("Luz_Direccional_Sol");
            sol = go.AddComponent<Light>();
            sol.type = LightType.Directional;
        }

        sol.gameObject.name = "Luz_Direccional_Sol";
        sol.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        sol.color = new Color(1f, 0.96f, 0.88f);
        sol.intensity = 1.25f;
        sol.shadows = LightShadows.Soft;
        sol.shadowStrength = 0.8f;

        // Cielo procedural, con el sol bien definido.
        Shader shCielo = Shader.Find("Skybox/Procedural");
        if (shCielo != null)
        {
            Material cielo = new Material(shCielo);
            cielo.SetFloat("_SunSize", 0.035f);
            cielo.SetFloat("_SunSizeConvergence", 6f);
            cielo.SetFloat("_AtmosphereThickness", 1.05f);
            cielo.SetColor("_SkyTint", new Color(0.52f, 0.66f, 0.86f));
            cielo.SetColor("_GroundColor", new Color(0.38f, 0.36f, 0.32f));
            cielo.SetFloat("_Exposure", 1.25f);

            string ruta = RUTA_GEN + "Mat_Cielo.mat";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(cielo, ruta);
            RenderSettings.skybox = cielo;
        }

        RenderSettings.sun = sol;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;

        // Niebla: da sensacion de distancia en la montana.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.85f);
        RenderSettings.fogDensity = 0.0032f;

        return sol;
    }

    // ======================================================================
    //  LIMITES INVISIBLES
    // ======================================================================
    public static void CrearLimites(Transform padre)
    {
        GameObject grupo = new GameObject("Limites_Invisibles");
        grupo.transform.SetParent(padre, false);

        float x = ConfigRecuperacion.TamX, z = ConfigRecuperacion.TamZ;
        float alto = 400f, grosor = 6f;

        Muro(grupo.transform, "Muro_Oeste", new Vector3(-grosor * 0.5f, alto * 0.5f, z * 0.5f),
             new Vector3(grosor, alto, z + grosor * 2f));
        Muro(grupo.transform, "Muro_Este", new Vector3(x + grosor * 0.5f, alto * 0.5f, z * 0.5f),
             new Vector3(grosor, alto, z + grosor * 2f));
        Muro(grupo.transform, "Muro_Sur", new Vector3(x * 0.5f, alto * 0.5f, -grosor * 0.5f),
             new Vector3(x + grosor * 2f, alto, grosor));
        Muro(grupo.transform, "Muro_Norte", new Vector3(x * 0.5f, alto * 0.5f, z + grosor * 0.5f),
             new Vector3(x + grosor * 2f, alto, grosor));
    }

    static void Muro(Transform padre, string nombre, Vector3 pos, Vector3 tam)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        BoxCollider c = go.AddComponent<BoxCollider>();
        c.size = tam;
    }

    // ======================================================================
    //  ARBOLES DE RESPALDO (si no pones los tuyos de la Asset Store)
    // ======================================================================
    public static GameObject[] CrearPrefabsArboles()
    {
        Material corteza = NuevoMaterial("Mat_Corteza", new Color(0.45f, 0.33f, 0.21f), 0f, 0.08f,
                                         Tex("arbol_corteza_albedo"), Tex("arbol_corteza_normal"), 2f);
        Material hoja = NuevoMaterial("Mat_Hoja", new Color(0.30f, 0.46f, 0.16f), 0f, 0.12f,
                                      Tex("arbol_hoja_albedo"), null, 3f);
        Material hojaOscura = NuevoMaterial("Mat_HojaOscura", new Color(0.17f, 0.32f, 0.12f), 0f, 0.1f,
                                            Tex("arbol_hoja_albedo"), null, 3f);

        GameObject[] prefabs = new GameObject[3];
        prefabs[0] = GuardarPrefab(ArbolFrondoso(corteza, hoja), "Arbol_1_Frondoso");
        prefabs[1] = GuardarPrefab(ArbolConifera(corteza, hojaOscura), "Arbol_2_Conifera");
        prefabs[2] = GuardarPrefab(ArbolPinoAlto(corteza, hojaOscura), "Arbol_3_PinoAlto");
        return prefabs;
    }

    static GameObject ArbolFrondoso(Material corteza, Material hoja)
    {
        GameObject raiz = new GameObject("Arbol_1_Frondoso");

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(raiz.transform, false);
        tronco.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        tronco.transform.localScale = new Vector3(0.55f, 2.4f, 0.55f);
        Limpiar(tronco, corteza);

        Vector3[] copas =
        {
            new Vector3(0f, 6.2f, 0f),
            new Vector3(1.5f, 5.4f, 0.6f),
            new Vector3(-1.3f, 5.6f, -0.9f),
            new Vector3(0.3f, 7.3f, -1.1f)
        };
        float[] tam = { 4.4f, 3.0f, 2.8f, 2.6f };

        for (int i = 0; i < copas.Length; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.SetParent(raiz.transform, false);
            c.transform.localPosition = copas[i];
            c.transform.localScale = new Vector3(tam[i], tam[i] * 0.85f, tam[i]);
            Limpiar(c, hoja);
        }

        AnadirColisionTronco(raiz, 0.5f, 5f);
        return raiz;
    }

    static GameObject ArbolConifera(Material corteza, Material hoja)
    {
        GameObject raiz = new GameObject("Arbol_2_Conifera");

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(raiz.transform, false);
        tronco.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        tronco.transform.localScale = new Vector3(0.38f, 1.8f, 0.38f);
        Limpiar(tronco, corteza);

        float[] alturas = { 2.6f, 4.6f, 6.4f, 7.9f };
        float[] radios = { 2.4f, 1.9f, 1.4f, 0.85f };

        for (int i = 0; i < alturas.Length; i++)
        {
            GameObject c = new GameObject("Copa" + i);
            c.transform.SetParent(raiz.transform, false);
            c.transform.localPosition = new Vector3(0f, alturas[i], 0f);
            c.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // cono hacia arriba
            Mesh m = CrearCono(radios[i], 2.6f, 14);
            c.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(m, "Cono_Conifera_" + i);
            c.AddComponent<MeshRenderer>().sharedMaterial = hoja;
        }

        AnadirColisionTronco(raiz, 0.4f, 5f);
        return raiz;
    }

    static GameObject ArbolPinoAlto(Material corteza, Material hoja)
    {
        GameObject raiz = new GameObject("Arbol_3_PinoAlto");

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(raiz.transform, false);
        tronco.transform.localPosition = new Vector3(0f, 4.2f, 0f);
        tronco.transform.localScale = new Vector3(0.32f, 4.2f, 0.32f);
        Limpiar(tronco, corteza);

        float[] alturas = { 6.6f, 8.2f, 9.5f };
        float[] radios = { 2.1f, 1.5f, 0.9f };

        for (int i = 0; i < alturas.Length; i++)
        {
            GameObject c = new GameObject("Copa" + i);
            c.transform.SetParent(raiz.transform, false);
            c.transform.localPosition = new Vector3(0f, alturas[i], 0f);
            c.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Mesh m = CrearCono(radios[i], 2.4f, 14);
            c.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(m, "Cono_Pino_" + i);
            c.AddComponent<MeshRenderer>().sharedMaterial = hoja;
        }

        AnadirColisionTronco(raiz, 0.35f, 7f);
        return raiz;
    }

    static void AnadirColisionTronco(GameObject raiz, float radio, float altura)
    {
        CapsuleCollider c = raiz.AddComponent<CapsuleCollider>();
        c.radius = radio;
        c.height = altura;
        c.center = new Vector3(0f, altura * 0.5f, 0f);
    }

    static Mesh GuardarMalla(Mesh m, string nombre)
    {
        string ruta = RUTA_GEN + nombre + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static GameObject GuardarPrefab(GameObject go, string nombre)
    {
        string ruta = RUTA_GEN + nombre + ".prefab";
        AssetDatabase.DeleteAsset(ruta);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ======================================================================
    //  UTILIDADES
    // ======================================================================
    /// <summary>Crea un cubo con material, y le quita el collider si no hace falta.</summary>
    static GameObject Cubo(string nombre, Transform padre, Vector3 posLocal, Vector3 escala,
                           Material mat, bool conCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!conCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }

    /// <summary>Quita el collider de un primitivo y le pone material.</summary>
    static void Limpiar(GameObject go, Material mat)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        MeshRenderer r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;
    }
}
