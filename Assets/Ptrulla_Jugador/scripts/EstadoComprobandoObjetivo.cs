using UnityEngine;

public class EstadoComprobandoObjetivo : EstadoIA
{
    public float distanciaParaVerBotin = 3f;
    private bool seDioCuenta = false; 

    public override void AlEntrar()
    {
        base.AlEntrar();
        if (cerebro.puntoObjetivo != null)
        {
            // Corremos hacia la sala del tesoro
            movimiento.MoverA(cerebro.puntoObjetivo.position, movimiento.velocidadPersecucion);
        }
    }
    void Update()
    {
        if (cerebro.puntoObjetivo == null) return;

        float dist = Vector3.Distance(transform.position, cerebro.puntoObjetivo.position);
        
        if (dist <= distanciaParaVerBotin && !seDioCuenta)
        {
            if (RecogerObjetivo.tieneElBotin) 
            {
                seDioCuenta = true;
                Debug.Log("🧐 ¡El guardia ha visto el pedestal vacío! Dando la alarma...");
                
                // Le decimos a nuestra radio que avise a los demás
                cerebro.capaSocial.DarAlarmaRobo(); 
            }
            else
            {
                // Si ha llegado y el botín SÍ está, se da la vuelta y sigue patrullando
                if (movimiento.HaLlegadoAlDestino())
                {
                    cerebro.fsm.CambiarEstado(cerebro.fsm.patrulla);
                }
            }
        }
    }

}