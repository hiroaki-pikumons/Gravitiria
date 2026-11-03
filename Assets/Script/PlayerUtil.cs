using UnityEngine;

public static class PlayerUtil
{
    public const string Tag = "Player";

    // 旧シーンではタグ未設定で名前だけ "Player" のため、両方を許容する
    public static bool IsPlayer(Component c)
    {
        if (c == null) return false;
        var root = c.attachedRigidbodyOrSelf();
        return root.CompareTag(Tag) || root.name == Tag;
    }

    private static GameObject attachedRigidbodyOrSelf(this Component c)
    {
        if (c is Collider col && col.attachedRigidbody != null) return col.attachedRigidbody.gameObject;
        return c.gameObject;
    }
}
