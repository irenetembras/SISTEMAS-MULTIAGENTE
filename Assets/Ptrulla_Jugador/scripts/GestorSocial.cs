using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BuzonMensajes))]
// LA NUEVA CAPA 2: EXCLUSIVA PARA GESTIÓN FIPA Y SUBASTAS
public class GestorSocial : MonoBehaviour
{
    private BuzonMensajes miBuzon;
    private GestorSocial[] todosLosSociales; 
    private Transform miTransform;

    [Header("Comunicación con el Cerebro")]
    public bool alarmaGeneralActivada = false;
    public bool estaDisponible = true;   // El cerebro nos dirá si estamos ocupados
    public bool tieneNuevaOrden = false; // Nosotros le diremos al cerebro si hay órdenes
    public Vector3 coordenadaOrdenada;   // El GPS o punto táctico ya masticado

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
        // Procesamos hasta  mensajes por frame
        int mensajesLeidos = 0;
        while (miBuzon != null && miBuzon.HayMensajesNuevos() && mensajesLeidos < 2)
        {
            ProcesarBuzon();
            mensajesLeidos++;
        }
    }

    // El Cerebro llama a esta función cuando ve al jugador primero
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
                MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.CFP, this.gameObject, compañero.gameObject, contenido);
                compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
            }
        }
        StartCoroutine(CerrarSubastaYAsignar(posLadron));
    }

    private void ProcesarBuzon()
    {
        MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
        if (mensaje == null) return;

        if (mensaje.performativa == PerformativaFIPA.CFP)
        {
            if (!estaDisponible) return; // Si el Cerebro dice que estamos ocupados, no pujamos

            Vector3 posLadron = ParsearCoordenadas(mensaje.contenido);
            float miDistancia = Vector3.Distance(miTransform.position, posLadron);
            MensajeFIPA oferta = new MensajeFIPA(PerformativaFIPA.PROPOSE, this.gameObject, mensaje.emisor, miDistancia.ToString(System.Globalization.CultureInfo.InvariantCulture));
            mensaje.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(oferta);
        }
        else if (mensaje.performativa == PerformativaFIPA.PROPOSE)
        {
            float distancia = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
            listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = distancia });
        }
        // Juntamos el ACCEPT de la subasta y el INFORM del GPS porque ambos nos dan una meta
        // Extraemos la lectura del buzón
        else if (mensaje.performativa == PerformativaFIPA.INFORM && mensaje.contenido == "ALARMA_ROBO")
        {
            alarmaGeneralActivada = true; // ¡Nos avisaron del robo! Levantamos la bandera roja.
            Debug.Log("📻 [" + gameObject.name + "] ¡Recibido CÓDIGO ROJO por radio! Voy a la emboscada.");
        }
        else if (mensaje.performativa == PerformativaFIPA.ACCEPT_PROPOSAL || (mensaje.performativa == PerformativaFIPA.INFORM && mensaje.contenido != "ALARMA_ROBO"))
        {
            coordenadaOrdenada = ParsearCoordenadas(mensaje.contenido);
            tieneNuevaOrden = true; 
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
        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        int guardiasAceptados = 0;
        int maxGuardias = 2; 

        GestorSensores sensores = GetComponent<GestorSensores>();
        if(sensores == null || sensores.TransformJugador == null) yield break;

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
        alarmaGeneralActivada = true; // Me doy por enterado yo también

        foreach (GestorSocial compañero in todosLosSociales)
        {
            if (compañero != this)
            {
                // Mandamos un INFORM a todos diciendo la palabra mágica
                MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.INFORM, this.gameObject, compañero.gameObject, "ALARMA_ROBO");
                compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
            }
        }
    }
}