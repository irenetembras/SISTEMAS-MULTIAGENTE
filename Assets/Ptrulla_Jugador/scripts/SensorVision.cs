using UnityEngine;

// Módulo exclusivo para la lógica visual (Responsabilidad Única)
public class SensorVision : MonoBehaviour
{
    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(90f, 250f)] public float anguloVision = 90f;
    public float alturaOjos = 1.6f;
    public float alturaObjetivoRelativa = 1.0f;

    // YA NO RECIBE AL JUGADOR. Escanea a ciegas a su alrededor.
    public Transform EvaluarVision()
    {
        // 1. EL RADAR: Cogemos TODOS los objetos en un radio a nuestro alrededor
        Collider[] objetosEnRango = Physics.OverlapSphere(transform.position, distanciaVision);

        foreach (Collider col in objetosEnRango)
        {
            // 2. EL FILTRO: ¿Es este objeto el jugador?
            if (col.CompareTag("Player"))
            {
                Vector3 ojos = transform.position + Vector3.up * alturaOjos;
                Vector3 objetivo = col.transform.position + Vector3.up * alturaObjetivoRelativa;
                Vector3 dir = objetivo - ojos;
                float distancia = dir.magnitude;

                // 3. EL CONO: Comprobamos si está dentro del ángulo de visión frontal
                Vector3 dirH = dir; 
                dirH.y = 0f;
                float ang = Vector3.Angle(transform.forward, dirH.normalized);
                
                // Si está fuera de nuestra visión periférica (a la espalda), pasamos
                if (dirH.sqrMagnitude >= 0.001f && ang > anguloVision / 2f) 
                    continue; 

                // 4. EL LÁSER (Línea de visión): Comprobamos que no haya paredes
                RaycastHit[] hits = Physics.RaycastAll(ojos, dir.normalized, distancia, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider.isTrigger) continue;
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                    // Si lo primero sólido que toco es el jugador... ¡LO VEO!
                    if (hit.transform == col.transform || hit.transform.IsChildOf(col.transform))
                    {
                        return col.transform; // Devuelvo su Transform para confirmar que lo vi
                    }
                    else
                    {
                        break; // Chocó con una pared de ladrillos. Dejo de mirar a este objeto.
                    }
                }
            }
        }
        return null; // Si termina de escanear y no ve a nadie, devuelve null
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Dibuja la burbuja del radar en la escena
        Gizmos.DrawWireSphere(transform.position, distanciaVision); 
    }
}