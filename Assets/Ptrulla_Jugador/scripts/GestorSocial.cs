using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BuzonMensajes))]
public class GestorSocial : MonoBehaviour
{
    private BuzonMensajes miBuzon;
    private GestorSocial[] todosLosSociales; 
    private Transform miTransform;

    [Header("Comunicación con el Cerebro")]
    public bool alarmaGeneralActivada = false;
    public bool estaDisponible = true;   
    public bool tieneNuevaOrden = false; 
    public Vector3 coordenadaOrdenada;   

    private struct Oferta { public GameObject guardia; public float distancia; }
    private List<Oferta> listaDeOfertas = new List<Oferta>();

    void Awake()
    {
        miBuzon = GetComponent<BuzonMensajes>();
        miTransform = transform;
        todosLosSociales = FindObjectsOfType<GestorSocial>();
    }

    void Update()
    {
        int mensajesLeidos = 0;
        while (miBuzon != null && miBuzon.HayMensajesNuevos() && mensajesLeidos < 2)
        {
            ProcesarBuzon();
            mensajesLeidos++;
        }
    }

    public void IniciarSubasta(Vector3 posLadron)
    {
        listaDeOfertas.Clear();
        string contenido = posLadron.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

        foreach (GestorSocial compañero in todosLosSociales)
        {
            if (compañero != this)
            {
                // Escudo antierrores
                BuzonMensajes suBuzon = compañero.GetComponent<BuzonMensajes>();
                if (suBuzon != null)
                {
                    MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.CFP, this.gameObject, compañero.gameObject, contenido);
                    suBuzon.RecibirMensaje(aviso);
                }
            }
        }
        
        Debug.Log(" [" + gameObject.name + "] ¡INICIO LA SUBASTA! 0.5 segundos para recibir ofertas...");
        StartCoroutine(CerrarSubastaYAsignar(posLadron));
    }

    private void ProcesarBuzon()
    {
        MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
        if (mensaje == null) return;

        if (mensaje.performativa == PerformativaFIPA.CFP)
        {
            if (!estaDisponible) 
            {
                Debug.Log("[" + gameObject.name + "] Ignoro el aviso porque estoy ocupado.");
                return; 
            }

            Vector3 posLadron = ParsearCoordenadas(mensaje.contenido);
            float miDistancia = Vector3.Distance(miTransform.position, posLadron);
            
            MensajeFIPA oferta = new MensajeFIPA(PerformativaFIPA.PROPOSE, this.gameObject, mensaje.emisor, miDistancia.ToString(System.Globalization.CultureInfo.InvariantCulture));
            mensaje.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(oferta);
            
            Debug.Log(" [" + gameObject.name + "] Mando oferta: ¡Estoy a " + miDistancia + " metros!");
        }
        else if (mensaje.performativa == PerformativaFIPA.PROPOSE)
        {
            float distancia = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
            listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = distancia });
        }
        else if (mensaje.performativa == PerformativaFIPA.INFORM && mensaje.contenido == "ALARMA_ROBO")
        {
            alarmaGeneralActivada = true; 
            Debug.Log(" [" + gameObject.name + "] ¡CÓDIGO ROJO por radio! Voy a la emboscada.");
        }
        else if (mensaje.performativa == PerformativaFIPA.ACCEPT_PROPOSAL || (mensaje.performativa == PerformativaFIPA.INFORM && mensaje.contenido != "ALARMA_ROBO"))
        {
            coordenadaOrdenada = ParsearCoordenadas(mensaje.contenido);
            tieneNuevaOrden = true; 
            Debug.Log(" [" + gameObject.name + "] ¡Fui aceptado en el equipo! Voy a cortar el paso.");
        }
        else if (mensaje.performativa == PerformativaFIPA.REJECT_PROPOSAL)
        {
            Debug.Log(" [" + gameObject.name + "] El líder me rechazó. Sigo patrullando.");
        }
    }

    private Vector3 ParsearCoordenadas(string contenido)
    {
        string[] coord = contenido.Split('|');
        float x = float.Parse(coord[0], System.Globalization.CultureInfo.InvariantCulture);
        float y = float.Parse(coord[1], System.Globalization.CultureInfo.InvariantCulture);
        float z = float.Parse(coord[2], System.Globalization.CultureInfo.InvariantCulture);
        return new Vector3(x, y, z);
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log(" [SUBASTA] Fin de tiempo para " + gameObject.name + ". Ofertas sobre la mesa: " + listaDeOfertas.Count);

        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        int guardiasAceptados = 0;
        int maxGuardias = 2; 

        GestorSensores sensores = GetComponent<GestorSensores>();
        if(sensores == null || sensores.TransformJugador == null) 
        {
            Debug.LogError(" [ERROR TÁCTICO] " + gameObject.name + " perdió al jugador de vista demasiado rápido. Aborto subasta.");
            yield break;
        }

        Transform jugador = sensores.TransformJugador;
        Vector3 adelante = jugador.forward;
        Vector3 derecha = jugador.right;
        Vector3 izquierda = -jugador.right;

        Vector3[] puntosEstrategicos = new Vector3[] {
            posLadron + (derecha * 10f) + (adelante * 5f),  
            posLadron + (izquierda * 10f) + (adelante * 5f) 
        };

        foreach (Oferta oferta in listaDeOfertas)
        {
            if (guardiasAceptados < maxGuardias)
            {
                Vector3 punto = puntosEstrategicos[guardiasAceptados];
                string contAceptar = punto.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, this.gameObject, oferta.guardia, contAceptar);
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
                guardiasAceptados++;
                
                Debug.Log(" [EL LÍDER " + gameObject.name + "] ASIGNA A: " + oferta.guardia.name + " al flanco de interceptación!");
            }
            else
            {
                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, this.gameObject, oferta.guardia, "Sigue");
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
            }
        }
    }

    public void DarAlarmaRobo()
    {
        alarmaGeneralActivada = true; 
        foreach (GestorSocial compañero in todosLosSociales)
        {
            if (compañero != this)
            {
                BuzonMensajes suBuzon = compañero.GetComponent<BuzonMensajes>();
                if (suBuzon != null)
                {
                    MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.INFORM, this.gameObject, compañero.gameObject, "ALARMA_ROBO");
                    suBuzon.RecibirMensaje(aviso);
                }
            }
        }
    }
}