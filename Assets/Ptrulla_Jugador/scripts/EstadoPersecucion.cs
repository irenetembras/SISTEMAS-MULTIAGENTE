using UnityEngine;

public class EstadoPersecucion : EstadoIA
{
    private Transform jugador; 

    public override void Configurar(IACerebro c, IAMovimiento m)
    {
        base.Configurar(c, m);
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) jugador = go.transform;
    }

    void Update()
    {
        // 1. Si lo veo con mis propios ojos, voy a su posición exacta real
        if (cerebro.objetivoDetectado && jugador != null)
        {
            movimiento.Perseguir(jugador.position);
        }
        // 2. Si NO lo veo, pero mi rol es perseguir, sigo el GPS que me manda el líder
        else if (cerebro.coordenadaTactica != Vector3.zero)
        {
            movimiento.Perseguir(cerebro.coordenadaTactica);
        }
    }
}