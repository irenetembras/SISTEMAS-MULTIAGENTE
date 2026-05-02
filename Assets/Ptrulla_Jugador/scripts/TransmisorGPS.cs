using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GestorSensores))]
public class TransmisorGPS : MonoBehaviour
{
    private GestorSensores sensores;
    private float relojGPS = 0f;
    private List<GameObject> perseguidoresGPS = new List<GameObject>();

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
    }

    void Update()
    {
        if (perseguidoresGPS.Count == 0 || sensores.TransformJugador == null) return;

        // Solo transmitimos si el sensor lo está detectando AHORA
        if (!sensores.EnContactoConJugador) return;

        relojGPS += Time.deltaTime;
        if (relojGPS >= 0.5f)
        {
            string pos = sensores.TransformJugador.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                         sensores.TransformJugador.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                         sensores.TransformJugador.position.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

            foreach (GameObject p in perseguidoresGPS)
            {
                p.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.INFORM, gameObject, p, pos));
            }
            relojGPS = 0f;
        }
    }

    // Funciones públicas para que el Planificador añada o quite perseguidores
    public void RegistrarPerseguidor(GameObject guardia)
    {
        if (!perseguidoresGPS.Contains(guardia)) perseguidoresGPS.Add(guardia);
    }

    // Permite que otros scripts consulten quién está persiguiendo actualmente
    public List<GameObject> ObtenerPerseguidores()
    {
        return perseguidoresGPS;
    }

    public void LimpiarPerseguidores()
    {
        perseguidoresGPS.Clear();
    }
}