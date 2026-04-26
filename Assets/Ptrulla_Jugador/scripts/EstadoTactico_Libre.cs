using UnityEngine;

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

        float distancia = Vector3.Distance(transform.position, objetivo);
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
            Debug.Log($"[LIBRE {gameObject.name}] Contrato del Vigia aceptado. Rol: PersecucionActiva. Yendo a {destino}");
            GetComponent<IAMovimiento>().MoverA(destino, GetComponent<IAMovimiento>().velocidadPersecucion);
        }
        else
        {
            DatosContrato contrato = JsonUtility.FromJson<DatosContrato>(accept.contenido);
            cerebro.capaSocial.miRolAsignado = contrato.rolOfertado;
            Debug.Log($"[LIBRE {gameObject.name}] Contrato del Comandante aceptado. Rol asignado: {contrato.rolOfertado}");

            if (contrato.rolOfertado == RolTactico.PatrullaSectorAdyacente && contrato.puntosDeRuta.Count > 0)
            {
                Debug.Log($"[LIBRE {gameObject.name}] Ruta dinamica asignada con {contrato.puntosDeRuta.Count} puntos.");
                GetComponent<IAMovimiento>().AsignarRutaDinamicaPorPuntos(contrato.puntosDeRuta);
            }
        }

        fsmTactica.CambiarEstado(fsmTactica.subordinado);
    }
}
