using UnityEngine;
using UnityEngine.AI; // Necesario para el cálculo de rutas (NavMeshPath)

// Estado: el guardia ejecuta el rol asignado por el Comandante.
// ¡NUEVO!: Ahora atiende a la radio y responde a subastas nuevas si lo detectan en otro lado.
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

            case PerformativaFIPA.ACCEPT_PROPOSAL:
                DatosContrato nuevaOrden = JsonUtility.FromJson<DatosContrato>(mensaje.contenido);
                cerebro.capaSocial.miRolAsignado = nuevaOrden.rolOfertado;
                cerebro.coordenadaTactica = nuevaOrden.coordenadaObjetivo;
                cerebro.ultimaPosJugador = nuevaOrden.coordenadaObjetivo; 
                
                if (nuevaOrden.puntosDeRuta != null && nuevaOrden.puntosDeRuta.Count > 0) {
                    cerebro.rutaExploracion = nuevaOrden.puntosDeRuta;
                }

                Debug.Log($"[SUBORDINADO {gameObject.name}] Cambio de orden recibido: {nuevaOrden.rolOfertado}");
                break;

            case PerformativaFIPA.REJECT_PROPOSAL:
                Debug.Log($"[SUBORDINADO {gameObject.name}] Escuadrón disuelto. Volviendo a patrulla.");
                cerebro.capaSocial.miRolAsignado = RolTactico.PatrullaNormal;
                fsmTactica.CambiarEstado(fsmTactica.libre);
                break;

            case PerformativaFIPA.CFP:
                // ¡EL ARREGLO ESTÁ AQUÍ! 
                // Ya no dicen "Estoy ocupado", ahora evalúan la nueva emergencia.
                ResponderCFP(mensaje);
                break;
        }
    }

    private void ProcesarInform(MensajeFIPA inform)
    {
        if (inform.contenido == "ALARMA_ROBO")
        {
            cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
            return;
        }

        if (inform.contenido.Contains('|'))
        {
            string[] p = inform.contenido.Split('|');
            Vector3 nuevoPunto = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );

            cerebro.coordenadaTactica = nuevoPunto;
            cerebro.ultimaPosJugador = nuevoPunto; 

            // Solo nos movemos al GPS si nuestra misión actual es perseguir
            if (cerebro.capaSocial.miRolAsignado == RolTactico.PersecucionActiva)
            {
                GetComponent<IAMovimiento>().MoverA(nuevoPunto, GetComponent<IAMovimiento>().velocidadPersecucion);
            }
        }
    }

    // Funciones copiadas del Guardia Libre para que el Subordinado sepa pujar
    private void ResponderCFP(MensajeFIPA cfp)
    {
        Vector3 objetivo;
        if (cfp.contenido.Contains('|'))
        {
            string[] p = cfp.contenido.Split('|');
            objetivo = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );
        }
        else
        {
            DatosContrato datos = JsonUtility.FromJson<DatosContrato>(cfp.contenido);
            objetivo = datos.coordenadaObjetivo;
        }

        float distancia = CalcularDistanciaNavMesh(transform.position, objetivo);

        MensajeFIPA propuesta = new MensajeFIPA(
            PerformativaFIPA.PROPOSE,
            gameObject,
            cfp.emisor,
            distancia.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );
        cfp.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(propuesta);
    }

    private float CalcularDistanciaNavMesh(Vector3 origen, Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(origen, destino, NavMesh.AllAreas, path))
        {
            float distanciaTotal = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distanciaTotal += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distanciaTotal;
        }
        return Vector3.Distance(origen, destino);
    }
}