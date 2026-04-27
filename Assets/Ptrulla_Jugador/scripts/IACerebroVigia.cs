using UnityEngine;

public class IACerebroVigia : MonoBehaviour
{
    private GestorSensores sensores;
    private PlanificadorTactico planificador;
    private BuzonMensajes buzon;

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
        planificador = GetComponent<PlanificadorTactico>();
        buzon = GetComponent<BuzonMensajes>();
        
        planificador.Inicializar(null); // No tiene cuerpo físico
    }

    void OnEnable() { sensores.OnJugadorDetectado += AlDetectar; }
    void OnDisable() { sensores.OnJugadorDetectado -= AlDetectar; }

    void Update()
    {
        // El vigía solo escucha ofertas para pasárselas a su planificador
        if (buzon.HayMensajesNuevos())
        {
            MensajeFIPA m = buzon.ExtraerSiguienteMensaje();
            if (m.performativa == PerformativaFIPA.PROPOSE)
            {
                float dist = float.Parse(m.contenido, System.Globalization.CultureInfo.InvariantCulture);
                planificador.RecibirOferta(m.emisor, dist);
            }
        }
    }

    private void AlDetectar(Vector3 pos)
    {
        planificador.IniciarPlanificacion(pos);
    }
}