using UnityEngine;

/// <summary>
/// Hace que un objeto gire sobre si mismo y suba y baje un poco.
/// Lo uso en la llave para que se vea bien y llame la atencion.
/// </summary>
public class GiroObjeto : MonoBehaviour
{
    [Tooltip("Grados que gira por segundo sobre el eje Y.")]
    public float velocidadGiro = 70f;

    [Tooltip("Cuanto sube y baja (en metros). 0 = no se mueve.")]
    public float alturaFlotacion = 0.25f;

    [Tooltip("Velocidad del vaiven de subir y bajar.")]
    public float velocidadFlotacion = 1.6f;

    // Posicion donde empieza el objeto: el vaiven se calcula a partir de aqui.
    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.position;
    }

    private void Update()
    {
        // Giro continuo. Time.deltaTime hace que vaya igual de rapido
        // aunque el ordenador tenga mas o menos FPS.
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);

        // Vaiven arriba/abajo con un seno.
        if (alturaFlotacion > 0f)
        {
            float desplazamiento = Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotacion;
            transform.position = posicionInicial + Vector3.up * desplazamiento;
        }
    }
}
