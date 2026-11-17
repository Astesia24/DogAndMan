using UnityEditor;
using UnityEngine;

/// <summary>
/// 把场景里所有墙块合并成一个 CompositeCollider2D。
/// 物体本身还在（还能拖），但物理和射线只处理一个碰撞体，开销小很多。
/// </summary>
public static class WallMerger
{
    [MenuItem("Tools/把当前场景的墙合并成一个碰撞体")]
    public static void Merge()
    {
        var walls = GameObject.Find("Walls");
        if (walls == null)
        {
            EditorUtility.DisplayDialog("找不到 Walls", "当前场景里没有名为 Walls 的容器。", "好");
            return;
        }

        if (walls.GetComponent<CompositeCollider2D>() != null)
        {
            EditorUtility.DisplayDialog("已经合并过了", "Walls 上已经有 CompositeCollider2D。", "好");
            return;
        }

        var rb = walls.GetComponent<Rigidbody2D>();
        if (rb == null) rb = walls.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var comp = walls.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;

        int count = 0;
        foreach (var col in walls.GetComponentsInChildren<Collider2D>(true))
        {
            if (col.gameObject == walls) continue;

            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            count++;
        }

        EditorUtility.SetDirty(walls);
        Debug.Log($"[WallMerger] {count} 块墙合并到 1 个 CompositeCollider2D");
    }
}
