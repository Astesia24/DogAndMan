using UnityEngine;

/// <summary>
/// 狗洞：只有狗能钻过去，声音和视线能穿过去。
/// 放在墙上的缺口处，用自己的层（DogHole）挡魔女和守卫，
/// 但启动时和自己的碰撞体和狗的碰撞体互相忽略。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DogHole : MonoBehaviour
{
    private void Start()
    {
        var dog = FindObjectOfType<DogMotor>();
        if (dog == null) return;

        var hole = GetComponent<Collider2D>();
        foreach (var col in dog.GetComponentsInChildren<Collider2D>())
        {
            Physics2D.IgnoreCollision(hole, col, true);
        }
    }
}
