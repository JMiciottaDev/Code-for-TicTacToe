
using UnityEngine;

public class TicTacToeProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 3f;
    public float upwardsModifier = 0.5f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    private bool hasLanded;
    private bool hasExploded;


    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider other)
    {
        if (hasLanded || hasExploded)
            return;

        BoardSlot slot = other.GetComponent<BoardSlot>();

        if (slot == null)
            slot = other.GetComponentInParent<BoardSlot>();

        if (slot == null)
        {
            Explode();
            return;
        }

        TicTacToePiece piece = GetComponent<TicTacToePiece>();
        TicTacToeManager manager =
            FindFirstObjectByType<TicTacToeManager>();

        if (piece != null && manager != null &&
            manager.TryPlacePiece(piece, slot))
        {
            MarkLanded();
        }
    }

    private void Explode()
    {
        if (hasExploded || hasLanded)
            return;

        hasExploded = true;

        Vector3 explosionPosition = transform.position;

        if (explosionEffect != null)
        {
            Instantiate(
                explosionEffect,
                explosionPosition,
                Quaternion.identity);
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(
                explosionSound,
                explosionPosition);
        }

        Collider[] nearbyColliders = Physics.OverlapSphere(
            explosionPosition,
            explosionRadius);

        foreach (Collider col in nearbyColliders)
        {
            Rigidbody rb = col.attachedRigidbody;

            if (rb != null && rb.gameObject != gameObject)
            {
                rb.AddExplosionForce(
                    explosionForce,
                    explosionPosition,
                    explosionRadius,
                    upwardsModifier,
                    ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }

    public void ResetForLaunch()
    {
        hasLanded = false;
        hasExploded = false;
    }

    public void MarkLanded()
    {
        hasLanded = true;
    }
}