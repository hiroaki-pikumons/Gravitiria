using UnityEngine;

public class SecurityLaser : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerUtil.IsPlayer(other)) return;

        GravityController player = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<GravityController>()
            : other.GetComponent<GravityController>();

        if (player != null)
        {
            if (player.IsInvincible) return;
            Debug.Log($"[SecurityLaser] {name} at {transform.position} (rot {transform.eulerAngles}) killed player at {player.transform.position}");
            player.ResetStage();
            return;
        }

        SceneFader fader = FindAnyObjectByType<SceneFader>();
        if (fader != null)
        {
            fader.FadeAndReload();
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
