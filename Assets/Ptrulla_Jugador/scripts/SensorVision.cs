using UnityEngine;

// Módulo exclusivo para la lógica visual
public class SensorVision : MonoBehaviour
{
    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(90f, 250f)] public float anguloVision = 90f;
    public float alturaOjos = 1.6f;
    public float alturaObjetivoRelativa = 1.0f;

    public Transform EvaluarVision()
    {
        Collider[] objetosEnRango = Physics.OverlapSphere(transform.position, distanciaVision);

        foreach (Collider col in objetosEnRango)
        {
            if (col.CompareTag("Player"))
            {
                Vector3 ojos = transform.position + Vector3.up * alturaOjos;
                Vector3 objetivo = col.transform.position + Vector3.up * alturaObjetivoRelativa;
                Vector3 dir = objetivo - ojos;
                float distancia = dir.magnitude;

                // Comprobamos si el jugador está dentro del cono de visión frontal
                Vector3 dirH = dir;
                dirH.y = 0f;
                float ang = Vector3.Angle(transform.forward, dirH.normalized);

                if (dirH.sqrMagnitude >= 0.001f && ang > anguloVision / 2f)
                    continue;

                // Verificamos línea de visión con raycast ordenado por distancia
                RaycastHit[] hits = Physics.RaycastAll(ojos, dir.normalized, distancia, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider.isTrigger) continue;
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                    if (hit.transform == col.transform || hit.transform.IsChildOf(col.transform))
                    {
                        return col.transform;
                    }
                    else
                    {
                        break; // Hay una pared entre el guardia y el jugador
                    }
                }
            }
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaVision);
    }
}
