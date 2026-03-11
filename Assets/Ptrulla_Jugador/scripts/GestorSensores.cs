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

    public Transform TransformJugador { get; private set; }
    private bool estabaDetectadoPreviamente = false;

    void Awake()
    {
        vision = GetComponent<SensorVision>();
        oido = GetComponent<SensorOido>();
    }

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) TransformJugador = go.transform;
    }

    void Update()
    {
        if (TransformJugador == null) return;

        bool loVeo = vision.EvaluarVision(TransformJugador);
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
