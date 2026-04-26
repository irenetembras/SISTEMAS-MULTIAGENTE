using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(BuzonMensajes))]
public class IACerebroVigia : MonoBehaviour
{
    private GestorSensores sensores;
    private BuzonMensajes miBuzon;
    
    // Solo contactará con los guardias que caminan (los que tienen el cerebro normal)
    private IACerebro[] guardiasTerrestres; 

    private struct Oferta
    {
        public GameObject guardia;
        public float distancia;
    }
    private List<Oferta> listaDeOfertas = new List<Oferta>();

    // Para no spamear subastas si ya está coordinando una
    private bool subastaEnCurso = false; 

    private List<GameObject> miEscuadron = new List<GameObject>(); // Para recordar a quién contratamos
    private float relojGPS = 0f;

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
        miBuzon = GetComponent<BuzonMensajes>();
        
        // Fichamos a todos los guardias "de a pie" del mapa
        guardiasTerrestres = FindObjectsByType<IACerebro>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); 
    }

    void OnEnable()
    {
        // Nos suscribimos para que, cuando los ojos vean algo, apretemos el botón de alarma
        sensores.OnJugadorDetectado += AlDetectar;
    }

    void OnDisable()
    {
        sensores.OnJugadorDetectado -= AlDetectar;
    }

    void Update()
    {
        // Leer el buzón para recoger las ofertas de los demás
        if (miBuzon != null && miBuzon.HayMensajesNuevos())
        {
            ProcesarBuzon();
        }

        // Si tenemos un escuadrón activo y seguimos viendo al ladrón, mandamos GPS
        if (subastaEnCurso && miEscuadron.Count > 0 && sensores.TransformJugador != null)
        {
            relojGPS += Time.deltaTime;
            if (relojGPS >= 0.5f) // Actualizamos la posición cada medio segundo
            {
                TransmitirGPS();
                relojGPS = 0f;
            }
        }
    }

    // --- EL VIGÍA VE AL LADRÓN ---
    private void AlDetectar(Vector3 pos)
    {
        // NUEVO: Si le vemos y lleva el botín encima, chivatazo masivo
        if (RecogerObjetivo.tieneElBotin)
        {
            DarAlarmaRobo();
            return; // Cortamos aquí para que no haga subasta normal
        }

        // LO VIEJO: Si no ha robado nada aún, subasta normal para acorralarlo
        if (!subastaEnCurso)
        {
            subastaEnCurso = true;
            listaDeOfertas.Clear();
            EnviarAvisoDeLadron(pos);
            StartCoroutine(CerrarSubastaYAsignar(pos));
        }
    }

    private void DarAlarmaRobo()
    {
        // Para no spamear mensajes 60 veces por segundo si le sigue viendo
        if (subastaEnCurso) return; 
        subastaEnCurso = true;

        foreach (IACerebro compañero in guardiasTerrestres)
        {
            MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.INFORM, this.gameObject, compañero.gameObject, "ALARMA_ROBO");
            compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
        }
        Debug.Log("🦅 [VIGÍA DE LA TORRE] ¡Veo al ladrón escapando con el botín! ¡CÓDIGO ROJO!");
    }

    // --- MÉTODOS DE COMUNICACIÓN MULTIAXENTE ---
    private void EnviarAvisoDeLadron(Vector3 posLadron)
    {
        string contenido = posLadron.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

        foreach (IACerebro compañero in guardiasTerrestres)
        {
            MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.CFP, this.gameObject, compañero.gameObject, contenido);
            compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
        }
        Debug.Log("🦅 [VIGÍA DE LA TORRE] ¡Veo al ladrón en " + contenido + "! ¡Abrid subasta!");
    }

    private void ProcesarBuzon()
    {
        MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
        if (mensaje == null) return;

        // El vigía solo espera recibir propuestas (PROPOSE) de distancia
        if (mensaje.performativa == PerformativaFIPA.PROPOSE)
        {
            float distancia = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
            listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = distancia });
        }
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        // Da medio segundo a los de abajo para responder
        yield return new WaitForSeconds(0.5f);

        // Ordenamos del que está más cerca al que está más lejos
        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        int guardiasAceptados = 0;
        int maxGuardias = 2; // Manda a 2 guardias a por ti

        
        // Extraemos la rotación actual del jugador a través de los sensores
        Transform jugador = GetComponent<GestorSensores>().TransformJugador;
        
        // Calculamos vectores relativos: hacia dónde mira y sus lados
        Vector3 adelante = jugador.forward;
        Vector3 derecha = jugador.right;
        Vector3 izquierda = -jugador.right;

        // Táctica de Pinza: 10 metros a los lados, y 5 metros hacia adelante para cortarle el paso
        Vector3[] puntosEstrategicos = new Vector3[] {
            posLadron + (derecha * 10f) + (adelante * 5f),  
            posLadron + (izquierda * 10f) + (adelante * 5f) 
        };

        miEscuadron.Clear();

        foreach (Oferta oferta in listaDeOfertas)
        {
            if (guardiasAceptados < maxGuardias)
            {
                Vector3 punto = puntosEstrategicos[guardiasAceptados];
                string contAceptar = punto.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, this.gameObject, oferta.guardia, contAceptar);
                miEscuadron.Add(oferta.guardia);
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
                guardiasAceptados++;
                

               
            }
            else
            {
                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, this.gameObject, oferta.guardia, "Sigue patrullando");
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
            }
        }

        // Reseteamos para que pueda volver a avisar si el ladrón se escapa y lo vuelve a ver
        yield return new WaitForSeconds(3f); 
        subastaEnCurso = false;
    }

    private void TransmitirGPS()
    {
        Transform jugador = sensores.TransformJugador;
        if (jugador == null) return;

        Vector3 adelante = jugador.forward;
        Vector3 derecha = jugador.right;
        Vector3 izquierda = -jugador.right;

        // Calculamos la pinza actualizada
        Vector3[] puntosEstrategicos = new Vector3[] {
            jugador.position + (derecha * 10f) + (adelante * 5f),  
            jugador.position + (izquierda * 10f) + (adelante * 5f) 
        };

        // Le mandamos a cada guardia de nuestro escuadrón su punto específico
        for (int i = 0; i < miEscuadron.Count; i++)
        {
            Vector3 punto = puntosEstrategicos[i];
            string contGPS = punto.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                             punto.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                             punto.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

            MensajeFIPA avisoGPS = new MensajeFIPA(PerformativaFIPA.INFORM, this.gameObject, miEscuadron[i], contGPS);
            miEscuadron[i].GetComponent<BuzonMensajes>().RecibirMensaje(avisoGPS);
        }
    }
}