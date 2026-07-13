using UnityEngine;
using MonSumo.Core;

public class SkillController : MonoBehaviour
{
    [SerializeField] private FireRoundPoint firePoint;

    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    public void UseSkill()
    {
        //Nếu có fireSkill trong bộ skill
        if (firePoint != null) firePoint.Cast();
        //if
        
    }
}
