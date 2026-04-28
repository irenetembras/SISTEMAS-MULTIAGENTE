using UnityEngine;
using UnityEngine.AI;
// Estado: el guardia patrulla por su cuenta, escucha la radio y puede ser reclutado.
public class EstadoTactico_Libre : EstadoTacticoBase
{
    public override void AlEntrar()
    {
        base.AlEntrar();
        cerebro.capaSocial.miRolAsignado = RolTactico.PatrullaNormal;
    }

    public override void ProcesarMensaje(MensajeFIPA mensaje)
    {
        switch (mensaje.performativa)
        {
            case PerformativaFIPA.CFP:
                ResponderCFP(mensaje);
                break;

            case PerformativaFIPA.ACCEPT_PROPOSAL:
                AceptarContrato(mensaje);
                break;

            case PerformativaFIPA.REJECT_PROPOSAL:
                // Ya estamos libres, no hay nada que hacer
                break;

            case PerformativaFIPA.INFORM:
                if (mensaje.contenido == "ALARMA_ROBO")
                {
                    cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
                    fsmTactica.CambiarEstado(fsmTactica.subordinado);
                }
                break;
        }
    }

    // Responde a una convocatoria de subasta con nuestra distancia al objetivo
    private void ResponderCFP(MensajeFIPA cfp)
    {
        // El Vigía envía "x|y|z"; el Comandante envía JSON con DatosContrato
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
        Debug.Log($"[LIBRE {gameObject.name}] Respondiendo a CFP de {cfp.emisor.name}. Mi distancia al objetivo: {distancia:F1}m");

        MensajeFIPA propuesta = new MensajeFIPA(
            PerformativaFIPA.PROPOSE,
            gameObject,
            cfp.emisor,
            distancia.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );
        cfp.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(propuesta);
    }

    // NUEVA FUNCIÓN: Dibuja un camino mental por el mapa y mide lo largo que es
    private float CalcularDistanciaNavMesh(Vector3 origen, Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        
        // Si el NavMesh logra trazar un camino por el suelo hasta el destino...
        if (NavMesh.CalculatePath(origen, destino, NavMesh.AllAreas, path))
        {
            float distanciaTotal = 0f;
            
            // Sumamos la distancia de cada esquina del camino
            for (int i = 1; i < path.corners.Length; i++)
            {
                distanciaTotal += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distanciaTotal;
        }
        
        // CORTAFUEGOS: Si no hay ruta posible (ej: el jugador está saltando o fuera del mapa), 
        // usamos la línea recta como plan B para que el código no falle.
        return Vector3.Distance(origen, destino);
    }

    // Aplica el contrato ganado y pasa a estado Subordinado
    private void AceptarContrato(MensajeFIPA accept)
    {
        // El Vigía envía coordenadas en formato "x|y|z"; el Comandante envía JSON
        if (accept.contenido.Contains('|'))
        {
            string[] p = accept.contenido.Split('|');
            Vector3 destino = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );
            cerebro.capaSocial.miRolAsignado = RolTactico.PersecucionActiva;
            cerebro.coordenadaTactica = destino; 
            cerebro.ultimaPosJugador = destino;
            Debug.Log($"[LIBRE {gameObject.name}] Contrato del Vigia aceptado. Rol: PersecucionActiva. Yendo a {destino}");
            GetComponent<IAMovimiento>().MoverA(destino, GetComponent<IAMovimiento>().velocidadPersecucion);
        }
        else
        {
            DatosContrato contrato = JsonUtility.FromJson<DatosContrato>(accept.contenido);
            cerebro.capaSocial.miRolAsignado = contrato.rolOfertado;
            cerebro.coordenadaTactica = contrato.coordenadaObjetivo;
            cerebro.ultimaPosJugador = contrato.coordenadaObjetivo;
            Debug.Log($"[LIBRE {gameObject.name}] Contrato del Comandante aceptado. Rol asignado: {contrato.rolOfertado}");

            // ---> AÑADE ESTE CHIVATO AQUÍ <---
            Debug.Log($"[RADIO] {gameObject.name} recibe contrato: {contrato.rolOfertado}. Destino: {contrato.coordenadaObjetivo}. Mi puntoMeta está en: {cerebro.puntoMeta.position}");
            
            if (contrato.rolOfertado == RolTactico.PatrullaSectorAdyacente && contrato.puntosDeRuta.Count > 0)
            {
                Debug.Log($"[LIBRE {gameObject.name}] Ruta dinamica asignada con {contrato.puntosDeRuta.Count} puntos.");
                GetComponent<IAMovimiento>().AsignarRutaDinamicaPorPuntos(contrato.puntosDeRuta);
            }
        }

        fsmTactica.CambiarEstado(fsmTactica.subordinado);
    }
}
