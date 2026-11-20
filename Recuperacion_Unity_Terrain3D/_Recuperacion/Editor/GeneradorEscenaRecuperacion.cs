using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ventana del editor que monta la escena entera de la actividad de recuperacion.
/// Menu:  Recuperacion > Abrir generador de escena
///
/// Lo que hace al pulsar el boton:
///   1. Crea el Terrain y lo esculpe (pradera + rio + montana nevada).
///   2. Crea las Terrain Layers con su Normal Map y pinta el escenario.
///   3. Planta arboles y hierba, y coloca rocas y troncos.
///   4. Pone el agua del rio y el puente para cruzarlo.
///   5. Coloca la llave, el cofre final y la flecha guia.
///   6. Crea la interfaz (avisos, indicador de llave y panel de victoria).
///   7. Ajusta sol, cielo y niebla, y cierra los bordes del mapa.
/// </summary>
public class GeneradorEscenaRecuperacion : EditorWindow
{
    const string NOMBRE_RAIZ = "ESCENA_RECUPERACION";

    // Prefabs de arbol. Si los dejas vacios, el generador crea tres de respaldo.
    GameObject arbol1, arbol2, arbol3;
    bool usarArbolesPropios = false;

    [MenuItem("Recuperacion/Abrir generador de escena", false, 0)]
    public static void Abrir()
    {
        GeneradorEscenaRecuperacion v = GetWindow<GeneradorEscenaRecuperacion>("Generador");
        v.minSize = new Vector2(420f, 430f);
        v.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Recuperacion - Creacion de entornos con Terrain",
                                   EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Monta la escena completa: terreno esculpido, texturas con Normal Map, " +
            "vegetacion, rio con puente, llave, cofre, interfaz y ambiente.\n\n" +
            "Despues solo te queda: poner el personaje de tercera persona en " +
            "'PuntoDeInicio' y montar el grafo de Visual Scripting de la flecha.",
            MessageType.Info);

        EditorGUILayout.Space(10);
        usarArbolesPropios = EditorGUILayout.ToggleLeft(
            "Usar mis propios prefabs de arbol (recomendado: Asset Store)", usarArbolesPropios);

