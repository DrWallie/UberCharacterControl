using UnityEngine;

public class CursorLock
{
    static bool  isLocked = true;

    static UberPlayerController player;
    //static CombatManager weapon;

    public void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #region Player Scripts Assignment
    /*public static void SetPlayerScripts (UberPlayerController _playerController, CombatManager  _weapon)
    {
        playerController = _playerController;
        weapon = _weapon;
    }*/
    public static  void SetPlayerScripts (UberPlayerController _playerController)
    {
        player = _playerController;
    }
    /*public static void SetPlayerScripts (CombatManager _weapon)
    {
        weapon = _weapon;
    }*/
    #endregion

    public static void Lock ()
    {
        Time.timeScale = isLocked ? 0f : 1.0f;

        //disable player & weapon
        if (player != null)
        {
            player.enabled = !isLocked;
        }
        /*if (weapon != null)
        {
            weapon.enabled = !isLocked;
        }*/

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = isLocked;
        isLocked = !isLocked;
    }
}