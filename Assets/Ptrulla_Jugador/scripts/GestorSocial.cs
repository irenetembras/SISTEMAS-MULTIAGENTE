using UnityEngine;

// Capa social simplificada: almacena el rol táctico actual del guardia.
public class GestorSocial : MonoBehaviour
{
    [Header("Rol Táctico Actual")]
    public RolTactico miRolAsignado = RolTactico.PatrullaNormal;

    // Emite una alarma de robo a todos los guardias del escuadrón
    public void DarAlarmaRobo()
    {
        GestorSocial[] todos = FindObjectsByType<GestorSocial>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (GestorSocial comp in todos)
        {
            if (comp == this) continue;
            BuzonMensajes buzon = comp.GetComponent<BuzonMensajes>();
            if (buzon == null) continue;
            buzon.RecibirMensaje(new MensajeFIPA(PerformativaFIPA.INFORM, gameObject, comp.gameObject, "ALARMA_ROBO"));
        }
        Debug.Log($"[{gameObject.name}] ¡ALARMA DE ROBO emitida!");
    }
}
