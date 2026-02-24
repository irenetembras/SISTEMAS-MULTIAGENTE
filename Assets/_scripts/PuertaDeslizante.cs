using System.Collections;
using UnityEngine;
using UnityEngine.AI; 
using Unity.AI.Navigation; 

public class PuertaCorredera : MonoBehaviour
{
    [Header("Configuración de IA")]
    public NavMeshLink conector;
   
    [Header("Configuración")]
    public float distanciaDesplazamiento = 3.0f; // Cuántos metros se mueve
    public float velocidad = 2.0f; // Velocidad del deslizamiento
    public float tiempoEspera = 3.0f; // Tiempo entre abrir/cerrar
    
    // Elige hacia dónde se mueve:
    // (1, 0, 0) = Derecha (Eje Rojo) 
    // (0, 1, 0) = Arriba (Eje Verde) 
    // (0, 0, 1) = Adelante (Eje Azul) 
    public Vector3 direccionMover = new Vector3(1, 0, 0); 

    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;
    public bool estaCerrado = true;

    void TogglePuerta()
    {
        estaCerrado = !estaCerrado;

        if (conector != null)
        {
            // OJO AQUÍ: Si "estaCerrado" es TRUE, el link debe ser FALSE (apagado)
            // Por eso le ponemos el símbolo "!" delante, que significa "lo contrario"
            conector.enabled = !estaCerrado; 
        }
    }
    void Start()
    {
        // 1. Guardamos dónde está el muro ahora mismo (Cerrado)
        posicionCerrada = transform.position;

        // 2. Calculamos dónde acabará (Posición actual + Dirección * Distancia)
        // Usamos "transform.TransformDirection" para que si giras el muro en el editor,
        // la dirección "Derecha" siga siendo LA SUYA, no la del mundo.
        Vector3 movimientoReal = transform.TransformDirection(direccionMover) * distanciaDesplazamiento;
        posicionAbierta = posicionCerrada + movimientoReal;

        // 3. Arrancamos el reloj
        StartCoroutine(Reloj());
    }

    void Update()
    {
        // Decidimos la meta
        Vector3 objetivo = estaCerrado ? posicionCerrada : posicionAbierta;

        // Moverse hacia la meta 
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);
    }

    IEnumerator Reloj()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEspera);
            TogglePuerta();;
        }
    }
}