using UnityEngine;

/// <summary>
/// 角色之间不做物理碰撞 —— 和网页版一致。
/// 网页版里狗、魔女、守卫是互相穿过去的，只有墙会挡人。
///
/// 用运行时设置而不是改 ProjectSettings，避免动工程配置。
/// </summary>
public static class PhysicsSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        int dog = LayerMask.NameToLayer("Dog");
        int witch = LayerMask.NameToLayer("Witch");
        int enemy = LayerMask.NameToLayer("Enemy");

        if (dog < 0 || witch < 0 || enemy < 0)
        {
            Debug.LogWarning("[PhysicsSetup] 找不到 Dog / Witch / Enemy 层，角色之间仍会碰撞");
            return;
        }

        // 角色 × 角色：全部穿过去
        Physics2D.IgnoreLayerCollision(dog, dog, true);
        Physics2D.IgnoreLayerCollision(witch, witch, true);
        Physics2D.IgnoreLayerCollision(enemy, enemy, true);

        Physics2D.IgnoreLayerCollision(dog, witch, true);
        Physics2D.IgnoreLayerCollision(dog, enemy, true);
        Physics2D.IgnoreLayerCollision(witch, enemy, true);

        // 墙照常挡（Wall 与三者的关系不动）
    }
}
