using UnityEngine;
using UnityEngine.AI;

public static class UtilidadesNavMesh
{
    // Extraído y estandarizado para evitar cálculos en línea recta a través de paredes
    public static float CalcularDistancia(Vector3 origen, Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origen, out hit, 10.0f, NavMesh.AllAreas)) origen = hit.position;
        if (NavMesh.SamplePosition(destino, out hit, 10.0f, NavMesh.AllAreas)) destino = hit.position;

        if (NavMesh.CalculatePath(origen, destino, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                return 9999f; // Distancia gigante para perder subastas si es inalcanzable
            }
            
            float distanciaTotal = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distanciaTotal += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distanciaTotal;
        }

        Debug.LogWarning("UtilidadesNavMesh: Castigo de 9999m aplicado al fallar NavMesh.");
        return 9999f; 
    }
}