using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// Puente entre el codigo C# y el grafo de Visual Scripting.
///
/// Copia el valor de EstadoJuego.tieneLlave a una VARIABLE DE ESCENA llamada
/// "tieneLlave", para que el grafo de la flecha guia pueda leerla con un nodo
/// "Get Variable (Scene)" y decidir a donde apunta.
///
/// NOTA: si tu proyecto no tiene instalado el paquete Visual Scripting, este
/// archivo dara error al compilar. En ese caso puedes borrarlo: el grafo tambien
/// puede leer el booleano directamente del componente EstadoJuego
/// (nodos Get Component + Get Member), como explico en la guia del grafo.
/// </summary>
public class PuenteVisualScripting : MonoBehaviour
{
    [Tooltip("Nombre de la variable de escena que lee el grafo de la flecha.")]
    public string nombreVariable = "tieneLlave";

    // Si falla una vez (porque la variable no existe), dejamos de intentarlo
    // para no llenar la consola de errores.
    private bool desactivado = false;

    private void Update()
    {
        if (desactivado) return;
        if (EstadoJuego.Instancia == null) return;

        try
        {
            // Solo escribimos si la variable existe ya en la escena.
            if (Variables.ActiveScene.IsDefined(nombreVariable))
            {
                Variables.ActiveScene.Set(nombreVariable, EstadoJuego.Instancia.tieneLlave);
            }
            else
            {
                desactivado = true;
                Debug.LogWarning("[PuenteVisualScripting] No existe la variable de escena '" +
                                 nombreVariable + "'. Creala en la ventana Variables " +
                                 "(Scene) o deja que el grafo lea EstadoJuego directamente.");
            }
        }
        catch (System.Exception e)
        {
            desactivado = true;
            Debug.LogWarning("[PuenteVisualScripting] No se ha podido escribir la variable: " + e.Message);
        }
    }
}
