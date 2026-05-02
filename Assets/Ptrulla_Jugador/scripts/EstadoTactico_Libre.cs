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

                    if (cerebro.puntoMeta != null) cerebro.coordenadaTactica = cerebro.puntoMeta.position;
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

        float distancia = UtilidadesNavMesh.CalcularDistancia(transform.position, objetivo);
        Debug.Log($"[LIBRE {gameObject.name}] Respondiendo a CFP de {cfp.emisor.name}. Mi distancia al objetivo: {distancia:F1}m");

        MensajeFIPA propuesta = new MensajeFIPA(
            PerformativaFIPA.PROPOSE,
            gameObject,
            cfp.emisor,
            distancia.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );
        cfp.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(propuesta);
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

            
            Debug.Log($"[RADIO] {gameObject.name} recibe contrato: {contrato.rolOfertado}. Destino: {contrato.coordenadaObjetivo}. Mi puntoMeta está en: {cerebro.puntoMeta.position}");
            
            if (contrato.rolOfertado == RolTactico.PatrullaSectorAdyacente && !string.IsNullOrEmpty(contrato.nombreSectorDestino))
            {
                GameObject sectorObj = GameObject.Find(contrato.nombreSectorDestino);
                if (sectorObj != null)
                {
                    Debug.Log($"[LIBRE {gameObject.name}] Asignando sector adyacente REAL: {contrato.nombreSectorDestino}");
                    SectorTactico nuevoSector = sectorObj.GetComponent<SectorTactico>();
                    GetComponent<IAMovimiento>().AsignarNuevaRutaDesdeSector(nuevoSector);
                }
            }
        }

        fsmTactica.CambiarEstado(fsmTactica.subordinado);
    }
}
