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
        // Si lo vemos directamente, vamos a su posición real
        if (cerebro.objetivoDetectado && jugador != null)
        {
            movimiento.Perseguir(jugador.position);
        }
        // Si no lo vemos, seguimos las coordenadas GPS del líder
        else if (cerebro.coordenadaTactica != Vector3.zero)
        {
            movimiento.Perseguir(cerebro.coordenadaTactica);
        }
    }
}