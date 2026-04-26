using UnityEngine;

public class SectorTactico : MonoBehaviour
{
    public string nombreSector;
    public Transform[] puntosDeInteres; 
    public SectorTactico[] sectoresAdyacentes; // Para que el Comandante sepa a dónde huiría el ladrón
}