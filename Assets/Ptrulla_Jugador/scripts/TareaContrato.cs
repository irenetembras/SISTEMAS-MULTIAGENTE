using UnityEngine;
using System.Collections.Generic;

// Define una tarea táctica que el planificador puede asignar a un guardia.
// Para añadir una nueva tarea al sistema basta con crear una instancia de esta
// clase en PlanificadorTactico.GenerarTareas() — no hace falta tocar nada más.
[System.Serializable]
public class TareaContrato
{
    public string nombre;
    public RolTactico rol;
    public int guardiasNecesarios = 1;
    public Vector3 coordenada;
    public List<Vector3> puntosDeRuta = new List<Vector3>();
}
