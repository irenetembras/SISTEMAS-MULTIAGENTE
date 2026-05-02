using UnityEngine;
using System;

[RequireComponent(typeof(SensorVision))]
[RequireComponent(typeof(SensorOido))]
public class GestorSensores : MonoBehaviour
{
    public event Action<Vector3> OnJugadorDetectado;
    public event Action OnJugadorPerdido;

    private SensorVision vision;
    private SensorOido oido;

    public Transform TransformJugador { get; private set; }
    public bool EnContactoConJugador { get; private set; } = false;
    public bool LoVeo { get; private set; } = false;

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

        Transform jugadorVisto = vision.EvaluarVision();
        LoVeo = (jugadorVisto != null);

        bool loOigo = oido.EvaluarOido(TransformJugador);

        bool detectadoAhora = LoVeo || loOigo;

        // Solo lanzamos eventos cuando cambia el estado de detección
        if (detectadoAhora && !EnContactoConJugador)
        {
            EnContactoConJugador = true;
            OnJugadorDetectado?.Invoke(TransformJugador.position);
        }
        else if (!detectadoAhora && EnContactoConJugador)
        {
            EnContactoConJugador = false;
            OnJugadorPerdido?.Invoke();
        }
    }
}
