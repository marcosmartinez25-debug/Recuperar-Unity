using UnityEngine;

/// <summary>
/// Guarda el estado de la partida: ahora mismo solo si el jugador lleva la llave.
/// Esta en un unico objeto de la escena ("GestorJuego") y cualquier otro script
/// puede consultarlo con EstadoJuego.Instancia.
/// El grafo de Visual Scripting tambien lee desde aqui el valor de "tieneLlave".
/// </summary>
public class EstadoJuego : MonoBehaviour
{
    // Referencia unica (patron Singleton). Asi otros scripts no necesitan arrastrar
    // el objeto en el Inspector: les basta con EstadoJuego.Instancia.
    public static EstadoJuego Instancia;

    [Header("Estado del jugador")]
    [Tooltip("Se pone en true cuando el jugador recoge la llave. " +
             "Es publico para que Visual Scripting pueda leerlo con Get Member.")]
    public bool tieneLlave = false;

    [Header("Objetivos del recorrido")]
    [Tooltip("Objeto llave. Lo usa la flecha guia como primer destino.")]
    public Transform objetivoLlave;

    [Tooltip("Cofre o puerta final. Es el segundo destino de la flecha guia.")]
    public Transform objetivoCofre;

    // Awake se ejecuta antes que Start, al cargar la escena.
    private void Awake()
    {
        // Si ya existia otro gestor, este sobra y se destruye (evita duplicados).
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    private void Start()
    {
        // Al empezar la partida el jugador nunca tiene la llave.
        tieneLlave = false;

        // Dejamos la interfaz en su estado inicial.
        if (InterfazJuego.Instancia != null)
        {
            InterfazJuego.Instancia.ActualizarIndicadorLlave(false);
            InterfazJuego.Instancia.MostrarAviso("Sigue la flecha para encontrar la llave", 4f);
        }
    }

    /// <summary>
    /// Marca la llave como recogida y avisa a la interfaz.
    /// La llama el script RecogerLlave cuando el jugador toca la llave.
    /// </summary>
    public void GuardarLlave()
    {
        tieneLlave = true;

        if (InterfazJuego.Instancia != null)
        {
            InterfazJuego.Instancia.ActualizarIndicadorLlave(true);
            InterfazJuego.Instancia.MostrarAviso("Has recogido la llave. Cruza el rio hacia la montana", 4f);
        }
    }

    /// <summary>
    /// Devuelve el objetivo al que deberia apuntar la flecha guia en este momento.
    /// Se puede llamar tambien desde Visual Scripting.
    /// </summary>
    public Transform ObjetivoActual()
    {
        return tieneLlave ? objetivoCofre : objetivoLlave;
    }
}
