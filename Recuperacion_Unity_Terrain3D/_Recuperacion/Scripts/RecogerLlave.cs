using UnityEngine;

/// <summary>
/// Va en el objeto LLAVE. Cuando el jugador entra en su collider (marcado como
/// Is Trigger), la llave desaparece y se guarda en el estado del jugador.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RecogerLlave : MonoBehaviour
{
    [Header("Opciones")]
    [Tooltip("Objeto hijo con el aspecto de la llave. Se apaga al recogerla.")]
    public GameObject parteVisible;

    [Tooltip("Sonido opcional al recoger la llave.")]
    public AudioClip sonidoRecoger;

    // Para no recoger la llave dos veces si el collider dispara varias veces.
    private bool yaRecogida = false;

    private void Reset()
    {
        // Al anadir el script en el editor dejamos el collider ya como trigger.
        GetComponent<Collider>().isTrigger = true;
    }

    // Unity llama a este metodo cuando otro collider entra en el trigger.
    private void OnTriggerEnter(Collider otro)
    {
        if (yaRecogida) return;
        if (!EsElJugador(otro)) return;

        yaRecogida = true;

        // Guardamos la llave en el estado de la partida.
        if (EstadoJuego.Instancia != null)
            EstadoJuego.Instancia.GuardarLlave();

        // Sonido opcional (se reproduce en la posicion de la llave).
        if (sonidoRecoger != null)
            AudioSource.PlayClipAtPoint(sonidoRecoger, transform.position);

        // La llave desaparece del escenario.
        // Apagamos el objeto entero: asi la flecha guia puede seguir leyendo su
        // posicion sin dar error, pero el jugador ya no la ve.
        if (parteVisible != null)
            parteVisible.SetActive(false);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Comprueba si el collider que ha entrado es el jugador.
    /// Miramos la etiqueta "Player" y, por si acaso no esta puesta, tambien si
    /// el objeto tiene un CharacterController (lo lleva el controlador de Unity).
    /// </summary>
    private bool EsElJugador(Collider otro)
    {
        if (otro.CompareTag("Player")) return true;
        if (otro.GetComponentInParent<CharacterController>() != null) return true;
        return false;
    }
}
