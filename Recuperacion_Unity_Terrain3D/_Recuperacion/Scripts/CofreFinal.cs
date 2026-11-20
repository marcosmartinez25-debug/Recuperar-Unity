using System.Collections;
using UnityEngine;

/// <summary>
/// Va en el COFRE (o puerta) del final del recorrido, en la zona de montana.
/// - Si el jugador se acerca SIN la llave: sale un mensaje de que esta cerrado.
/// - Si se acerca CON la llave: la tapa gira, se abre y aparece la victoria.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CofreFinal : MonoBehaviour
{
    [Header("Partes del cofre")]
    [Tooltip("Tapa del cofre (o hoja de la puerta). Es la que gira al abrirse.")]
    public Transform tapa;

    [Header("Ajustes de apertura")]
    [Tooltip("Grados que gira la tapa sobre el eje X al abrirse.")]
    public float anguloAbierto = -100f;

    [Tooltip("Velocidad del giro en grados por segundo.")]
    public float velocidadApertura = 110f;

    [Tooltip("Segundos de espera entre abrir el cofre y ensenar la victoria.")]
    public float retardoVictoria = 0.8f;

    [Header("Mensajes")]
    public string mensajeCerrado = "Esta cerrado. Necesitas la llave";
    public string mensajeAbierto = "Has abierto el cofre";

    // Estado interno
    private bool abierto = false;          // el cofre ya se ha abierto
    private bool jugadorDentro = false;    // el jugador esta en la zona del trigger

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsElJugador(otro)) return;
        jugadorDentro = true;
        Comprobar();
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsElJugador(otro)) return;
        jugadorDentro = false;
    }

    /// <summary>
    /// Decide que pasa cuando el jugador llega al cofre.
    /// </summary>
    private void Comprobar()
    {
        if (abierto) return;   // ya estaba abierto, no hacemos nada

        bool llevaLlave = EstadoJuego.Instancia != null && EstadoJuego.Instancia.tieneLlave;

        if (!llevaLlave)
        {
            // CASO 1: no tiene la llave -> aviso de cerrado.
            if (InterfazJuego.Instancia != null)
                InterfazJuego.Instancia.MostrarAviso(mensajeCerrado, 3f);
            return;
        }

        // CASO 2: tiene la llave -> abrimos.
        abierto = true;

        if (InterfazJuego.Instancia != null)
            InterfazJuego.Instancia.MostrarAviso(mensajeAbierto, 2f);

        StartCoroutine(AbrirYGanar());
    }

    /// <summary>
    /// Corrutina que gira la tapa poco a poco y luego muestra la victoria.
    /// </summary>
    private IEnumerator AbrirYGanar()
    {
        if (tapa != null)
        {
            // Angulo de partida y angulo final de la tapa.
            Quaternion giroInicial = tapa.localRotation;
            Quaternion giroFinal = giroInicial * Quaternion.Euler(anguloAbierto, 0f, 0f);

            // Cuanto hay que girar en total, para saber cuanto tiempo tardamos.
            float gradosTotales = Mathf.Abs(anguloAbierto);
            float duracion = gradosTotales / Mathf.Max(velocidadApertura, 1f);
            float t = 0f;

            while (t < duracion)
            {
                t += Time.deltaTime;
                // Slerp va interpolando el giro de forma suave entre inicio y final.
                tapa.localRotation = Quaternion.Slerp(giroInicial, giroFinal, t / duracion);
                yield return null;   // espera al siguiente frame
            }

            tapa.localRotation = giroFinal;
        }

        yield return new WaitForSeconds(retardoVictoria);

        // Mensaje de VICTORIA en pantalla.
        if (InterfazJuego.Instancia != null)
            InterfazJuego.Instancia.MostrarVictoria();
    }

    private bool EsElJugador(Collider otro)
    {
        if (otro.CompareTag("Player")) return true;
        if (otro.GetComponentInParent<CharacterController>() != null) return true;
        return false;
    }

    // Por si el jugador entra justo en el mismo frame en que recoge la llave:
    // si sigue dentro de la zona y ya tiene la llave, se abre igualmente.
    private void Update()
    {
        if (!abierto && jugadorDentro &&
            EstadoJuego.Instancia != null && EstadoJuego.Instancia.tieneLlave)
        {
            Comprobar();
        }
    }
}
