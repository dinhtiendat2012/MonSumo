using UnityEngine;
public class SkillController : MonoBehaviour
{
    [SerializeField] private FireRoundPoint firePoint;

    public void UseSkill()
    {
        //Nếu có fireSkill trong bộ skill
        if (firePoint != null) firePoint.Cast();
        //if
        
    }
}
