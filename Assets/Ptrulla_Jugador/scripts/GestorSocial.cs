using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BuzonMensajes))]
public class GestorSocial : MonoBehaviour
{
    private BuzonMensajes miBuzon;
    private GestorSocial[] todosLosSociales; 
    private IACerebro miCerebro;

    [Header("FSM Superior (Táctica)")]
    public EstadoTactico estadoTacticoActual = EstadoTactico.Libre;
    public RolTactico miRolAsignado = RolTactico.PatrullaNormal;

    [Header("Planificador Táctico")]
    public FaseAlerta faseActual = FaseAlerta.Tranquilidad;
    private float tiempoDesdePerdido = 0f;

    private struct Oferta { public GameObject guardia; public float distancia; }
    private List<Oferta> listaDeOfertas = new List<Oferta>();

    void Awake()
    {
        miBuzon = GetComponent<BuzonMensajes>();
        todosLosSociales = FindObjectsByType<GestorSocial>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    void Update()
    {
        int mensajesLeidos = 0;
        while (miBuzon != null && miBuzon.HayMensajesNuevos() && mensajesLeidos < 2)
        {
            ProcesarBuzon();
            mensajesLeidos++;
        }
        // 2. Lógica del Planificador (Solo si soy el líder temporal de la situación)
        if (estadoTacticoActual == EstadoTactico.Comandante)
        {
            ActualizarPlanificador();
        }
    }

    public void AsumirMandoYSubastar(Vector3 posLadron)
    {
        if (estadoTacticoActual == EstadoTactico.Comandante) return;

        estadoTacticoActual = EstadoTactico.Comandante;
        faseActual = FaseAlerta.ContactoVisual;
        tiempoDesdePerdido = 0f;

        Debug.Log($" [{gameObject.name}] ¡Contacto! Asumo el mando táctico. Abriendo subasta...");
        IniciarSubasta(posLadron, RolTactico.PersecucionActiva);
    }

    private void IniciarSubasta(Vector3 posLadron, RolTactico rolPrincipal)
    {
        listaDeOfertas.Clear();
        
        DatosContrato contratoSubasta = new DatosContrato();
        contratoSubasta.rolOfertado = rolPrincipal;
        contratoSubasta.coordenadaObjetivo = posLadron;
        
        string contenidoJson = JsonUtility.ToJson(contratoSubasta);

        foreach (GestorSocial compañero in todosLosSociales)
        {
            if (compañero != this && compañero.miBuzon != null)
            {
                MensajeFIPA cfp = new MensajeFIPA(PerformativaFIPA.CFP, this.gameObject, compañero.gameObject, contenidoJson);
                compañero.miBuzon.RecibirMensaje(cfp);
            }
        }
        
        StartCoroutine(CerrarSubastaYAsignar(posLadron));
    }

    private void ProcesarBuzon()
    {
        MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
        if (mensaje == null) return;

        switch (mensaje.performativa)
        {
            case PerformativaFIPA.CFP:
                ResponderACFP(mensaje);
                break;

            case PerformativaFIPA.PROPOSE:
                if (estadoTacticoActual == EstadoTactico.Comandante)
                {
                    float dist = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
                    listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = dist });
                }
                break;

            case PerformativaFIPA.ACCEPT_PROPOSAL:
                DatosContrato contrato = JsonUtility.FromJson<DatosContrato>(mensaje.contenido);
                miRolAsignado = contrato.rolOfertado;
                estadoTacticoActual = EstadoTactico.Subordinado;
                
                // LA MAGIA: Si me mandan a patrullar adyacentes, le paso los puntos al cuerpo
                if (miRolAsignado == RolTactico.PatrullaSectorAdyacente && contrato.puntosDeRuta.Count > 0)
                {
                    GetComponent<IAMovimiento>().AsignarRutaDinamicaPorPuntos(contrato.puntosDeRuta);
                }
                
                Debug.Log($" [{gameObject.name}] He recibido órdenes: Mi rol ahora es {miRolAsignado}.");
                break;

            case PerformativaFIPA.REJECT_PROPOSAL:
                if (estadoTacticoActual == EstadoTactico.Subordinado)
                {
                    estadoTacticoActual = EstadoTactico.Libre;
                    miRolAsignado = RolTactico.PatrullaNormal;
                }
                break;
                
            case PerformativaFIPA.INFORM:
                if (mensaje.contenido == "ALARMA_ROBO")
                {
                    faseActual = FaseAlerta.ContactoVisual;
                    miRolAsignado = RolTactico.BloqueoSalida;
                    Debug.Log($" [{gameObject.name}] ¡CÓDIGO ROJO! El botín ha sido robado. Corriendo a las salidas.");
                }
                break;
        }
    }

    private void ResponderACFP(MensajeFIPA mensaje)
    {
        // Solo respondemos si no estamos ocupados tapando puertas u otras cosas vitales
        if (estadoTacticoActual == EstadoTactico.Libre || miRolAsignado == RolTactico.PatrullaNormal || miRolAsignado == RolTactico.PatrullaSectorAdyacente)
        {
            DatosContrato datos = JsonUtility.FromJson<DatosContrato>(mensaje.contenido);
            float miDist = Vector3.Distance(transform.position, datos.coordenadaObjetivo);

            MensajeFIPA propuesta = new MensajeFIPA(PerformativaFIPA.PROPOSE, this.gameObject, mensaje.emisor, miDist.ToString(System.Globalization.CultureInfo.InvariantCulture));
            mensaje.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(propuesta);
        }
    }

    // ==============================================================
    // REGLAS TÁCTICAS DE REPARTO (La Inteligencia del Escuadrón)
    // ==============================================================

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.4f); // Simula el tiempo de red

        // Ordenamos del que está más cerca al que está más lejos
        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        // 1. ¿Qué hago yo, el Comandante?
        bool soyVigia = (GetComponent<IACerebroVigia>() != null); 
        if (!soyVigia) 
        {
            miRolAsignado = RolTactico.PersecucionActiva; // El de a pie persigue por instinto
        }

        int puertasBloqueadas = 0;

        // --- AÑADE ESTAS DOS LÍNEAS AQUÍ ---
        SectorTactico miSector = GetComponent<IAMovimiento>().sectorActual;
        int indiceAdyacente = 0;
        // -----------------------------------

        // 2. Repartimos a los demás según su cercanía
        for (int i = 0; i < listaDeOfertas.Count; i++)
        {
            DatosContrato respuesta = new DatosContrato();
            
            // REGLA 1: Si soy el Vigía y no puedo correr, mando al guardia más cercano (i == 0)
            if (soyVigia && i == 0)
            {
                respuesta.rolOfertado = RolTactico.PersecucionActiva;
                respuesta.coordenadaObjetivo = posLadron;
            }
            // REGLA 2: Los siguientes 2 más cercanos bloquean salidas
            else if (puertasBloqueadas < 2)
            {
                respuesta.rolOfertado = RolTactico.BloqueoSalida;
                // Si tienes punto de meta, les mandamos ahí. (Mejoraremos esto luego con el mapa real)
                respuesta.coordenadaObjetivo = (miCerebro.puntoMeta != null) ? miCerebro.puntoMeta.position : posLadron;
                puertasBloqueadas++;
            }
            // REGLA 3: El penúltimo (o el cuarto) peina el sector donde vimos al ladrón
            else if (i == listaDeOfertas.Count - 2 || i == 3) 
            {
                respuesta.rolOfertado = RolTactico.ExplorarSectorSospechoso;
                respuesta.coordenadaObjetivo = posLadron; 
            }
            // REGLA 4: Los que pillen más lejos, cierran la red por fuera (sectores adyacentes)
            // REGLA 4: Asignar Sectores Adyacentes Reales
            else 
            {
                respuesta.rolOfertado = RolTactico.PatrullaSectorAdyacente;
                
                // Si mi sector tiene sectores conectados en el mapa...
                if (miSector != null && miSector.sectoresAdyacentes != null && miSector.sectoresAdyacentes.Length > 0)
                {
                    // Elegimos uno (y si hay varios guardias de sobra, se los vamos rotando)
                    SectorTactico sectorDestino = miSector.sectoresAdyacentes[indiceAdyacente % miSector.sectoresAdyacentes.Length];
                    
                    // Extraemos los Vector3 y los metemos en el paquete
                    foreach (Transform t in sectorDestino.puntosDeInteres)
                    {
                        respuesta.puntosDeRuta.Add(t.position);
                    }
                    indiceAdyacente++;
                }
            }

            string json = JsonUtility.ToJson(respuesta);
            MensajeFIPA accept = new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, this.gameObject, listaDeOfertas[i].guardia, json);
            listaDeOfertas[i].guardia.GetComponent<BuzonMensajes>().RecibirMensaje(accept);
        }
    }

    private void ActualizarPlanificador()
    {
        if (miCerebro.objetivoDetectado)
        {
            tiempoDesdePerdido = 0f;
            faseActual = FaseAlerta.ContactoVisual;
        }
        else
        {
            tiempoDesdePerdido += Time.deltaTime;

            // FASE 2: Recién perdido. Pasamos de Perseguir a Buscar.
            if (faseActual == FaseAlerta.ContactoVisual && tiempoDesdePerdido > 2f)
            {
                faseActual = FaseAlerta.BusquedaActiva;
                if (miRolAsignado == RolTactico.PersecucionActiva) 
                    miRolAsignado = RolTactico.ExplorarSectorSospechoso; 
            }
            
            // FASE 3: ASEDIO. Llevamos 15s. Las salidas están bloqueadas, así que sigue dentro.
            else if (faseActual == FaseAlerta.BusquedaActiva && tiempoDesdePerdido > 15f)
            {
                faseActual = FaseAlerta.Contencion;
                Debug.Log($" [{gameObject.name}] Asedio: ¡Las puertas están bloqueadas, seguid buscando, no ha salido!");
            }

            // FASE 4: ABORTO. Llevamos 45s. Confirmamos que ha huido de alguna forma.
            else if (faseActual == FaseAlerta.Contencion && tiempoDesdePerdido > 45f)
            {
                Debug.Log($" [{gameObject.name}] Sector despejado. Desmontando escuadrón.");
                TerminarMando();
            }
        }
    }

    private void TerminarMando()
    {
        estadoTacticoActual = EstadoTactico.Libre;
        miRolAsignado = RolTactico.PatrullaNormal;
        faseActual = FaseAlerta.Tranquilidad;
        
        foreach (GestorSocial compañero in todosLosSociales)
        {
             if (compañero != this && compañero.miBuzon != null)
             {
                 MensajeFIPA fin = new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, this.gameObject, compañero.gameObject, "Vuelta a patrulla.");
                 compañero.miBuzon.RecibirMensaje(fin);
             }
        }
    }

    public void DarAlarmaRobo()
    {
        foreach (GestorSocial compañero in todosLosSociales)
        {
            if (compañero != this && compañero.miBuzon != null)
            {
                MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.INFORM, this.gameObject, compañero.gameObject, "ALARMA_ROBO");
                compañero.miBuzon.RecibirMensaje(aviso);
            }
        }
    }
}