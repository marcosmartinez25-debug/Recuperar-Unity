using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla todo lo que se ve en pantalla (UI):
///  - un aviso temporal arriba ("Has recogido la llave", "Esta cerrado"...)
///  - un indicador fijo que dice si llevas la llave
///  - el panel de VICTORIA del final
/// El generador de la escena rellena estas referencias automaticamente.
/// </summary>
public class InterfazJuego : MonoBehaviour
{
    public static InterfazJuego Instancia;

    [Header("Referencias de la interfaz")]
    [Tooltip("Texto de avisos temporales (parte superior de la pantalla).")]
    public Text textoAviso;

    [Tooltip("Texto fijo que indica si el jugador lleva la llave.")]
    public Text textoLlave;

    [Tooltip("Panel que aparece al abrir el cofre con el mensaje de victoria.")]
    public GameObject panelVictoria;

    // Guardamos la corrutina del aviso para poder cortarla si llega otro aviso nuevo.
    private Coroutine corrutinaAviso;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        // El panel de victoria arranca oculto.
        if (panelVictoria != null)
            panelVictoria.SetActive(false);

        if (textoAviso != null)
            textoAviso.text = "";
    }

    /// <summary>
    /// Muestra un mensaje durante unos segundos y luego lo borra.
    /// </summary>
    /// <param name="mensaje">Texto que se vera en pantalla.</param>
    /// <param name="segundos">Cuanto tiempo se queda visible.</param>
    public void MostrarAviso(string mensaje, float segundos = 2.5f)
    {
        if (textoAviso == null) return;

        // Si habia un aviso anterior en marcha, lo paramos para que no se pisen.
        if (corrutinaAviso != null)
            StopCoroutine(corrutinaAviso);

        corrutinaAviso = StartCoroutine(RutinaAviso(mensaje, segundos));
    }

    // Corrutina: escribe el texto, espera y lo borra.
    private IEnumerator RutinaAviso(string mensaje, float segundos)
    {
        textoAviso.text = mensaje;
        yield return new WaitForSeconds(segundos);
        textoAviso.text = "";
        corrutinaAviso = null;
    }

    /// <summary>
    /// Cambia el indicador de la llave (arriba a la izquierda).
    /// </summary>
    public void ActualizarIndicadorLlave(bool tieneLlave)
    {
        if (textoLlave == null) return;

        textoLlave.text = tieneLlave ? "LLAVE: SI" : "LLAVE: NO";
        textoLlave.color = tieneLlave ? new Color(1f, 0.85f, 0.2f) : new Color(0.85f, 0.85f, 0.85f);
    }

    /// <summary>
    /// Ensena el panel de victoria y libera el raton para poder cerrar el juego.
    /// </summary>
    public void MostrarVictoria()
    {
        if (panelVictoria != null)
            panelVictoria.SetActive(true);

        if (textoAviso != null)
            textoAviso.text = "";

        // Soltamos el cursor (el controlador de tercera persona lo bloquea al jugar).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
