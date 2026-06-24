using UnityEngine;

public static class CharacterSelection
{
    public static int SelectionCharacterId = (int)CharacterType.None;

   public static void SetCharacterSelection(CharacterType characterType)
    {
        SelectionCharacterId = (int)characterType;
    }
}
