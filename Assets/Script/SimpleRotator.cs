using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    // 回転する速度（インスペクターから自由に変えられます）
    public Vector3 rotationSpeed;

    void Update()
    {
        // Time.unscaledDeltaTime を使うことで、スローモーション中も滑らかに回ります
        transform.Rotate(rotationSpeed * Time.unscaledDeltaTime);
    }
}
