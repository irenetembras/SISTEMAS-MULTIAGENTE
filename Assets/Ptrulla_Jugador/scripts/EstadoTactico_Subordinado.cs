using UnityEngine;

// Estado: el guardia ejecuta el rol asignado por el Comandante.
// Escucha actualizaciones de GPS y órdenes de repliegue.
public class EstadoTactico_Subordinado : EstadoTacticoBase
{
    public override void AlEntrar()
    {
        base.AlEntrar();
    }

    public override void ProcesarMensaje(MensajeFIPA mensaje)
    {
        switch (mensaje.performativa)
        {
            case PerformativaFIPA.INFORM:
                ProcesarInform(mensaje);
                break;

            case PerformativaFIPA.REJECT_PROPOSAL:
                Debug.Log($"[SUBORDINADO {gameObject.name}] Escuadron disuelto por {mensaje.emisor.name}. Volviendo a patrulla.");
                cerebro.capaSocial.miRolAsignado = RolTactico.PatrullaNormal;
                fsmTactica.CambiarEstado(fsmTactica.libre);
                break;

            case PerformativaFIPA.CFP:
                Debug.Log($"[SUBORDINADO {gameObject.name}] CFP ignorado de {mensaje.emisor.name}. Estoy ocupado con rol: {cerebro.capaSocial.miRolAsignado}");
                break;
        }
    }

    private void ProcesarInform(MensajeFIPA inform)
    {
        if (inform.contenido == "ALARMA_ROBO")
        {
            Debug.Log($"[SUBORDINADO {gameObject.name}] ALARMA DE ROBO recibida. Cambiando a BloqueoSalida.");
            cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
            return;
        }

        // Actualización GPS del Vigía: "x|y|z"
        if (inform.contenido.Contains('|'))
        {
            string[] p = inform.contenido.Split('|');
            Vector3 nuevoPunto = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );
            Debug.Log($"[SUBORDINADO {gameObject.name}] Actualizacion GPS recibida. Nuevo destino: {nuevoPunto}");
            GetComponent<IAMovimiento>().MoverA(nuevoPunto, GetComponent<IAMovimiento>().velocidadPersecucion);
        }
    }
}
