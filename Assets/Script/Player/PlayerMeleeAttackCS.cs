using UnityEngine;

public class PlayerMeleeAttackCS : MonoBehaviour
{
    public int damage { get; private set; } = 1;

    public void Setup(int damageValue)
    {
        damage = damageValue;
    }
}