        if (usarArbolesPropios)
        {
            EditorGUILayout.HelpBox(
                "Arrastra 3 prefabs de arbol DISTINTOS (especies diferentes, " +
                "no el mismo cambiado de color o de tamano).", MessageType.None);
            arbol1 = (GameObject)EditorGUILayout.ObjectField("Arbol 1", arbol1, typeof(GameObject), false);
            arbol2 = (GameObject)EditorGUILayout.ObjectField("Arbol 2", arbol2, typeof(GameObject), false);
            arbol3 = (GameObject)EditorGUILayout.ObjectField("Arbol 3", arbol3, typeof(GameObject), false);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Se usaran tres arboles de respaldo generados automaticamente. " +
                "Funcionan, pero para la nota de 'estilo realista' es mejor importar " +
                "arboles gratuitos de la Asset Store y marcar la casilla de arriba.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(16);

        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("GENERAR ESCENA COMPLETA", GUILayout.Height(44)))
        {
            GameObject[] prefabs = usarArbolesPropios
                ? new[] { arbol1, arbol2, arbol3 }
                : null;
            Generar(prefabs);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.95f, 0.65f, 0.6f);
        if (GUILayout.Button("Borrar lo generado", GUILayout.Height(26)))
            Borrar();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(14);
        EditorGUILayout.LabelField("Despues de generar:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Arrastra el PlayerArmature a 'PuntoDeInicio'.");
        EditorGUILayout.LabelField("2. Mete 'FlechaGuia' dentro del personaje (altura 2.5).");
        EditorGUILayout.LabelField("3. Monta el grafo de la flecha (ver la guia en .md).");
        EditorGUILayout.LabelField("4. Guarda la escena (Ctrl+S).");
    }

    // ======================================================================
    //  GENERACION
    // ======================================================================
    [MenuItem("Recuperacion/Generar escena completa (rapido)", false, 1)]
    public static void GenerarRapido()
    {
        Generar(null);
    }

    public static void Generar(GameObject[] prefabsArboles)
    {
        try
        {
            Borrar();
            GeneradorTerreno.PrepararCarpetas();   // las carpetas antes de crear assets

            GameObject raiz = new GameObject(NOMBRE_RAIZ);

            // --- Ambiente: sol, cielo y niebla ----------------------------
            EditorUtility.DisplayProgressBar("Generando escena", "Ajustando sol, cielo y niebla...", 0.02f);
            Light sol = GeneradorObjetos.ConfigurarAmbiente();
            if (sol != null && sol.transform.parent == null)
                sol.transform.SetParent(raiz.transform, true);

            // --- Arboles: los tuyos o los de respaldo ---------------------
            GameObject[] arboles = prefabsArboles;
            bool hayArboles = false;
            if (arboles != null)
                foreach (GameObject g in arboles)
                    if (g != null) hayArboles = true;

            if (!hayArboles)
            {
                EditorUtility.DisplayProgressBar("Generando escena", "Creando arboles de respaldo...", 0.05f);
                arboles = GeneradorObjetos.CrearPrefabsArboles();
            }

            // --- Terreno ---------------------------------------------------
            Terrain terreno = GeneradorTerreno.Crear(raiz.transform, arboles);

            // --- Rio, puente, rocas ---------------------------------------
            EditorUtility.DisplayProgressBar("Generando escena", "Creando el rio y el puente...", 0.82f);
            GeneradorObjetos.CrearRio(raiz.transform);
            GeneradorObjetos.CrearPuente(raiz.transform, terreno);
            GeneradorObjetos.CrearRocasYTroncos(raiz.transform, terreno);

            // --- Objetos del recorrido ------------------------------------
            EditorUtility.DisplayProgressBar("Generando escena", "Colocando llave y cofre...", 0.9f);
            Vector2 posLlave = new Vector2(214f, 193f);
            Vector2 posCofre = new Vector2(334f, 428f);

            GameObject llave = GeneradorObjetos.CrearLlave(raiz.transform, terreno, posLlave);
            GameObject cofre = GeneradorObjetos.CrearCofre(raiz.transform, terreno, posCofre);

            // --- Punto de inicio del jugador -------------------------------
            Vector2 inicio = ConfigRecuperacion.PuntosCamino()[0];
            float yInicio = terreno.SampleHeight(new Vector3(inicio.x, 0f, inicio.y));
            GameObject puntoInicio = new GameObject("PuntoDeInicio");
            puntoInicio.transform.SetParent(raiz.transform, false);
            puntoInicio.transform.position = new Vector3(inicio.x, yInicio + 0.2f, inicio.y);
            // Que mire hacia el siguiente punto del camino.
            Vector2 siguiente = ConfigRecuperacion.PuntosCamino()[1];
            puntoInicio.transform.rotation = Quaternion.LookRotation(
                new Vector3(siguiente.x - inicio.x, 0f, siguiente.y - inicio.y));

            // --- Flecha guia -----------------------------------------------
            GeneradorObjetos.CrearFlecha(
                raiz.transform, puntoInicio.transform.position + Vector3.up * 3f);

            // --- Interfaz ---------------------------------------------------
            EditorUtility.DisplayProgressBar("Generando escena", "Montando la interfaz...", 0.95f);
            GeneradorObjetos.CrearInterfaz(raiz.transform);

            // --- Gestor del juego -------------------------------------------
            GameObject gestor = new GameObject("GestorJuego");
            gestor.transform.SetParent(raiz.transform, false);
            EstadoJuego estado = gestor.AddComponent<EstadoJuego>();
            estado.objetivoLlave = llave.transform;
            estado.objetivoCofre = cofre.transform;
            gestor.AddComponent<PuenteVisualScripting>();

            // --- Limites del mapa --------------------------------------------
            GeneradorObjetos.CrearLimites(raiz.transform);

            // --- Camara temporal, por si aun no hay personaje ----------------
            if (Camera.main == null)
            {
                GameObject cam = new GameObject("Camara_Temporal");
                cam.transform.SetParent(raiz.transform, false);
                cam.transform.position = puntoInicio.transform.position + new Vector3(0f, 4f, -8f);
                cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                cam.AddComponent<Camera>();
                cam.AddComponent<AudioListener>();
                cam.tag = "MainCamera";
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();

            Selection.activeGameObject = puntoInicio;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log("<b>[Recuperacion] Escena generada.</b> Siguiente paso: " +
                      "pon el personaje de tercera persona en 'PuntoDeInicio', " +
                      "mete 'FlechaGuia' dentro del personaje y monta el grafo de Visual Scripting.");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[Recuperacion] Ha fallado la generacion: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ======================================================================
    //  BORRADO
    // ======================================================================
    [MenuItem("Recuperacion/Borrar lo generado", false, 20)]
    public static void Borrar()
    {
        GameObject anterior = GameObject.Find(NOMBRE_RAIZ);
        while (anterior != null)
        {
            Object.DestroyImmediate(anterior);
            anterior = GameObject.Find(NOMBRE_RAIZ);
        }

        // Si quedo un Terrain suelto de una generacion anterior, fuera tambien.
        GameObject sueltoTerreno = GameObject.Find("Terreno");
        if (sueltoTerreno != null && sueltoTerreno.GetComponent<Terrain>() != null)
            Object.DestroyImmediate(sueltoTerreno);
    }
}
