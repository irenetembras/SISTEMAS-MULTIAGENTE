using UnityEngine;
using System;

[RequireComponent(typeof(SensorVision))]
[RequireComponent(typeof(SensorOido))]
public class GestorSensores : MonoBehaviour
{
    // EVENTOS: Los "megáfonos" que gritan al resto del juego
    public event Action<Vector3> OnJugadorDetectado;
    public event Action OnJugadorPerdido;

    private SensorVision vision;
    private SensorOido oido;

    // Mantenemos esta variable pública para que el oído y el Cerebro puedan leerla
    public Transform TransformJugador { get; private set; }
    private bool estabaDetectadoPreviamente = false;

    void Awake()
    {
        vision = GetComponent<SensorVision>();
        oido = GetComponent<SensorOido>();
    }

    void Start()
    {
        // Buscamos al jugador una vez para tener su referencia
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) TransformJugador = go.transform;
    }

    void Update()
    {
        if (TransformJugador == null) return;

        // VISIÓN "CIEGA": Ya no le pasamos el TransformJugador. Busca solo.
        Transform jugadorVisto = vision.EvaluarVision();
        bool loVeo = (jugadorVisto != null);
        
        // EL OÍDO: Sigue usando el Transform como lo tenías
        bool loOigo = oido.EvaluarOido(TransformJugador);
        
        bool detectadoAhora = loVeo || loOigo;

        // SISTEMA DE EVENTOS PUSH: Solo hablamos si hay un CAMBIO en el mundo
        if (detectadoAhora && !estabaDetectadoPreviamente)
        {
            estabaDetectadoPreviamente = true;
            OnJugadorDetectado?.Invoke(TransformJugador.position); // ¡Gritamos que lo hemos visto!
        }
        else if (!detectadoAhora && estabaDetectadoPreviamente)
        {
            estabaDetectadoPreviamente = false;
            OnJugadorPerdido?.Invoke(); // ¡Gritamos que lo hemos perdido!
        }
    }
}